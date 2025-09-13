using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using orderSys_bk.Data;
using orderSys_bk.Model.Dto;
using senior_project_web.Models;
using System.Linq;

namespace orderSys_bk.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly OrderSysDbContext _dbContext;
        public CustomerController(OrderSysDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 生成隨機訂單編號
        /// </summary>
        /// <param name="dbContext">資料庫操作的DbContext</param>
        /// <returns>與資料庫中訂單不重複的Id</returns>
        private static string GenerateRandomOrderId(OrderSysDbContext dbContext)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            string newId;
            do
            {
                newId = new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());
            } while (dbContext.Order.Any(o => o.order_id == newId));

            return newId;
        } 

        //取得餐點列表
        [HttpGet("getMeals")]
        public async Task<IActionResult> GetMeals()
        {
            try
            {
                var meals = await _dbContext.Meal
                    .Select(m => new
                    {
                        id = m.meal_id,
                        name = m.name,
                        type = m.type,
                        img_path = m.img_path,
                        description = m.description,
                        price = m.price,
                        cost = m.cost,
                    }).ToListAsync();
                if (meals == null)
                {
                    return BadRequest(new { message = "找不到餐點" });
                }
                return Ok(meals);
            }catch (Exception ex)
            {
                return StatusCode(500, new { message = $"錯誤: {ex.Message}" });
            }
        }

        //處理點餐
        [HttpPost("createOrder")]
        public async Task<IActionResult> CreateOrder(Dictionary<String, Object> req)
        {
            try
            {
                Console.WriteLine("=====【ConnectionStart: CustomerController -> CreateOrder()】=====");
                Console.WriteLine($"【CustomerController】 -> CreateOrder() -> 處理點餐: req: {Services.JsonServices.ToJson(req)}");
                if (req == null || req.Count == 0)
                {
                    return BadRequest(new { success = false, message = "訂單建立失敗: 請提供點餐資訊!" });
                }

                String user_id = req.ContainsKey("user_id") ? req["user_id"]?.ToString() : "guest"; //user_id
                String payment = req.ContainsKey("payment") ? req["payment"]?.ToString() : null; //付款方式
                int total = req.ContainsKey("total") ? Services.JsonServices.ToInt(req["total"] ?? 0) : 0; //訂單總價
                Dictionary<String, Object> location = req.ContainsKey("location") ? Services.JsonServices.ToDictionary(req["location"]) as Dictionary<String, Object> : null; //位置資訊
                List<Dictionary<String, Object>> orders = req.ContainsKey("orders") ? Services.JsonServices.ToListOfDictionary(req["orders"]) as List<Dictionary<String, Object>> : null; //點餐內容
                Console.WriteLine($"【CustomerController】 -> CreateOrder() -> user_id: {user_id}, payment: {payment}, total: {total}, location: {Services.JsonServices.ToJson(location)}, orders: {Services.JsonServices.ToJson(orders)}");

                if (payment.IsNullOrEmpty() || total < 0 || location == null || orders == null || orders.Count() <= 0)
                {
                    return BadRequest(new { success = false, message = "訂單建立失敗: 錯誤的點餐資訊!" });
                }

                String latitudeStr = location.ContainsKey("latitude") ? location["latitude"]?.ToString() : null; //緯度
                String longitudeStr = location.ContainsKey("longitude") ? location["longitude"]?.ToString() : null; //經度
                Console.WriteLine($"【CustomerController】 -> CreateOrder() -> latitudeStr: {latitudeStr}, longitudeStr: {longitudeStr}");

                if (latitudeStr.IsNullOrEmpty() || longitudeStr.IsNullOrEmpty())
                {
                    return BadRequest(new { success = false, message = "訂單建立失敗: 錯誤的地理位置資訊!" });
                }

                double latitude = double.Parse(latitudeStr); //緯度轉double
                double longitude = double.Parse(longitudeStr); //經度轉double

                string cusDateStr = "2025-09-06"; //客製化日期
                DateTime cusDateTime = DateTime.Parse(cusDateStr); //客製化日期轉DateTime
                DateTime orderDate = DateTime.Now; //訂單建立日期

                String season = Services.WeatherService.getSeason(DateTime.Now.Month); //取得當前季節
                string weather_condition = await Services.WeatherService.GetWeatherForecastAsync(cusDateStr, latitude, longitude); //取得天氣狀況
                Console.WriteLine($"【CustomerController】 -> CreateOrder() -> season: {season}, weather_condition: {weather_condition}");

                if (weather_condition.IsNullOrEmpty() || season.IsNullOrEmpty())
                {
                    return BadRequest(new { success = false, message = "訂單建立失敗: 無法取得天氣資訊!" });
                }

                //創造訂單物件
                var newOrder = new OrderModel
                {
                    order_id = GenerateRandomOrderId(_dbContext),
                    date = cusDateTime,//orderDate,
                    weather_condition = weather_condition,
                    season = season,
                    payment = payment,
                    total = total,
                    user_id = user_id,
                };
                Console.WriteLine($"【CustomerController】 -> CreateOrder() -> newOrder: {Services.JsonServices.ToJson(newOrder)}");

                int orderTotal = 0; //訂單的總價格

                //創造訂單餐點物件
                var order_meals = new List<Order_MealModel>();
                foreach (var meal in orders)
                {
                    String meal_id_str = meal.ContainsKey("meal_id") ? meal["meal_id"]?.ToString() : null;
                    String name = meal.ContainsKey("name") ? meal["name"]?.ToString() : null;
                    int amount = meal.ContainsKey("amount") ? Services.JsonServices.ToInt(meal["amount"] ?? 0) : 0;
                    Console.WriteLine($"【CustomerController】 -> CreateOrder() -> meal_id_str: {meal_id_str}, name: {name}, amount: {amount}");
                    if (meal_id_str.IsNullOrEmpty() || name.IsNullOrEmpty() || amount <= 0)
                    {
                        return BadRequest(new { message = "訂單建立失敗: 錯誤的餐點資訊!" });
                    }
                    Guid meal_id = Guid.Parse(meal_id_str);

                    //檢查餐點庫存
                    var find_meal = await _dbContext.Meal
                        .Where(m => m.meal_id == meal_id && m.name == name)
                        .Include(m => m.Inventory)
                        .FirstOrDefaultAsync();
                    Console.WriteLine($"【CustomerController】 -> CreateOrder() -> find_meal: {Services.JsonServices.ToJson(find_meal)}");

                    if (find_meal == null)
                    {
                        return NotFound(new { message = "訂單建立失敗: 找不到對應餐點!" });
                    }

                    Console.WriteLine($"【CustomerController】 -> CreateOrder() -> 餐點庫存更新前 find_meal.Inventory.quantity: {find_meal.Inventory.quantity}, amount: {amount}");
                    if (find_meal.Inventory.quantity <= 0 || find_meal.Inventory.quantity < amount)
                    {
                        return BadRequest(new { message = $"訂單建立失敗: 餐點 {find_meal.name} 庫存不足!" });
                    }

                    orderTotal += (find_meal.price * amount); //統計總價
                    Console.WriteLine($"【CustomerController】 -> CreateOrder() -> orderTotal: {orderTotal}");

                    //更新中間表
                    var order_meal = new Order_MealModel
                    {
                        order_meal_id = 0,
                        amount = amount,
                        order_id = newOrder.order_id,
                        meal_id = meal_id,
                    };
                    Console.WriteLine($"【CustomerController】 -> CreateOrder() -> order_meal: {Services.JsonServices.ToJson(order_meal)}");

                    order_meals.Add(order_meal);

                    find_meal.Inventory.quantity -= amount; //更新餐點庫存
                    Console.WriteLine($"【CustomerController】 -> CreateOrder() -> 餐點庫存更新後 find_meal.Inventory.quantity: {find_meal.Inventory.quantity}");
                }

                newOrder.total = orderTotal; //將統計的總價加入訂單
                Console.WriteLine($"【CustomerController】 -> CreateOrder() -> newOrder.total: {newOrder.total}");
                Console.WriteLine($"【CustomerController】 -> CreateOrder() -> final newOrder: {Services.JsonServices.ToJson(newOrder)}, order_meals: {Services.JsonServices.ToJson(order_meals)}");

                //將訂單物件加入資料庫
                await _dbContext.Order.AddAsync(newOrder);
                await _dbContext.Order_Meal.AddRangeAsync(order_meals);

                //儲存變更
                await _dbContext.SaveChangesAsync();

                //取得訂單資料
                var orderData = await _dbContext.Order
                    .Where(o => o.order_id == newOrder.order_id)
                    .Include(o => o.Order_Meal)
                    .ThenInclude(om => om.Meal)
                    .Select(o => new
                    {
                        meals = o.Order_Meal.Select(om => new
                        {
                            name = om.Meal.name,
                            amount = om.amount,
                        }).ToList(),
                    })
                    .FirstOrDefaultAsync();
                Console.WriteLine($"【CustomerController】 -> CustomerController() -> orderData: {Services.JsonServices.ToJson(orderData)}");

                return Ok(new {success = true, message = "訂購成功!", o_id = newOrder.order_id, o_pay = newOrder.payment, o_t = newOrder.total, o_data = orderData});
            } catch (Exception ex)
            {
                Console.WriteLine("\n-----【ERROR】-----\n");
                Console.WriteLine("【CustomerController】 -> CustomerController() -> 伺服器錯誤: " + ex.Message);
                Console.WriteLine("\n-------------------\n");
                return StatusCode(500, new { message = $"伺服器錯誤: {ex.Message}" });
            }
            finally
            {
                Console.WriteLine("======【ConnectionEnd: CustomerController -> CreateOrder()】======");
            }
        }
    }
}

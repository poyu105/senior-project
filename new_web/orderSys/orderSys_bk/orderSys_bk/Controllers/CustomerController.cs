using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using orderSys_bk.Data;
using orderSys_bk.Model.Dto;
using senior_project_web.Models;

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
        public async Task<IActionResult> CreateOrder(CreateOrderDto order)
        {
            try
            {
                if(order == null)
                {
                    return BadRequest(new { message = "訂單建立失敗: 錯誤的點餐資訊!" });
                }

                //確認是否登入
                string userid = ""; //登入者的id
                if(order.user_id.IsNullOrEmpty())
                {
                    userid = "guest";
                }
                else
                {
                    userid = order.user_id;
                }

                //確認付款方式
                if (order.payment.IsNullOrEmpty())
                {
                    return BadRequest(new { message = "訂單建立失敗: 請選擇付款方式!" });
                }

                //創造訂單物件
                var newOrder = new OrderModel
                {
                    order_id = GenerateRandomOrderId(_dbContext),
                    date = DateTime.Now,
                    weather_condition = null,
                    season = "N",
                    payment = order.payment,
                    total = 0,
                    user_id = userid,
                };

                int orderTotal = 0; //訂單的總價格

                //創造訂單餐點物件
                var order_meals = new List<Order_MealModel>();
                foreach (var meal in order.orders)
                {
                    //檢查餐點庫存
                    var find_meal = await _dbContext.Meal
                        .Where(m => m.meal_id == meal.meal_id && m.name == meal.name)
                        .Include(m => m.Inventory)
                        .FirstOrDefaultAsync();

                    if(find_meal == null)
                    {
                        return NotFound(new { message = "訂單建立失敗: 找不到對應餐點!" });
                    }
                    if (find_meal.Inventory.quantity <= 0)
                    {
                        return BadRequest(new { message = $"訂單建立失敗: 餐點 {find_meal.name} 庫存不足!" });
                    }

                    orderTotal += (find_meal.price * meal.amount); //統計總價

                    //更新中間表
                    var order_meal = new Order_MealModel
                    {
                        order_meal_id = 0,
                        amount = meal.amount,
                        order_id = newOrder.order_id,
                        meal_id = meal.meal_id,
                    };
                    order_meals.Add(order_meal);
                }

                newOrder.total = orderTotal; //將統計的總價加入訂單

                //將訂單物件加入資料庫
                await _dbContext.Order.AddAsync(newOrder);
                await _dbContext.Order_Meal.AddRangeAsync(order_meals);

                //更新餐點庫存
                foreach (var meal in order.orders)
                {
                    var find_meal = await _dbContext.Meal
                        .Where(m => m.meal_id == meal.meal_id && m.name == meal.name)
                        .Include(m => m.Inventory)
                        .FirstOrDefaultAsync();
                    if (find_meal == null)
                    {
                        return NotFound(new { message = "訂單建立失敗: 找不到對應的餐點!" });
                    }
                    find_meal.Inventory.quantity -= meal.amount;
                }

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

                return Ok(new {message = "訂購成功!", o_id = newOrder.order_id, o_pay = newOrder.payment, o_t = newOrder.total, o_data = orderData});
            } catch (Exception ex)
            {
                return StatusCode(500, new { message = $"伺服器錯誤: {ex.Message}" });
            }
        }
    }
}

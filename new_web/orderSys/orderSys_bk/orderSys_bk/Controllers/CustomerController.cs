using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using orderSys_bk.Data;
using orderSys_bk.Model.Dto;
using senior_project_web.Models;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace orderSys_bk.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly OrderSysDbContext _dbContext;

        private readonly IDbConnection _dbConnection;
        public CustomerController(OrderSysDbContext dbContext)
        {
            _dbContext = dbContext;
            _dbConnection = dbContext.Database.GetDbConnection();
        }

        private readonly HttpClient _httpClient = new HttpClient();

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
                Console.WriteLine("\n=====【ConnectionStart: CustomerController -> GetMeals()】=====\n");

                String user_id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "guest"; //從JWT取得user_id
                Console.WriteLine($"【CustomerController】 -> GetMeals() -> user_id: {user_id}");

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
                Console.WriteLine($"【CustomerController】 -> GetMeals() -> meals: {Services.JsonServices.ToJson(meals)}");

                if (meals == null)
                {
                    return BadRequest(new { message = "找不到餐點" });
                }
                return Ok(new { success = true, meals = meals });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"錯誤: {ex.Message}" });
            }
            finally
            {
                Console.WriteLine("\n======【ConnectionEnd: CustomerController -> GetMeals()】======\n");
            }
        }

        //處理點餐
        [HttpPost("createOrder")]
        public async Task<IActionResult> CreateOrder(Dictionary<String, Object> req)
        {
            try
            {
                Console.WriteLine("\n=====【ConnectionStart: CustomerController -> CreateOrder()】=====\n");
                Console.WriteLine($"【CustomerController】 -> CreateOrder() -> 處理點餐: req: {Services.JsonServices.ToJson(req)}");
                if (req == null || req.Count == 0)
                {
                    return BadRequest(new { success = false, message = "訂單建立失敗: 請提供點餐資訊!" });
                }
                String user_id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "guest"; //從JWT取得user_id
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
                    date = orderDate,//cusDateTime,
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

                return Ok(new { success = true, message = "訂購成功!", o_id = newOrder.order_id, o_pay = newOrder.payment, o_t = newOrder.total, o_data = orderData });
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n-----【ERROR】-----\n");
                Console.WriteLine("【CustomerController】 -> CustomerController() -> 伺服器錯誤: " + ex.Message);
                Console.WriteLine("\n-------------------\n");
                return StatusCode(500, new { message = $"伺服器錯誤: {ex.Message}" });
            }
            finally
            {
                Console.WriteLine("\n======【ConnectionEnd: CustomerController -> CreateOrder()】======\n");
            }
        }

        //處理推薦餐點
        [HttpPost("getRecommendMeals")]
        public async Task<IActionResult> getRecommendMeals(Dictionary<String, Object> req)
        {
            try
            {
                Console.WriteLine("\n=====【ConnectionStart: CustomerController -> RecommendMeals()】=====\n");
                Console.WriteLine($"【CustomerController】 -> RecommendMeals() -> 處理推薦餐點: req: {Services.JsonServices.ToJson(req)}");
                if (req == null || req.Count == 0)
                {
                    return BadRequest(new { success = false, message = "推薦餐點失敗: 請提供資訊!" });
                }

                String user_id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "guest"; //從JWT取得user_id
                String dateStr = req.ContainsKey("date") ? req["date"]?.ToString() : null; //日期
                Dictionary<String, Object> location = req.ContainsKey("location") ? Services.JsonServices.ToDictionary(req["location"]) as Dictionary<String, Object> : null; //位置資訊
                Console.WriteLine($"【CustomerController】 -> RecommendMeals() -> user_id: {user_id}, dateStr: {dateStr}, location: {Services.JsonServices.ToJson(location)}");

                if (user_id.IsNullOrEmpty() || dateStr.IsNullOrEmpty() || location == null)
                {
                    return BadRequest(new { success = false, message = "推薦餐點失敗: 錯誤的資訊!" });
                }

                //將字串轉成日期
                if (!DateTime.TryParse(dateStr, out DateTime parsedDate))
                {
                    return BadRequest(new { success = false, message = "日期格式錯誤，請使用YYYY-MM-DD格式" });
                }

                List<Dictionary<String, Object>> meals = new List<Dictionary<String, Object>>();
                if (user_id != "guest")
                {
                    String season = Services.WeatherService.getSeason(parsedDate.Month); //取得當前季節
                    String latitudeStr = location.ContainsKey("latitude") ? location["latitude"]?.ToString() : null; //緯度
                    String longitudeStr = location.ContainsKey("longitude") ? location["longitude"]?.ToString() : null; //經度
                    Console.WriteLine($"【CustomerController】 -> RecommendMeals() -> latitudeStr: {latitudeStr}, longitudeStr: {longitudeStr}");

                    if (latitudeStr.IsNullOrEmpty() || longitudeStr.IsNullOrEmpty())
                    {
                        return BadRequest(new { success = false, message = "推薦餐點失敗: 錯誤的地理位置資訊!" });
                    }

                    double latitude = double.Parse(latitudeStr); //緯度轉double
                    double longitude = double.Parse(longitudeStr); //經度轉double
                    String weatherCondition = await Services.WeatherService.GetWeatherForecastAsync(dateStr, latitude, longitude); //取得天氣狀況
                    if (weatherCondition == "N")
                    {
                        return BadRequest(new { success = false, message = "無法取得預測日期的天氣狀況(未知)" });
                    }
                    else if (string.IsNullOrEmpty(weatherCondition))
                    {
                        return BadRequest(new { success = false, message = "無法取得預測日期的天氣狀況" });
                    }

                    String baseOrderDataSql =
                        @"
                            with DailyMeals as (
                                select
                                    convert(varchar(8), o.date, 112) as order_date,  -- 同一天
                                    o.user_id,
                                    m.meal_id,
                                    m.name,
                                    m.type,
                                    sum(om.amount) as total_amount
                                from [Order] o
                                inner join [Order_Meal] om on o.order_id = om.order_id
                                inner join [Meal] m on om.meal_id = m.meal_id
                                where o.user_id = @user_id
                                group by
                                    convert(varchar(8), o.date, 112),
                                    o.user_id,
                                    m.meal_id,
                                    m.name,
                                    m.type
                            ),
                            Last5Dates as (
                                select top 5 order_date
                                from DailyMeals
                                group by order_date
                                order by order_date desc
                            )
                        ";

                    String lastFiveDateSql = baseOrderDataSql +
                        @"
                            select count(*) as date_count
                            from (select distinct order_date from DailyMeals) as DistinctDates
                            where order_date in (select order_date from Last5Dates);
                        ";

                    var lastFiveDateResult = await _dbConnection.QueryFirstOrDefaultAsync(lastFiveDateSql, new { user_id });
                    Console.WriteLine($"【CustomerController】 -> RecommendMeals() -> lastFiveDateResult: {Services.JsonServices.ToJson(lastFiveDateResult)}");
                    int date_count = lastFiveDateResult != null ? Services.JsonServices.ToInt(lastFiveDateResult.date_count, 0) : 0;
                    Console.WriteLine($"【CustomerController】 -> RecommendMeals() -> date_count: {date_count}");
                    if (date_count >= 5)
                    {
                        String userOrderedSql = baseOrderDataSql +
                            @"
                            select *
                            from DailyMeals
                            where order_date in (select order_date from Last5Dates)
                            order by order_date desc, name;
                        ";

                        var userOrderedResult = await _dbConnection.QueryAsync(userOrderedSql, new { user_id });
                        Console.WriteLine($"【CustomerController】 -> RecommendMeals() -> userOrderedResult: {Services.JsonServices.ToJson(userOrderedResult)}");

                        List<Dictionary<String, object>> orders = userOrderedResult.Select(r => new Dictionary<string, object>
                        {
                            { "order_date", r.order_date },
                            { "user_id", r.user_id },
                            { "meal_id", r.meal_id },
                            { "name", r.name },
                            { "type", r.type },
                            { "total_amount", r.total_amount },
                        }).ToList(); //使用者近期(5次)點過的餐點

                        List<Dictionary<String, object>> dataForRecommend = new List<Dictionary<string, object>> { 
                            new Dictionary<string, object> {
                                { "date", dateStr },
                                { "weather_condition", weatherCondition },
                                { "season", season },
                                { "orders", orders }
                            }
                        };
                        Console.WriteLine($"【CustomerController】 -> RecommendMeals() -> dataForRecommend: {Services.JsonServices.ToJson(dataForRecommend)}");

                        List<Dictionary<String, Object>> recommendResult = new List<Dictionary<String, Object>>();
                        recommendResult = await CallPythonRecommendAsync(dataForRecommend); //呼叫python進行餐點推薦
                        Console.WriteLine($"【CustomerController】 -> RecommendMeals() -> 餐點推薦結果: {System.Text.Json.JsonSerializer.Serialize(recommendResult)}");

                        if (recommendResult == null || recommendResult.Count == 0)
                        {
                            return BadRequest(new { success = false, message = "推薦餐點失敗: 無法取得推薦結果!" });
                        }

                        foreach (var item in recommendResult)
                        {
                            if (item.ContainsKey("meal_id") && item["meal_id"] != null)
                            {
                                String meal_id_str = item["meal_id"].ToString();
                                Console.WriteLine($"【CustomerController】 -> RecommendMeals() -> 查詢推薦餐點的價格與圖片: meal_id_str: {meal_id_str}");
                                if (!meal_id_str.IsNullOrEmpty())
                                {
                                    Guid meal_id = Guid.Parse(meal_id_str);
                                    var meal = await _dbContext.Meal
                                        .Where(m => m.meal_id == meal_id)
                                        .Select(m => new Dictionary<string, object>
                                        {
                                            { "price", m.price },
                                            { "img_path", m.img_path },
                                        })
                                        .FirstOrDefaultAsync();
                                    if (meal != null)
                                    {
                                        item.Add("price", meal["price"]);
                                        item.Add("img_path", meal["img_path"]);
                                        Console.WriteLine($"【CustomerController】 -> RecommendMeals() -> 推薦餐點加入價格與圖片後 item: {Services.JsonServices.ToJson(item)}");
                                    }
                                }
                            }
                        }

                        meals = recommendResult; //使用者近期點餐次數多於5次，回傳推薦餐點
                    }
                    else
                    {
                        //使用者近期點餐次數少於5次，回傳近期熱銷餐點
                        meals = await recommendForGuest();
                    }
                }
                else
                {
                    meals = await recommendForGuest();
                }

                return Ok(new { success = true, meals = meals });
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n-----【ERROR】-----\n");
                Console.WriteLine("【CustomerController】 -> RecommendMeals() -> 伺服器錯誤: " + ex.Message);
                Console.WriteLine("\n-------------------\n");
                return StatusCode(500, new { success = false, message = $"伺服器錯誤: {ex.Message}" });
            }
            finally
            {
                Console.WriteLine("\n======【ConnectionEnd: CustomerController -> RecommendMeals()】======\n");
            }
        }

        //訪客推薦餐點(近期熱銷餐點)
        private async Task<List<Dictionary<String, Object>>> recommendForGuest()
        {
            //如果是訪客就回傳過去7天的銷量排行
            DateTime startDate = DateTime.Now.AddDays(-7); //取得7天前的日期
            String sql =
                @"select 
	                dense_rank() over (order by sum(om.amount) desc) as rank,
                    m.meal_id, 
                    m.name, 
                    m.description, 
                    m.type, 
                    m.price, 
                    m.img_path
                from [Order] as o
                left join [Order_Meal] as om on o.order_id = om.order_id
                left join [Meal] as m on om.meal_id = m.meal_id
                where o.date >= @StartDate
                group by m.meal_id, m.name, m.description, m.type, m.price, m.img_path
                order by sum(om.amount) desc;";

            var result = await _dbConnection.QueryAsync(sql, new { startDate });
            Console.WriteLine($"【CustomerController】 -> recommondForGuest() -> 餐點銷量排行 result: {Services.JsonServices.ToJson(result)}");

            return result.Select(r => new Dictionary<string, object>
                   {
                       {"rank", r.rank },
                       { "meal_id", r.meal_id },
                       { "name", r.name },
                       { "description", r.description },
                       { "type", r.type },
                       { "price", r.price },
                       { "img_path", r.img_path },
                   }).ToList(); //近期(7天)熱銷餐點
        }

        //python推薦餐點
        private async Task<List<Dictionary<String, Object>>> CallPythonRecommendAsync(List<Dictionary<String, Object>> orderData)
        {
            Console.WriteLine($"【CustomerController】 -> CallPythonRecommendAsync() -> 呼叫Python進行餐點推薦: orderData: {System.Text.Json.JsonSerializer.Serialize(orderData)}");

            try
            {
                var response = await _httpClient.PostAsJsonAsync("http://127.0.0.1:5000/recommend", orderData);
                Console.WriteLine($"【CustomerController】 -> CallPythonRecommendAsync() -> 呼叫Python進行餐點推薦: response: {response}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<Dictionary<String, Object>>();
                    List<Dictionary<String, Object>> meals = result != null && result.ContainsKey("orders") ? Services.JsonServices.ToListOfDictionary(result["orders"]) as List<Dictionary<String, Object>> : new List<Dictionary<String, Object>>();
                    Console.WriteLine($"【CustomerController】 -> CallPythonRecommendAsync() -> 呼叫Python進行餐點推薦: meals: {Services.JsonServices.ToJson(meals)}");
                    return meals;
                }
                else
                {
                    throw new Exception("Python餐點推薦服務回傳錯誤狀態碼: " + response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("呼叫Python餐點推薦失敗: " + ex.Message);
            }
        }
    }
}

using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using orderSys_bk.Data;
using orderSys_bk.Model.Dto;
using senior_project_web.Models;
using System.Data;

namespace orderSys_bk.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly OrderSysDbContext _dbContext;

        private readonly IDbConnection _dbConnection;
        public AdminController(OrderSysDbContext dbContext)
        {
            _dbContext = dbContext;
            _dbConnection = dbContext.Database.GetDbConnection();
        }

        private readonly HttpClient _httpClient = new HttpClient();

        //取得庫存資料
        [HttpGet("getInventory")]
        public async Task<IActionResult> GetInventory()
        {
            try
            {
                var inventory = await _dbContext.Meal
                    .Include(m=>m.Inventory)
                    .Select(m => new
                    {
                        m.img_path,
                        m.name,
                        m.type,
                        m.description,
                        m.cost,
                        m.price,
                        m.Inventory.quantity,
                        id = m.inventory_id,
                    })
                    .ToListAsync();

                return Ok(inventory);
            } catch (Exception ex)
            {
                return BadRequest(new { message = "伺服器錯誤: " + ex.Message });
            }
        }

        //新增庫存
        [HttpPost("addInventory")]
        public async Task<IActionResult> AddInventory([FromBody] UpdateMealDto inventoryData)
        {
            try
            {
                if (inventoryData == null)
                {
                    return BadRequest(new { message = "請提供庫存資料" });
                }

                var meal = await _dbContext.Meal
                    .FirstOrDefaultAsync(m => m.name == inventoryData.name);
                if (meal != null)
                {
                    return BadRequest(new { message = "重複的餐點名稱!" });
                }

                // 解析 base64 字串（格式: data: image / png; base64,...）
                var base64Data = inventoryData.img_path;
                var base64Parts = base64Data.Split(',');
                if (base64Parts.Length != 2)
                {
                    return BadRequest(new { message = "圖片格式不正確!" });
                }

                var imageBytes = Convert.FromBase64String(base64Parts[1]);

                // 產生唯一檔名
                var fileName = Guid.NewGuid().ToString() + ".jpg";
                var relativePath = Path.Combine("images", fileName);
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);

                // 確保資料夾存在
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                // 寫入檔案
                System.IO.File.WriteAllBytes(fullPath, imageBytes);

                //新增庫存
                var newInventory = new InventoryModel
                {
                    inventory_id = Guid.NewGuid(),
                    quantity = inventoryData.quantity,
                };

                //新增餐點
                var newMeal = new MealModel
                {
                    meal_id = Guid.NewGuid(),
                    name = inventoryData.name,
                    type = inventoryData.type,
                    description = inventoryData.description,
                    cost = inventoryData.cost,
                    price = inventoryData.price,
                    img_path = relativePath,
                    inventory_id = newInventory.inventory_id,
                };

                //將庫存和餐點加入資料庫
                _dbContext.Inventory.Add(newInventory);
                _dbContext.Meal.Add(newMeal);
                await _dbContext.SaveChangesAsync();

                return Ok(new { success = true, message = "庫存新增成功" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "伺服器錯誤: " + ex.Message });
            }
        }

        //刪除庫存
        [HttpDelete("deleteInventory/{id}")]
        public async Task<IActionResult> DeleteInventory([FromRoute] Guid id)
        {
            try
            {
                //檢查庫存是否存在
                var inventory = await _dbContext.Inventory.FindAsync(id);
                if (inventory == null)
                {
                    return NotFound(new { message = "找不到該庫存" });
                }

                //檢查庫存是否有關聯的餐點
                var meal = await _dbContext.Meal
                    .FirstOrDefaultAsync(m => m.inventory_id == id);
                if (meal != null)
                {
                    _dbContext.Meal.Remove(meal);
                }

                _dbContext.Inventory.Remove(inventory);
                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "庫存刪除成功" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "伺服器錯誤: " + ex.Message });
            }
        }

        //編輯庫存
        [HttpPut("editInventory")]
        public async Task<IActionResult> EditInventory([FromBody] UpdateMealDto inventoryData)
        {
            try
            {
                Console.WriteLine("進入控制器");
                if(inventoryData == null)
                {
                    return BadRequest(new { message = "錯誤的庫存資料" });
                }
                var find_meal = await _dbContext.Meal.Where(m => m.inventory_id == inventoryData.id)
                    .Include(m=>m.Inventory)
                    .FirstOrDefaultAsync();
                if (find_meal == null)
                {
                    return NotFound(new { message = "找不到對應的資料" });
                }

                // 解析 base64 字串（格式: data: image / png; base64,...）
                var base64Data = inventoryData.img_path;
                var base64Parts = base64Data.Split(',');
                if (base64Parts.Length != 2)
                {
                    return BadRequest(new { message = "圖片格式不正確!" });
                }

                var imageBytes = Convert.FromBase64String(base64Parts[1]);

                // 產生唯一檔名
                var fileName = Guid.NewGuid().ToString() + ".jpg";
                var relativePath = Path.Combine("images", fileName);
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);

                // 確保資料夾存在
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                // 寫入檔案
                System.IO.File.WriteAllBytes(fullPath, imageBytes);

                find_meal.name = inventoryData.name;
                find_meal.type = inventoryData.type;
                find_meal.description = inventoryData.description;
                find_meal.cost = inventoryData.cost;
                find_meal.price = inventoryData.price;
                find_meal.img_path = relativePath;
                find_meal.Inventory.quantity = inventoryData.quantity;

                await _dbContext.SaveChangesAsync();
                return Ok(new { success = true, message = "修改成功!" });
            } catch (Exception ex)
            {
                return StatusCode(500, new { message = $"伺服器發生錯誤:{ex.Message}" });
            }
        }

        //取得銷售狀況資料
        [HttpGet("getSales")]
        public async Task<IActionResult> GetSales()
        {
            try
            {
                //今天日期
                var today = DateTime.Today;
                //明天日期
                var tomorrow = today.AddDays(1);

                //取得所有今日訂單
                var orders = await _dbContext.Order
                    .Where(o => o.date >= today && o.date < tomorrow)
                    .Select(o => o.order_id)
                    .ToListAsync();

                //由訂單編號查詢所有對應餐點及數量
                var salesSummary = await _dbContext.Meal
                    .GroupJoin(
                        _dbContext.Order_Meal
                            .Where(om => orders.Contains(om.order_id)),
                        m => m.meal_id,
                        om => om.meal_id,
                        (m, oms) => new
                        {
                            meal_id = m.meal_id.ToString(),
                            meal_name = m.name,
                            amount = oms.Sum(x => x.amount),
                            sales = m.price * oms.Sum(x => x.amount),
                        }
                    )
                    .ToListAsync();

                if (salesSummary.IsNullOrEmpty())
                {
                    return BadRequest(new { success = false, message = "無法取得銷售資料，請聯繫系統管理員!" });
                }

                return Ok(new {success = true, data = salesSummary});
            } catch (Exception ex) {
                return StatusCode(500, new { message = $"伺服器錯誤: {ex.Message}" });
            }
        }

        /// <summary>
        /// 呼叫python進行銷售預測
        /// </summary>
        /// <param name="salesReportsFromDB">過去10天的銷售紀錄[{第X天、餐點id、餐點類型、天氣狀況、季節、銷售數量}]</param>
        /// <param name="predictionData">預測當天日期、天氣狀況、季節</param>
        /// <returns>預測銷售資料</returns>
        private async Task<List<Dictionary<String, Object>>> CallPythonPredictionAsync(List<Dictionary<String, Object>> salesReportsFromDB, Dictionary<String, Object> predictionData)
        {
            Console.WriteLine($"【AdminController】 -> CallPythonPredictionAsync() -> 呼叫Python進行銷售預測: salesReportsFromDB: {System.Text.Json.JsonSerializer.Serialize(salesReportsFromDB)}, predictionData: {System.Text.Json.JsonSerializer.Serialize(predictionData)}");
            var payload = new
            {
                sales_data = salesReportsFromDB,
                prediction_data = predictionData
            };
            Console.WriteLine($"【AdminController】 -> CallPythonPredictionAsync() -> 呼叫Python進行銷售預測: payload: {System.Text.Json.JsonSerializer.Serialize(payload)}");

            try
            {
                var response = await _httpClient.PostAsJsonAsync("http://127.0.0.1:5000/face-recognition", payload);
                Console.WriteLine($"【AdminController】 -> CallPythonPredictionAsync() -> 呼叫Python進行銷售預測: response: {response}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<List<Dictionary<String, Object>>>();
                    Console.WriteLine($"【AdminController】 -> CallPythonPredictionAsync() -> 呼叫Python進行銷售預測: result: {System.Text.Json.JsonSerializer.Serialize(result)}");
                    return result;
                }
                else
                {
                    throw new Exception("Python銷售預測服務回傳錯誤狀態碼: " + response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("呼叫Python進行銷售預測失敗: " + ex.Message);
            }
        }

        //取得銷售預測資料
        [HttpPost("getPrediction")]
        public async Task<IActionResult> GetPrediction([FromBody] Dictionary<String, Object> req)
        {
            try
            {
                Console.WriteLine("=====【ConnectionStart: AdminController -> GetPrediction()】=====");
                if (req == null || !req.ContainsKey("date") || !req.ContainsKey("latitude") || !req.ContainsKey("longitude"))
                {
                    return BadRequest(new { success = false, message = "請提供正確的預測資料!" });
                }

                String predictionDateStr = req["date"].ToString() ?? ""; //預測日期
                String latitudeStr = req["latitude"].ToString() ?? "";
                String longitudeStr = req["longitude"].ToString() ?? "";

                double latitude = Convert.ToDouble(latitudeStr); //緯度
                double longitude = Convert.ToDouble(longitudeStr); //經度

                Console.WriteLine($"【AdminController】 -> GetPrediction() -> 銷售預測資料: 預測日期={predictionDateStr}, 緯度={latitude}, 經度={longitude}");

                if (String.IsNullOrEmpty(predictionDateStr))
                {
                    return BadRequest(new { success = false, message = "請提供預測日期" });
                }

                //將字串轉成日期
                if (!DateTime.TryParse(predictionDateStr, out DateTime parsedDate))
                {
                    return BadRequest(new { success = false, message = "日期格式錯誤，請使用YYYY-MM-DD格式" });
                }

                DateTime today = DateTime.Today; //今天日期
                Console.WriteLine($"AdminController -> GetPrediction() -> 今天日期: {today.ToString("yyyy-MM-dd")}, 預測日期: {parsedDate.ToString("yyyy-MM-dd")}");
                //可預測範圍: 今天~3天內
                if (parsedDate < today)
                {
                    return BadRequest(new { success = false, message = "預測日期不可早於今天" });
                }else if((parsedDate - today).TotalDays >= 3)
                {
                    return BadRequest(new { success = false, message = "預測日期不可超過3天" });
                }

                //取得過去10天的銷售紀錄(從今天日期開始往前推)
                var startDate = today.AddDays(-10).ToString("yyyyMMdd");
                var endDate = today.AddDays(-1).ToString("yyyyMMdd");
                Console.WriteLine($"【AdminController】 -> GetPrediction() -> 查詢過去10天銷售紀錄: {startDate} ~ {endDate}");

                if (_dbConnection.State != ConnectionState.Open)
                {
                    await ((SqlConnection)_dbConnection).OpenAsync();
                }

                var sql =
                    @" 
                        ;WITH DateRange AS (
                            SELECT CAST(@startDate AS DATE) AS DateValue
                            UNION ALL
                            SELECT DATEADD(DAY, 1, DateValue)
                            FROM DateRange
                            WHERE DateValue < @endDate
                        ),
                        DailyMealSales AS (
                            SELECT 
                                CONVERT(nvarchar(8), o.date, 112) AS date,
                                om.meal_id,
                                SUM(om.amount) AS amount,
                                MAX(o.weather_condition) AS weatherCondition,
                                MAX(o.season) AS season
                            FROM [Order] o
                            INNER JOIN Order_Meal om ON o.order_id = om.order_id
                            WHERE CONVERT(nvarchar(8), o.date, 112) BETWEEN @startDate AND @endDate
                            GROUP BY CONVERT(nvarchar(8), o.date, 112), om.meal_id
                        )
                        SELECT 
                            CONVERT(nvarchar(8), d.DateValue, 112) AS date,
                            m.meal_id,
                            m.name,
                            m.type,
                            ISNULL(dms.amount, 0) AS amount,
                            dms.weatherCondition,
                            dms.season
                        FROM DateRange d
                        CROSS JOIN Meal m
                        LEFT JOIN DailyMealSales dms
                            ON dms.meal_id = m.meal_id
                            AND dms.date = CONVERT(nvarchar(8), d.DateValue, 112)
                        ORDER BY d.DateValue, m.name
                        OPTION (MAXRECURSION 0);
                    ";
                var result = await _dbConnection.QueryAsync(sql, new { startDate, endDate });
                List<Dictionary<String,Object>> salesReportsFromDB = result.Select(r => new Dictionary<String, Object>
                {
                    { "date", r.date },
                    { "meal_id", r.meal_id },
                    { "name", r.name },
                    { "type", r.type },
                    { "amount", r.amount },
                    { "weather", Services.StringServices.TrimSpaces(r.weatherCondition) },
                    { "season", r.season }
                }).ToList();
                Console.WriteLine($"【AdminController】 -> GetPrediction() -> 取得過去10天銷售紀錄: {salesReportsFromDB.Count} 筆");
                Console.WriteLine($"【AdminController】 -> GetPrediction() -> 過去10天銷售紀錄: {System.Text.Json.JsonSerializer.Serialize(salesReportsFromDB)}");

                //if (salesReportsFromDB.IsNullOrEmpty())
                //{
                //    return BadRequest(new { success = false, message = "無法取得過去10天銷售紀錄!" });
                //}

                string weatherCondition = Services.StringServices.TrimSpaces(await Services.WeatherService.GetWeatherForecastAsync(predictionDateStr, latitude, longitude)); //取得預測日期的天氣狀況
                Console.WriteLine($"【AdminController】 -> GetPrediction() -> 預測日期的天氣狀況: {weatherCondition}");

                //if (weatherCondition == "N")
                //{
                //    return BadRequest(new { success = false, message = "無法取得預測日期的天氣狀況(未知)" });
                //}else if(string.IsNullOrEmpty(weatherCondition))
                //{
                //    return BadRequest(new { success = false, message = "無法取得預測日期的天氣狀況" });
                //}

                Console.WriteLine($"【AdminController】 -> GetPrediction() -> 預測日期的月份: {parsedDate.Month}");
                if (parsedDate.Month < 1 || parsedDate.Month > 12)
                {
                    return BadRequest(new { success = false, message = "月份錯誤，請提供正確的月份" });
                }

                //對應季節 win:冬季(12,1,2) spr:春季(3,4,5) sum:夏季(6,7,8) aut:秋季(9,10,11)
                string season = Services.WeatherService.getSeason(parsedDate.Month);
                Console.WriteLine($"【AdminController】 -> GetPrediction() -> 預測日期的季節: {season}");

                //預測資料
                Dictionary<String, Object> predictionData = new Dictionary<String, Object>
                {
                    { "date", predictionDateStr },
                    { "weather", weatherCondition },
                    { "season", season }
                };

                List<Dictionary<String, Object>> predictionResult = new List<Dictionary<String, Object>>();
                predictionResult = await CallPythonPredictionAsync(salesReportsFromDB, predictionData); //呼叫python進行銷售預測
                Console.WriteLine($"【AdminController】 -> GetPrediction() -> 銷售預測結果: {System.Text.Json.JsonSerializer.Serialize(predictionResult)}");

                return Ok(new { success = true, data = predictionResult } );
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n-----【ERROR】-----\n");
                Console.WriteLine("【AdminController】 -> GetPrediction() -> 伺服器錯誤: " + ex.Message);
                Console.WriteLine("\n-------------------\n");
                return StatusCode(500, new { success = false, message = $"伺服器錯誤: {ex.Message}" });
            }
            finally
            {
                Console.WriteLine("======【ConnectionEnd: AdminController -> GetPrediction()】======");
            }
        }
    }
}

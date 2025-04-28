using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using orderSys_bk.Data;
using orderSys_bk.Model.Dto;
using senior_project_web.Models;
using static System.Net.Mime.MediaTypeNames;
using System.Buffers.Text;
using System.Net.NetworkInformation;

namespace orderSys_bk.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly OrderSysDbContext _dbContext;
        public AdminController(OrderSysDbContext dbContext)
        {
            _dbContext = dbContext;
        }

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

                find_meal.name = inventoryData.name;
                find_meal.type = inventoryData.type;
                find_meal.description = inventoryData.description;
                find_meal.cost = inventoryData.cost;
                find_meal.price = inventoryData.price;
                find_meal.img_path = inventoryData.img_path;
                find_meal.Inventory.quantity = inventoryData.quantity;

                await _dbContext.SaveChangesAsync();
                return Ok(new { success = true, message = "修改成功!" });
            } catch (Exception ex)
            {
                return StatusCode(500, new { message = $"伺服器發生錯誤:{ex.Message}" });
            }
        }
    }
}

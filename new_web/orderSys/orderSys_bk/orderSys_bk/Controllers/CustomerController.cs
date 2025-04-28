using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using orderSys_bk.Data;

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
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using senior_project_web.Data;
using senior_project_web.Models;
using System.Diagnostics;

namespace senior_project_web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly OrderSystemDbContext _context;

        public HomeController(ILogger<HomeController> logger, OrderSystemDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            var meals = _context.Meal.ToList();
            return View(meals);
        }

        [HttpGet]
        [Route("meals/{meal_id}")]
        public async Task<IActionResult> GetMealInfo(Guid meal_id)
        {
            var meal = await _context.Meal.FirstOrDefaultAsync(m => m.meal_id == meal_id);
            if (meal == null)
            {
                return NotFound(new { message = "找無餐點資訊" });
            }
            return Ok(meal);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

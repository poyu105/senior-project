using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using senior_project_web.Data;
using senior_project_web.Models;
using System.Security.Claims;

namespace senior_project_web.Controllers
{
    public class AdminController : Controller
    {
        private readonly OrderSystemDbContext _context;

        public AdminController(OrderSystemDbContext orderSystemDbContext)
        {
            _context = orderSystemDbContext;
        }

        //管理員首頁
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            //判斷使否已有登入
            if(!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login","Auth"); //如果沒有用戶登入，則導航至AuthController, Login方法
            }

            var admin_id = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _context.Admin
                .Include(a => a.User)   //包含User資料
                .FirstOrDefaultAsync(u => u.admin_id == admin_id);
            if (user == null)
            {
                return RedirectToAction("Logout","Auth"); // 若找不到Admin資料(無權限)，登出並返回登入頁面
            }

            return View(user);
        }

        //庫存管理
        [HttpGet]
        public async Task<IActionResult> Inventory()
        {
            var meals = await _context.Meal.Include(m => m.Inventory).ToListAsync();
            ViewBag.MealLength = meals.Count();
            return View(meals);
        }

        //新增Meal
        [HttpPost]
        public async Task<IActionResult> AddNewMeal(IFormFile img_path, string name, string type, string description, int cost, int price, int quantity)
        {
            if(img_path == null || name == null || type == null || description == null || cost == null || price == null || quantity == null)
            {
                TempData["errMsg"] = "輸入錯誤，請重新確認!\n"+img_path+name;
                return RedirectToAction("Inventory");
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", img_path.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await img_path.CopyToAsync(stream);
            }

            var newMealInventory = new InventoryModel
            {
                inventory_id = Guid.NewGuid(),
                quantity = quantity
            };
            
            await _context.Inventory.AddAsync(newMealInventory);

            var newMeal = new MealModel
            {
                name = name,
                img_path = "images/" + img_path.FileName,
                type = type,
                description = description,
                cost = cost,
                price = price,
                inventory_id = newMealInventory.inventory_id
            };

            _context.Meal.Add(newMeal);
            await _context.SaveChangesAsync();
            return RedirectToAction("Inventory");
        }

        //刪除Meal
        [HttpPost]
        public async Task<IActionResult> DeleteMeal(Guid meal_id)
        {
            var meal = await _context.Meal.FirstOrDefaultAsync(m => m.meal_id  == meal_id);
            if (meal == null)
            {
                TempData["errMsg"] = "找無餐點資訊!";
                return RedirectToAction("Inventory");
            }
            var inventory = await _context.Inventory.FirstOrDefaultAsync(i => i.inventory_id == meal.inventory_id);
            if (inventory.quantity > 0)
            {
                TempData["errMsg"] = "庫存大於0，無法進行刪除!";
                return RedirectToAction("Inventory");
            }
            // 刪除圖片文件
            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", meal.img_path); 
            Console.WriteLine("img_path ============================= \n"+imagePath);
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }
            _context.Inventory.Remove(inventory);
            _context.Meal.Remove(meal);
            await _context.SaveChangesAsync();
            TempData["errMsg"] = "成功刪除!";

            return RedirectToAction("Inventory");
        }

        //銷售預測
        [HttpGet]
        public IActionResult Prediction()
        {
            return View();
        }
    }
}

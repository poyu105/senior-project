using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using senior_project_web.Data;
using senior_project_web.Models;
using System.Collections.Concurrent;
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
                return RedirectToAction("AdminLogin","Auth"); //如果沒有用戶登入，則導航至AuthController, Login方法
            }

            var admin_id = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _context.Admin
                .Include(a => a.User)   //包含User資料
                .FirstOrDefaultAsync(u => u.admin_id == admin_id);
            if (user == null)
            {
                return RedirectToAction("AdminLogout","Auth"); // 若找不到Admin資料(無權限)，登出並返回登入頁面
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
                TempData["errMsg"] = "輸入錯誤，請重新確認!\n";
                return RedirectToAction("Inventory");
            }

            var uniquePath = Guid.NewGuid() + Path.GetExtension(img_path.FileName); //使用Guid生成唯一路徑
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", uniquePath);
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
                img_path = "images/"+uniquePath,
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

        //編輯Meal
        [HttpPost]
        public async Task<IActionResult> EditMeal(Guid meal_id)
        {
            var meal = await _context.Meal.Include(m => m.Inventory).FirstOrDefaultAsync(m => m.meal_id == meal_id);
            if(meal == null)
            {
                TempData["errMsg"] = "找無餐點資訊!";
                return RedirectToAction("Inventory");
            }
            return View(meal);
        }
        //儲存編輯Meal
        [HttpPost]
        public async Task<IActionResult> SaveChange(Guid meal_id, IFormFile img_path, string name, string type, string description, int cost, int price, int quantity)
        {
            var meal = await _context.Meal.Include(m => m.Inventory).FirstOrDefaultAsync(m => m.meal_id == meal_id);
            if (meal == null)
            {
                TempData["errMsg"] = "找無餐點資訊!";
                return RedirectToAction("Inverntory");
            }
            //刪除舊圖片
            if (img_path != null)
            {
                var oldImgPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", meal.img_path);
                if (System.IO.File.Exists(oldImgPath))
                {
                    System.IO.File.Delete(oldImgPath);
                }
                //重新添加圖片
                var newUniqueImgPath = Guid.NewGuid() + Path.GetExtension(img_path.FileName);
                var newPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", newUniqueImgPath);
                using (var stream = new FileStream(newPath, FileMode.Create))
                {
                    await img_path.CopyToAsync(stream);
                }
                //儲存新路徑
                meal.img_path = "images/" + newUniqueImgPath;
            }
            //檢查描述是否為空值
            if (string.IsNullOrEmpty(description))
            {
                //若為空則保留為源字串
                description = meal.description;
            }
            
            //修改其他資料
            meal.name = name;
            meal.type = type;
            meal.description = description;
            meal.cost = cost;
            meal.price = price;
            meal.Inventory.quantity = quantity;
            meal.Inventory.update_at = DateTime.Now; //更新時間

            await _context.SaveChangesAsync();
            TempData["errMsg"] = "修改成功!";
            return RedirectToAction("Inventory");
        }

        //每日報表
        [HttpGet]
        public async Task<IActionResult> Daily_Sales_Report()
        {
            var reports = await _context.Daily_Sales_Report.Include(r=>r.ReportMeal).ThenInclude(m=>m.Meal).ToListAsync();
            ViewBag.ReportsLength = reports.Count();
            return View(reports);
        }
        //新增報表
        [HttpGet]
        public async Task<IActionResult> addReport()
        {
            var meal = await _context.Meal.ToListAsync();
            ViewBag.MealLength = meal.Count();
            return View(meal);
        }
        //新增報表(POST)
        [HttpPost]
        public async Task<IActionResult> addNewReport(List<Guid> meal_id, List<int> quantity)
        {
            // 驗證所有列表長度是否一致
            if (meal_id.Count != quantity.Count)
            {
                TempData["errMsg"] = "提供的資料不完整，請確認所有欄位的資料後再提交!";
                return RedirectToAction("addReport");
            }
            try
            {
                for (int i = 0; i < meal_id.Count; i++)
                {
                    var meal = await _context.Meal.FirstOrDefaultAsync(m => m.meal_id == meal_id[i]);
                    if (meal == null)
                    {
                        TempData["errMsg"] = $"錯誤的餐點資訊，無法找到餐點ID: {meal_id[i]}";
                        return View();
                    }
                    // 創建新的報表
                    var report = new Daily_Sales_ReportModel
                    {
                        meal_id = meal_id[i],
                        total_quantity = quantity[i],
                        total_sales = quantity[i] * meal.price,
                        date = DateTime.Now
                    };
                    await _context.Daily_Sales_Report.AddAsync(report);

                    // 將資料儲存進中間表
                    var reportMeal = new ReportMealModel
                    {
                        rm_id = Guid.NewGuid(),
                        meal_id = meal_id[i],
                        Meal = meal,
                        report_id = report.report_id,
                        Report = report
                    };
                    await _context.ReportMeal.AddAsync(reportMeal);
                }
                await _context.SaveChangesAsync();
                return RedirectToAction("Daily_Sales_Report");
            }
            catch(Exception ex)
            {
                return StatusCode(500, $"伺服器錯誤:{ex.Message}");
            }
        }

        //刪除報表
        [HttpPost]
        public async Task<IActionResult> delReport(Guid rm_id)
        {
            try
            {
                //查找中間表
                var rm = await _context.ReportMeal.FindAsync(rm_id);
                if(rm == null)
                {
                    throw new ArgumentException("找不到對應的關聯表!");
                }
                //查找中間表對應的每日報表
                var report = await _context.Daily_Sales_Report.FindAsync(rm.report_id);
                if(report == null)
                {
                    throw new ArgumentException("找不到對應的報表!");
                }
                _context.Daily_Sales_Report.Remove(report);
                _context.ReportMeal.Remove(rm);
                await _context.SaveChangesAsync();
                TempData["errMsg"] = "修改成功!";
                return RedirectToAction("Daily_Sales_Report");
            }
            catch(Exception ex)
            {
                return StatusCode(500, $"伺服器錯誤:{ex.Message}");
            }
        }

        //銷售預測
        [HttpGet]
        public IActionResult Prediction()
        {
            return View();
        }
    }
}

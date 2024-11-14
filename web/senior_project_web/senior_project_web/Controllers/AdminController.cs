using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using senior_project_web.Data;
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
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using senior_project_web.Data;
using BCrypt.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using senior_project_web.Models;

namespace senior_project_web.Controllers
{
    public class AuthController : Controller
    {
        private readonly OrderSystemDbContext _context;
        public AuthController(OrderSystemDbContext orderSystemDbContext)
        {
            _context = orderSystemDbContext;
        }

        //管理員登入頁面
        [HttpGet]
        public IActionResult AdminLogin()
        {
            return View();
        }
        //管理員登入方法
        [HttpPost]
        public async Task<IActionResult> AdminLogin([FromBody] AdminModel request)
        {
            //從資料庫找尋是否有admin帳號存在
            var user = await _context.Admin.Include(a => a.User).FirstOrDefaultAsync(u => u.admin_account == request.admin_account);
            
            if (user == null)
            {
                return NotFound(new { success = false, message = "帳號或密碼錯誤!" });
            }

            //比對密碼是否正確
            if (!BCrypt.Net.BCrypt.Verify(request.password, user.password))
            {
                return NotFound(new { success = false, message = "帳號或密碼錯誤!" });
            }

            //將帳號轉為String
            var account = request.admin_account.ToString();
            var id = user.admin_id.ToString();

            //登入成功，建立Cookie
            Claim[] claims = new[]
            {
                new Claim(ClaimTypes.Name, user.User.username), //使用Name來儲存帳號
                new Claim(ClaimTypes.NameIdentifier, id), //用來辨識唯一用戶
                new Claim(ClaimTypes.Role, "Admin") //新增登入角色
            };
            //創建ClaimsIdentity，並設置驗證方案
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            //創建ClaimPrinciple物件
            ClaimsPrincipal claimsprincipal = new ClaimsPrincipal(claimsIdentity);

            //設定登入時的驗證屬性
            var authProperties = new AuthenticationProperties
            {
                ExpiresUtc = DateTime.UtcNow.AddMinutes(60) //設定過期時間60分鐘
            };

            //使用SignInAsync登入並建立Cookie
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsprincipal, authProperties);

            return Ok(new { success = true, message = "成功登入!", redirectTo = "/Admin/Index" });
        }
        //管理員註冊頁面
        [HttpGet]
        public IActionResult AdminRegister()
        {
            return View();
        }
        //管理員註冊方法
        [HttpPost]
        public async Task<IActionResult> AdminRegister(string email, string password)
        {
            var user = await _context.User.FirstOrDefaultAsync(u => u.email == email);
            if(user == null)
            {
                ViewBag.errMsg = "查無用戶";
                return View();
            }
            var IsAdminExist = await _context.Admin.FirstOrDefaultAsync(a => a.user_id == user.user_id);
            if (IsAdminExist != null)
            {
                ViewBag.errMsg = "帳號已存在";
                return View();
            }

            //密碼加密
            var hashpassword = BCrypt.Net.BCrypt.HashPassword(password);


            //創建新的AdminModel
            AdminModel admin = new AdminModel
            {
                admin_id = Guid.NewGuid(),
                password = hashpassword,
                user_id = user.user_id
            };

            _context.Admin.Add(admin);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }
        //管理員登出方法
        [HttpGet]
        public async Task<IActionResult> AdminLogout()
        {
            //清除驗證cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Admin");
        }

        //用戶登入畫面
        [HttpGet]
        public IActionResult UserLogin()
        {
            return View();
        }
        //用戶登入方法
        public async Task<IActionResult> UserLogin([FromBody] UserModel request)
        {
            var user = await _context.User.FirstOrDefaultAsync(u => u.user_id == request.user_id);
            if (user == null)
            {
                return NotFound(new { success = false, message = "用戶不存在!" });
            }

            // 創建 Claims
            Claim[] claims = new[]
            {
                new Claim(ClaimTypes.Name, user.username),
                new Claim(ClaimTypes.NameIdentifier, user.user_id),
                new Claim(ClaimTypes.Role, "User")
            };

            // 創建 ClaimsIdentity 和 ClaimsPrincipal
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            // 設定登入時的驗證屬性
            var authProperties = new AuthenticationProperties
            {
                ExpiresUtc = DateTime.UtcNow.AddMinutes(60) // 設定過期時間 60 分鐘
            };

            // 執行 Cookie 登入
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, authProperties);

            return Ok(new { success = true, message = "登入成功!", redirectTo = "/Home/Index" });
        }
        //用戶註冊頁面
        [HttpGet]
        public IActionResult UserRegister()
        {
            return View();
        }
    }
}

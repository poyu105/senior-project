using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using senior_project_web.Data;
using senior_project_web.Models;

namespace senior_project_web.Controllers.api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : Controller
    {
        public readonly OrderSystemDbContext _context;
        public AccountController(OrderSystemDbContext context)
        {
            _context = context;
        }

        //人臉辨識請求class
        public class FaceLoginRequest
        {
            public string FaceId { get; set; }
        }

        //python人臉辨識API
        [HttpPost("face-login")]
        public async Task<IActionResult> FaceLogin([FromBody] FaceLoginRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.FaceId))
                {
                    throw new ArgumentException("請求為空!");
                }
                var user = await _context.User.FirstOrDefaultAsync(u => u.user_id == request.FaceId);
                if (user == null)
                {
                    TempData["errMsg"] = "未找到對應用戶，請註冊!";
                    return NotFound(new
                    {
                        success = false,
                        message = "未找到對應用戶，請註冊!",
                        redirectTo = "/Auth/UserRegister"
                    });
                }

                //找到用戶後將資料傳遞到AuthController建立Cookie
                var authController = HttpContext.RequestServices.GetRequiredService<AuthController>();
                //呼叫UserLogin方法，傳遞UserId
                var loginRequest = new UserModel{ user_id = user.user_id };
                var loginResult = await authController.UserLogin(loginRequest);
                return loginResult;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "伺服器錯誤: " + ex.Message });
            }
        }
    }
}

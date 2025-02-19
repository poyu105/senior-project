using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using senior_project_web.Data;

namespace senior_project_web.Controllers
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

        [HttpPost("face-login")]
        public async Task<IActionResult> FaceLogin([FromBody] FaceLoginRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.FaceId))
                {
                    return BadRequest(new { success = false, message = "請求為空!" });
                }

                var user = await _context.User.FirstOrDefaultAsync(u => u.user_id == request.FaceId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "未找到對應用戶，請註冊!" });
                }

                return Ok(new
                {
                    success = true,
                    message = "登入成功!",
                    user = new
                    {
                        id = user.user_id,
                        username = user.username,
                        gender = user.gender,
                        birth = user.birth,
                        phone_number = user.phone_number,
                        email = user.email
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "伺服器錯誤: " + ex.Message });
            }
        }
    }
}

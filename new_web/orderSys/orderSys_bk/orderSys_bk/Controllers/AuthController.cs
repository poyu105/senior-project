using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using orderSys_bk.Data;
using orderSys_bk.Model.Dto;
using senior_project_web.Models;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace orderSys_bk.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly OrderSysDbContext _dbContext;
        public AuthController(OrderSysDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        private readonly HttpClient _httpClient = new HttpClient();

        //呼叫python臉部辨識
        private async Task<string> CallPythonFaceRecognitionAsync(string[] base64Images)
        {
            var payload = new { images = base64Images };

            try
            {
                // 呼叫 Flask API
                var response = await _httpClient.PostAsJsonAsync("http://127.0.0.1:5000/face-recognition", payload);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"呼叫API失敗: {response.StatusCode}, 內容: {errorContent}");
                    return null;
                }

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();

                // 讀取回傳的 JSON 結果裡的 id
                if (jsonResponse.TryGetProperty("id", out var idProp))
                {
                    Console.WriteLine($"【人臉辨識API:CallPythonFaceRecognitionAsync】:辨識成功! 出現最多次ID: {idProp.GetString()}");
                    return idProp.GetString();
                }
                else
                {
                    Console.WriteLine("API 回傳中找不到 id");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"呼叫API例外: {ex.Message}");
                return null;
            }
        }

        //顧客註冊
        [HttpPost("customer-register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto req)
        {
            if (req.photos == null || req.photos.Length == 0)
            {
                return BadRequest(new { success = false, message = "註冊失敗: 錯誤的辨識資料!" });
            }
                
            // 直接用一次呼叫API，傳4張照片
            var id = await CallPythonFaceRecognitionAsync(req.photos);
            if (id == null){
                return StatusCode(500, new { success = false, message = "人臉辨識服務錯誤" });
            }

            // 檢查是否有對應的使用者存在
            var existingUser = await _dbContext.User.FirstOrDefaultAsync(u => u.user_id == id);
            if (existingUser != null){
                return BadRequest(new { success = false, message = "註冊失敗: 用戶已存在!" });
            }

            // 建立新的使用者
            var newUser = new UserModel
            {
                user_id = id,
                username = req.username,
                gender = 'M',
                birth = DateTime.Now,
                phone_number = req.phone_number,
                email = "test@email.com",
                create_at = DateTime.Now,
                update_at = DateTime.Now,
            };

            // 將使用者加入資料庫
            _dbContext.User.Add(newUser);
            await _dbContext.SaveChangesAsync();
            return Ok(new { success = true, message = "註冊成功，請登入!" });
        }

        //顧客登入
        [HttpPost("customer-login")]
        public async Task<IActionResult> Login([FromBody] string[] photos)
        {
            if (photos == null || photos.Length == 0)
                return BadRequest(new { message = "登入失敗: 錯誤的資訊!" });

            // 直接用一次呼叫API，傳4張照片
            var id = await CallPythonFaceRecognitionAsync(photos);

            if (id == null)
                return StatusCode(500, new { message = "人臉辨識服務錯誤" });

            // 判斷返回值(可依你的業務邏輯調整)
            if (id == "Excel 人臉數據庫不存在，將初始化新數據庫。")
            {
                return StatusCode(500, new { message = "系統錯誤，請聯絡管理員!" });
            }

            // 從資料庫找用戶
            var user = await _dbContext.User.FirstOrDefaultAsync(u => u.user_id == id);
            if (user == null)
                return Ok(new { success = false, message = "查無用戶，請註冊!" });

            // 產生JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("1234567890_abcdefghijklmnopqrstuvwxyz_ABCDEFGHIJKLMNOPQRSTUVWXYZ_0987654321");
            var tokenDesciptior = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.user_id) }),
                Expires = DateTime.UtcNow.AddMinutes(30),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            };
            var token = tokenHandler.CreateToken(tokenDesciptior);
            var jwtToken = tokenHandler.WriteToken(token);

            return Ok(new { success = true, message = "成功登入", token = jwtToken });
        }

        //呼叫python臉部辨識
        /*private async Task<string> CallPythonFaceRecognition(string base64Image, int index)
        {
            if (base64Image.StartsWith("data:image"))
            {
                base64Image = base64Image.Substring(base64Image.IndexOf(',') + 1);
            }
            Console.WriteLine($"[臉部辨識]:進行python呼叫設定[{index}]");
            var psi = new ProcessStartInfo
            {
                FileName = "C:\\Users\\chen0\\AppData\\Local\\Programs\\Python\\Python310\\python.exe", //python執行檔案(python.exe)路徑
                Arguments = "C:\\Users\\chen0\\code\\畢業專題\\senior-project\\python\\人臉辨識\\image.py", //欲執行的python專案路徑
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,  // 啟用錯誤輸出管線
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Console.WriteLine($"[臉部辨識]:準備執行python[{index}]");
            using (var process = Process.Start(psi))
            {
                if (process == null) return "unknown";

                process.StandardInput.WriteLine(base64Image);
                process.StandardInput.Close();

                string id = process.StandardOutput.ReadLine();
                Console.WriteLine($"[臉部辨識]:python運行結果[{index}]，id為\"{id}\"");
                process.WaitForExit();
                Console.WriteLine($"[臉部辨識]:python運行結束[{index}]");
                return id ?? "unknown";
            }
        }*/

        //顧客登入
        /*[HttpPost("customer-login")]
        public async Task<IActionResult> Login([FromBody] string[] photos)
        {
            try
            {
                if (photos == null || photos.Length == 0)
                {
                    return BadRequest(new { message = "登入失敗: 錯誤的資訊!" });
                }

                Dictionary<string, int> ids = new Dictionary<string, int>(); //統計回傳Id字典
                //針對相片集執行python獲取Id
                for(int i = 0; i < photos.Length; i++)
                {
                    string id = await CallPythonFaceRecognition(photos[i], i);
                    if (ids.ContainsKey(id))
                    {
                        ids[id]++;
                    }
                    else
                    {
                        ids[id] = 1;
                    }
                }

                //找出最高出現次數的Id
                string finalId = ids.OrderByDescending(ids => ids.Value).First().Key;

                if(finalId == "Excel 人臉數據庫不存在，將初始化新數據庫。")
                {
                    throw new Exception("系統錯誤，請聯絡管理員!");
                }

                //從資料庫中找到對應用戶
                var user = await _dbContext.User.Where(u => u.user_id == finalId).FirstOrDefaultAsync();
                if(user == null)
                {
                    return BadRequest(new { message = "查無用戶，請註冊!" });
                }

                //產生JWT token
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes("1234567890_abcdefghijklmnopqrstuvwxyz_ABCDEFGHIJKLMNOPQRSTUVWXYZ_0987654321"); //密鑰
                var tokenDesciptior = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.user_id),
                    }),
                    Expires = DateTime.UtcNow.AddMinutes(30), //設定JWT過期時效
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                };
                var token = tokenHandler.CreateToken(tokenDesciptior);
                var jwtToken = tokenHandler.WriteToken(token);

                return Ok(new { message = "成功登入", token = jwtToken });
            } catch (Exception ex)
            {
                return StatusCode(500, new { message = $"伺服器錯誤: {ex.Message}" });
            }
        }*/

        //管理員登入
        [HttpPost("admin-login")]
        public async Task<IActionResult> Login([FromBody] AdminLoginDto req)
        {
            try
            {
                if ( req.account < 1000 || string.IsNullOrEmpty(req.password))
                {
                    return BadRequest(new { message = "登入失敗: 錯誤的資訊!" });
                }

                //從資料庫中找到對應用戶
                var admin = await _dbContext.Admin.Where(a => a.admin_account == req.account).FirstOrDefaultAsync();
                if (admin == null)
                {
                    return BadRequest(new { message = "查無用戶，請註冊!" });
                }

                //檢查密碼
                if (!BCrypt.Net.BCrypt.Verify(req.password, admin.password))
                {
                    return BadRequest(new { message = "登入失敗: 錯誤的資訊!" });
                }

                //產生JWT token
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes("1234567890_abcdefghijklmnopqrstuvwxyz_ABCDEFGHIJKLMNOPQRSTUVWXYZ_0987654321"); //密鑰
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, admin.admin_id.ToString())
                    }),
                    Expires = DateTime.UtcNow.AddHours(1),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var jwtToken = tokenHandler.WriteToken(token);

                return Ok(new { message = "成功登入", token = jwtToken, user = admin.username });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"伺服器錯誤: {ex.Message}" });
            }
        }

        //管理員註冊
        [HttpPost("admin-register")]
        public async Task<IActionResult> Register([FromBody] AdminRegisterDto req)
        {
            try
            {
                if(req == null)
                {
                    return BadRequest(new { message = "註冊失敗: 錯誤的資訊!" });
                }

                //檢查是否有對應的使用者存在
                var existingAdmin = await _dbContext.Admin.Where(a=>a.username == req.username).FirstOrDefaultAsync();
                if (existingAdmin != null)
                {
                    return BadRequest(new { message = "註冊失敗: 使用者名稱已存在!" });
                }

                //密碼加密
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(req.password);

                //建立新Id
                var newId = Guid.NewGuid();

                //建立新的使用者
                var newAdmin = new AdminModel
                {
                    admin_id = newId,
                    username = req.username,
                    password = passwordHash,
                    create_at = DateTime.Now,
                };

                //將使用者加入資料庫
                _dbContext.Admin.Add(newAdmin);
                await _dbContext.SaveChangesAsync();

                var adminAccount = await _dbContext.Admin.Where(a=>a.admin_id == newId).Select(a => a.admin_account).FirstOrDefaultAsync();

                return Ok(new { message = $"註冊成功，請使用帳號:{adminAccount}進行登入!" });
            } catch (Exception ex)
            {
                return StatusCode(500, new { message = $"伺服器錯誤: {ex.Message}" });
            }
        }
    }
}

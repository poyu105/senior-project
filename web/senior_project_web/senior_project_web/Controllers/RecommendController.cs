using Microsoft.AspNetCore.Mvc;

namespace senior_project_web.Controllers
{
    public class RecommendController : Controller
    {
        //連線字串變數
        private readonly string _connectionString;

        //取得連線字串
        public RecommendController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("OrderSystem") ?? string.Empty;
        }

        //根據用戶id進行餐點預測
        public IActionResult Index(string user_id)
        {
            return View();
        }
    }
}

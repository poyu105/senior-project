using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using senior_project_web.Data;
using senior_project_web.Helpers;
using senior_project_web.Models;

namespace senior_project_web.Controllers
{
    public class CartController : Controller
    {
        private readonly OrderSystemDbContext _context;

        public CartController(OrderSystemDbContext context)
        {
            _context = context;
        }

        //檢視購物車
        [HttpGet]
        public IActionResult Index()
        {
            List<CartModel> cartItems = SessionHelper.GetObjectFromJson<List<CartModel>>(HttpContext.Session, "cart");

            return View(cartItems);
        }

        [HttpPost]
        public async Task<IActionResult> AddCart(Guid _meal_id, int _amount)
        {
            try
            {
                if(_meal_id == Guid.Empty)
                {
                    throw new ArgumentException("傳入的_meal_id為空!");
                }
                if(_amount <= 0)
                {
                    throw new ArgumentException("數量不可小於1!");
                }

                CartModel item = new CartModel
                {
                    Meal = await _context.Meal.FindAsync(_meal_id),
                    amount = _amount
                };

                //判斷Session是否已建立購物車
                if (SessionHelper.GetObjectFromJson<List<CartModel>>(HttpContext.Session, "cart") == null)
                {
                    //沒有則建立新的購物車
                    List<CartModel> cart = new List<CartModel>();
                    cart.Add(item);
                    SessionHelper.SetObjectAsJson(HttpContext.Session, "cart", cart);
                }
                else
                {
                    //若有則尋找有無相同的餐點: 有則調整數量
                    List<CartModel> cart = SessionHelper.GetObjectFromJson<List<CartModel>>(HttpContext.Session, "cart");
                    int index = cart.FindIndex(c => c.Meal.meal_id == _meal_id);
                    if (index != -1)
                    {
                        cart[index].amount += _amount;
                    }
                    else
                    {
                        cart.Add(item);
                    }
                    SessionHelper.SetObjectAsJson(HttpContext.Session, "cart", cart);
                }
                TempData["errMsg"] = "成功加入購物車! \n";

            }
            catch(Exception ex)
            {
                TempData["errMsg"] = "新增至購物車失敗! \n"+ex.Message;
            }

            return RedirectToAction("Index", "Home");
        }
    }
}

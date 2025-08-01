using orderSys_bk.Model.Dto;
using senior_project_web.Models;

namespace orderSys_bk.Model.Dto
{
    public class OrderMeals
    {
        public Guid meal_id { get; set; } //餐點ID
        public string name { get; set; } //餐點名稱
        public int amount { get; set; } = 1; //點餐數量
    }
    public class CreateOrderDto
    {
        public string? payment {  get; set; }
        public int? total { get; set; }
        public string? user_id { get; set; }
        public List<OrderMeals>? orders {  get; set; }
    }
}

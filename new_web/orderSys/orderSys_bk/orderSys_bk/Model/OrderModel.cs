using System.ComponentModel.DataAnnotations;

namespace senior_project_web.Models
{
    public class OrderModel
    {
        [Key]
        public int order_id { get; set; }

        [Required]
        public DateTime date { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(15)]
        public string weather_condition { get; set; }

        [Required]
        public string season { get; set; }

        [Required]
        public int total {  get; set; } //訂單總金額

        [Required]
        public string payment {  get; set; } //付款方式(cash, credit, mobile)

        // FK
        [Required]
        public string user_id { get; set; }
        public UserModel User { get; set; }

        public ICollection<Order_MealModel> Order_Meal { get; set; }
    }
}

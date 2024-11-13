using System.ComponentModel.DataAnnotations;

namespace senior_project_web.Models
{
    public class Order_MealModel
    {
        [Key]
        public int order_meal_id {  get; set; }
    
        public int quantity { get; set; }

        //FK
        [Required]
        public int order_id { get; set; }
        public OrderModel order { get; set; }

        //FK
        [Required]
        public string meal_id { get; set; }
        public MealModel meal { get; set; }
    }
}

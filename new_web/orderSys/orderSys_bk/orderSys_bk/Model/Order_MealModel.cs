using System.ComponentModel.DataAnnotations;

namespace senior_project_web.Models
{
    public class Order_MealModel
    {
        [Key]
        public int order_meal_id {  get; set; }
    
        public int amount { get; set; }

        //FK
        [Required]
        public int order_id { get; set; }
        public OrderModel Order { get; set; }

        //FK
        [Required]
        public Guid meal_id { get; set; }
        public MealModel Meal { get; set; }
    }
}

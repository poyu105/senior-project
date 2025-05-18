using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace senior_project_web.Models
{
    public class Daily_Sales_ReportModel
    {
        [Key]
        public int report_id {  get; set; }

        [Required]
        public int total_sales { get; set; }

        [Required]
        public int total_quantity {  get; set; }

        [Required]
        public DateTime date { get; set; }

        // FK
        [Required]
        [ForeignKey("meal_id")]
        public Guid meal_id { get; set; }
        public ICollection<MealModel> Meal { get; set; }
        public ICollection<ReportMealModel> ReportMeal { get; set; } //中間表操作
    }
}

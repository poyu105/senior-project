using System.ComponentModel.DataAnnotations;

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
        public string meal_id { get; set; }
        public MealModel meal { get; set; }
    }
}

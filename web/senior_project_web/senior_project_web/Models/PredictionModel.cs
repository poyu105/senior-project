using System.ComponentModel.DataAnnotations;

namespace senior_project_web.Models
{
    public class PredictionModel
    {
        [Key]
        public string prediction_id {  get; set; }

        [Required]
        public DateTime date { get; set; }

        [Required]
        public int predicted_sales {  get; set; }

        [Required]
        [MaxLength(10)]
        public string weather_condition { get; set; }

        [Required]
        public int temperature { get; set; }

        [Required]
        [MaxLength(32)]
        public string model_version { get; set; }

        public DateTime create_at { get; set; }

        // FK
        [Required]
        public string meal_id { get; set; }
        public MealModel Meal { get; set; }
    }
}

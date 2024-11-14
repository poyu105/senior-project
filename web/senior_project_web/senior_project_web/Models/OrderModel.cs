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
        public int temperature { get; set; }

        // FK
        [Required]
        public string user_id { get; set; }
        public UserModel User { get; set; }
    }
}

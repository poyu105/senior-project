using System.ComponentModel.DataAnnotations;

namespace senior_project_web.Models
{
    public class MealModel
    {
        [Key]
        public string meal_id {  get; set; } //Guid

        [Required]
        [MaxLength(255)]
        public string name { get; set; }

        [Required]
        [MaxLength(64)]
        public string type { get; set; }

        [Required]
        [MaxLength(255)]
        public string img_path { get; set; }

        [Required]
        [MaxLength(255)]
        public string description { get; set; }

        [Required]
        public int price { get; set; }

        [Required]
        public int cost { get; set; }

        //FK
        [Required]
        public string inventory_id { get; set; }
        public InventoryModel inventory { get; set; }
    }
}

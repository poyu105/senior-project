using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace senior_project_web.Models
{
    public class MealModel
    {
        [Key]
        public Guid meal_id {  get; set; } //Guid

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
        [ForeignKey("inventory_id")]
        public Guid inventory_id { get; set; }
        public InventoryModel Inventory { get; set; }
    }
}

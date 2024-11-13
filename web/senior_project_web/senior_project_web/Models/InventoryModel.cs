using System.ComponentModel.DataAnnotations;

namespace senior_project_web.Models
{
    public class InventoryModel
    {
        [Key]
        public string inventory_id { get; set; } //Guid

        [Required]
        public int quantity { get; set; }

        public DateTime create_at { get; set; } = DateTime.Now;
        public DateTime update_at { get; set; } = DateTime.Now;
    }
}

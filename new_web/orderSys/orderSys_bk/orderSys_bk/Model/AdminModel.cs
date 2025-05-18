using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace senior_project_web.Models
{
    public class AdminModel
    {
        [Key]
        public Guid admin_id { get; set; }    //使用Guid生成

        public int admin_account {  get; set; } //自動遞增

        [Required]
        public string username { get; set; } //使用者名稱

        [Required]
        [MaxLength(255)]
        public string password { get; set; }

        public DateTime create_at { get; set; } = DateTime.Now;
        public DateTime update_at { get; set;} = DateTime.Now;
    }
}

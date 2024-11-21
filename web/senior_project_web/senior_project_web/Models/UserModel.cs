using System.ComponentModel.DataAnnotations;

namespace senior_project_web.Models
{
    public class UserModel
    {
        [Key]
        public string user_id { get; set; } //使用python中的SHA-256生成
        [Required]
        [MaxLength(255)]
        public string username { get; set; }
        [Required]
        public char gender { get; set; }    //'M'或'F'
        [Required]
        public DateTime birth {  get; set; }
        [Required]
        [MaxLength(15)]
        public string phone_number { get; set; }
        [Required]
        [MaxLength(255)]
        public string email { get; set; }
    
        public DateTime create_at { get; set; } = DateTime.Now;
        public DateTime update_at { get; set; } = DateTime.Now;

        public AdminModel Admin { get; set; }
    }
}

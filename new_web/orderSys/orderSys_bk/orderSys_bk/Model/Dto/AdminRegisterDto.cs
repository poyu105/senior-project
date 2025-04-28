using System.ComponentModel.DataAnnotations;

namespace orderSys_bk.Model.Dto
{
    public class AdminRegisterDto
    {
        [Required]
        public string username { get; set; }

        [Required]
        public string password { get; set; }
    }
}

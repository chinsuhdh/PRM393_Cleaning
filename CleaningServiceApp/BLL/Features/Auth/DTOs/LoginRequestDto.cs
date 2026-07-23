using System.ComponentModel.DataAnnotations;

namespace Cleaning.BLL.Features.Auth
{
    public class LoginRequestDto
    {
        [Required]
        public string EmailOrPhone { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;
    }
}
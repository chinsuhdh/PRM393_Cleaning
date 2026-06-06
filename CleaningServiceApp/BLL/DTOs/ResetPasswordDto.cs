using System.ComponentModel.DataAnnotations;

namespace Cleaning.BLL.DTOs
{
    public class ResetPasswordDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string OtpCode { get; set; } = null!;

        [Required, MinLength(6)]
        public string NewPassword { get; set; } = null!;
    }
}
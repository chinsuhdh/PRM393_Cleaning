using System.ComponentModel.DataAnnotations;

namespace Cleaning.BLL.DTOs
{
    public class ForgotPasswordRequestDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;
    }
}
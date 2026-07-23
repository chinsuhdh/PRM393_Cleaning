using System.ComponentModel.DataAnnotations;

namespace Cleaning.BLL.Features.Auth
{
    public class ForgotPasswordRequestDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;
    }
}
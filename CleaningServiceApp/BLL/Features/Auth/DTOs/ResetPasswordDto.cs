using System.ComponentModel.DataAnnotations;

namespace Cleaning.BLL.Features.Auth
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

    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Mật khẩu cũ là bắt buộc")]
        public string OldPassword { get; set; } = null!;

        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải từ 6 ký tự trở lên")]
        public string NewPassword { get; set; } = null!;
    }

    public class VerifyPhoneDto
    {
        [Required, Phone]
        public string PhoneNumber { get; set; } = null!;

        [Required]
        public string OtpCode { get; set; } = null!;
    }
}
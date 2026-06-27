using System.ComponentModel.DataAnnotations;

namespace Cleaning.BLL.DTOs
{
    public class ProfileDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = null!;
        public string? AvatarUrl { get; set; }

        // [THÊM MỚI] 3 trường lấy từ bảng Accounts
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsPhoneVerified { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class UpdateProfileDto
    {
        [Required(ErrorMessage = "FullName is required")]
        [StringLength(100, ErrorMessage = "FullName cannot exceed 100 characters")]
        public string FullName { get; set; } = null!;

        public string? AvatarUrl { get; set; }
    }
}
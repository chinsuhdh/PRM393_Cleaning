using System.ComponentModel.DataAnnotations;

namespace Cleaning.BLL.DTOs
{
    public class ProfileDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateProfileDto
    {
        [Required]
        public string FullName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
    }
}
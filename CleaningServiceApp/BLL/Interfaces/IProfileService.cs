using Cleaning.BLL.DTOs;

namespace Cleaning.BLL.Interfaces
{
    public interface IProfileService
    {
        Task<ProfileDto?> GetProfileAsync(Guid userId);
        Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileDto request);
        Task<bool> UpdateAvatarAsync(Guid userId, string avatarUrl);

        // [CÁC HÀM THÊM MỚI]
        Task<bool> CompleteOnboardingAsync(Guid userId);
        Task<bool> DeleteAccountAsync(Guid userId);
    }
}
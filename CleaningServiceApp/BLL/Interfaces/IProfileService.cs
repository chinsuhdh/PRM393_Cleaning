using Cleaning.BLL.DTOs;

namespace Cleaning.BLL.Interfaces
{
    public interface IProfileService
    {
        Task<ProfileDto?> GetProfileAsync(Guid userId);
        Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileDto request);
    }
}
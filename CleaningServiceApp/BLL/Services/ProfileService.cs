using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Interfaces;

namespace Cleaning.BLL.Services
{
    public class ProfileService : IProfileService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProfileService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ProfileDto?> GetProfileAsync(Guid userId)
        {
            var profile = await _unitOfWork.Repository<Profile>().GetByIdAsync(userId);
            if (profile == null) return null;

            return new ProfileDto
            {
                Id = profile.Id,
                FullName = profile.FullName,
                AvatarUrl = profile.AvatarUrl,
                CreatedAt = profile.CreatedAt
            };
        }

        public async Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileDto request)
        {
            var profile = await _unitOfWork.Repository<Profile>().GetByIdAsync(userId);
            if (profile == null) return false;

            profile.FullName = request.FullName;
            profile.AvatarUrl = request.AvatarUrl;

            _unitOfWork.Repository<Profile>().Update(profile);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }
}
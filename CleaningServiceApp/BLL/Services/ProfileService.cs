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

            if (profile == null)
                return null;

            return new ProfileDto
            {
                Id = profile.Id,
                FullName = profile.FullName,
                AvatarUrl = profile.AvatarUrl,
                CreatedAt = profile.CreatedAt,
                UpdatedAt = profile.UpdatedAt
            };
        }

        public async Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileDto request)
        {
            var profile = await _unitOfWork.Repository<Profile>().GetByIdAsync(userId);

            if (profile == null)
                return false;

            // Cập nhật thông tin
            profile.FullName = request.FullName;
            profile.AvatarUrl = request.AvatarUrl;
            profile.UpdatedAt = DateTime.UtcNow; // Xử lý đồng bộ với TIMESTAMPTZ

            _unitOfWork.Repository<Profile>().Update(profile);

            // Nếu SaveChangesAsync trả về > 0 nghĩa là update thành công
            return await _unitOfWork.SaveChangesAsync() > 0;
        }
    }
}
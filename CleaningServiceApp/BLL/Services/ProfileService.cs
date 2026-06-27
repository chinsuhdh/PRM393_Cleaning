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
            // Lấy cả Profile và Account (vì 2 bảng này dùng chung Id/UserId)
            var profile = await _unitOfWork.Repository<Profile>().GetByIdAsync(userId);
            var account = await _unitOfWork.Repository<Account>().GetByIdAsync(userId);

            if (profile == null || account == null)
                return null;

            return new ProfileDto
            {
                Id = profile.Id,
                FullName = profile.FullName,
                AvatarUrl = profile.AvatarUrl,
                Email = account.Email,                   // [THÊM MỚI] Mapping từ Account
                PhoneNumber = account.PhoneNumber,       // [THÊM MỚI] Mapping từ Account
                IsPhoneVerified = account.IsPhoneVerified, // [THÊM MỚI] Mapping từ Account
                CreatedAt = profile.CreatedAt,
                UpdatedAt = profile.UpdatedAt
            };
        }

        public async Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileDto request)
        {
            var profile = await _unitOfWork.Repository<Profile>().GetByIdAsync(userId);

            if (profile == null)
                return false;

            profile.FullName = request.FullName;
            profile.AvatarUrl = request.AvatarUrl;
            profile.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Profile>().Update(profile);
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAvatarAsync(Guid userId, string avatarUrl)
        {
            var profile = await _unitOfWork.Repository<Profile>().GetByIdAsync(userId);
            if (profile == null) return false;

            profile.AvatarUrl = avatarUrl;
            profile.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Profile>().Update(profile);
            return await _unitOfWork.SaveChangesAsync() > 0;
        }
    }
}
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

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
            var account = await _unitOfWork.Repository<Account>().GetByIdAsync(userId);

            if (profile == null || account == null)
                return null;

            // [THÊM MỚI] Tính toán số lượng dữ liệu thực tế từ Database
            // Đếm số lượng đơn đặt lịch (Booking) của người dùng
            var bookingCount = await _unitOfWork.Repository<Booking>()
                .CountAsync(b => b.ClientId == userId);

            // Đếm số lượng địa chỉ đã lưu (UserAddress) của người dùng
            var savedCount = await _unitOfWork.Repository<UserAddress>()
                .CountAsync(a => a.UserId == userId);

            // Mặc định Rating là 5.0 (Có thể tính trung bình từ bảng Review nếu có)
            var averageRating = 5.0m;

            return new ProfileDto
            {
                Id = profile.Id,
                FullName = profile.FullName,
                AvatarUrl = profile.AvatarUrl,
                Email = account.Email,
                PhoneNumber = account.PhoneNumber,
                IsPhoneVerified = account.IsPhoneVerified,
                CreatedAt = profile.CreatedAt,
                UpdatedAt = profile.UpdatedAt,
                OnboardingCompletedAt = profile.OnboardingCompletedAt,

                // Gán dữ liệu thống kê vào DTO
                BookingCount = bookingCount,
                Rating = averageRating,
                SavedCount = savedCount
            };
        }

        public async Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileDto request)
        {
            var profile = await _unitOfWork.Repository<Profile>().GetByIdAsync(userId);
            if (profile == null) return false;

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

        public async Task<bool> CompleteOnboardingAsync(Guid userId)
        {
            var profile = await _unitOfWork.Repository<Profile>().GetByIdAsync(userId);
            if (profile == null) return false;

            profile.OnboardingCompletedAt = DateTime.UtcNow;
            profile.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Profile>().Update(profile);
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAccountAsync(Guid userId)
        {
            var account = await _unitOfWork.Repository<Account>().GetByIdAsync(userId);
            var profile = await _unitOfWork.Repository<Profile>().GetByIdAsync(userId);

            if (account == null) return false;

            if (profile != null) _unitOfWork.Repository<Profile>().Remove(profile);
            _unitOfWork.Repository<Account>().Remove(account);

            return await _unitOfWork.SaveChangesAsync() > 0;
        }
    }
}
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Cleaning.BLL.Services
{
    /// <summary>
    /// Service responsible for managing user profiles, including fetching, updating, and deleting profiles.
    /// </summary>
    public class ProfileService : IProfileService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProfileService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Retrieves the detailed profile information for a given user ID, including their account details and statistics.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A ProfileDto containing profile details, or null if the user is not found.</returns>
        public async Task<ProfileDto?> GetProfileAsync(Guid userId)
        {
            var profile = await _unitOfWork.Repository<Profile>().GetByIdAsync(userId);
            var account = await _unitOfWork.Repository<Account>().GetByIdAsync(userId);

            if (profile == null || account == null)
                return null;

            var bookingCount = await _unitOfWork.Repository<Booking>()
                .CountAsync(b => b.ClientId == userId);

            var savedCount = await _unitOfWork.Repository<UserAddress>()
                .CountAsync(a => a.UserId == userId);

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

                BookingCount = bookingCount,
                Rating = averageRating,
                SavedCount = savedCount
            };
        }

        /// <summary>
        /// Updates the basic profile information (e.g., Full Name, Avatar) for a given user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="request">The data transfer object containing the updated profile information.</param>
        /// <returns>True if the update was successful, otherwise false.</returns>
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

        /// <summary>
        /// Updates specifically the avatar URL of a user's profile.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="avatarUrl">The new avatar URL string.</param>
        /// <returns>True if the update was successful, otherwise false.</returns>
        public async Task<bool> UpdateAvatarAsync(Guid userId, string avatarUrl)
        {
            var profile = await _unitOfWork.Repository<Profile>().GetByIdAsync(userId);
            if (profile == null) return false;

            profile.AvatarUrl = avatarUrl;
            profile.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Profile>().Update(profile);
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// Marks the onboarding process as completed for a given user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>True if the onboarding completion was recorded successfully, otherwise false.</returns>
        public async Task<bool> CompleteOnboardingAsync(Guid userId)
        {
            var profile = await _unitOfWork.Repository<Profile>().GetByIdAsync(userId);
            if (profile == null) return false;

            profile.OnboardingCompletedAt = DateTime.UtcNow;
            profile.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Profile>().Update(profile);
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// Permanently deletes a user's account and associated profile data.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to delete.</param>
        /// <returns>True if the deletion was successful, otherwise false.</returns>
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
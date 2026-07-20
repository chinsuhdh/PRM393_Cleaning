using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Interfaces;
using Cleaning.DAL.Enums;

namespace Cleaning.BLL.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDispatchPublisher? _dispatchPublisher;

        public ReviewService(IUnitOfWork unitOfWork, IDispatchPublisher? dispatchPublisher = null)
        {
            _unitOfWork = unitOfWork;
            _dispatchPublisher = dispatchPublisher;
        }

        public async Task<ReviewDto> CreateReviewAsync(Guid reviewerId, CreateReviewDto request)
        {
            if (reviewerId == request.RevieweeId)
                throw new InvalidOperationException("You cannot review yourself.");

            var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(request.BookingId);
            if (booking == null)
                throw new ArgumentException("Booking not found.");

            if (booking.Status != BookingStatus.Completed)
                throw new InvalidOperationException("Reviews can only be created for completed bookings.");

            if (booking.ClientId != reviewerId && booking.WorkerId != reviewerId)
                throw new InvalidOperationException("You are not authorized to review this booking.");

            var existingReviews = await _unitOfWork.Repository<Review>()
                .FindAsync(r => r.BookingId == request.BookingId &&
                                r.ReviewerId == reviewerId &&
                                r.RevieweeId == request.RevieweeId);

            if (existingReviews.Any())
                throw new InvalidOperationException("You have already reviewed this user for this booking.");

            var review = new Review
            {
                BookingId = request.BookingId,
                ReviewerId = reviewerId,
                RevieweeId = request.RevieweeId,
                Rating = request.Rating,
                Comment = request.Comment,
                CreatedAt = DateTime.UtcNow
            };

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.Repository<Review>().AddAsync(review);
                await _unitOfWork.SaveChangesAsync();

                var workerProfile = await _unitOfWork.Repository<WorkerProfile>().GetByIdAsync(request.RevieweeId);

                if (workerProfile != null)
                {
                    var allReviews = await _unitOfWork.Repository<Review>()
                        .FindAsync(r => r.RevieweeId == request.RevieweeId);

                    if (allReviews.Any())
                    {
                        decimal newAverage = (decimal)allReviews.Average(r => r.Rating);
                        workerProfile.AverageRating = Math.Round(newAverage, 2);

                        _unitOfWork.Repository<WorkerProfile>().Update(workerProfile);
                        await _unitOfWork.SaveChangesAsync();
                    }
                }

                await transaction.CommitAsync();

                if (_dispatchPublisher != null)
                    await _dispatchPublisher.BookingStatusChangedAsync(booking.Id, booking.ClientId, booking.Status.ToString());

                return new ReviewDto
                {
                    Id = review.Id,
                    BookingId = review.BookingId,
                    ReviewerId = review.ReviewerId,
                    RevieweeId = review.RevieweeId,
                    Rating = review.Rating,
                    Comment = review.Comment,
                    CreatedAt = review.CreatedAt
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<ReviewDto>> GetReviewsForUserAsync(Guid revieweeId)
        {
            var reviews = await _unitOfWork.Repository<Review>()
                                           .FindAsync(r => r.RevieweeId == revieweeId);

            return reviews.Select(r => new ReviewDto
            {
                Id = r.Id,
                BookingId = r.BookingId,
                ReviewerId = r.ReviewerId,
                RevieweeId = r.RevieweeId,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            }).OrderByDescending(r => r.CreatedAt);
        }
    }
}
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cleaning.BLL.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReviewService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ReviewDto> CreateReviewAsync(Guid reviewerId, CreateReviewDto request)
        {
            // 1. Tạo Review mới
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

                // 2. Tự động tính toán lại Average Rating cho người bị đánh giá (Reviewee)
                // Lấy tất cả đánh giá của người này
                var allReviews = await _unitOfWork.Repository<Review>()
                                      .FindAsync(r => r.RevieweeId == request.RevieweeId);

                if (allReviews.Any())
                {
                    decimal newAverage = (decimal)allReviews.Average(r => r.Rating);

                    // Kiểm tra xem người bị đánh giá có phải là Thợ không
                    var workerProfile = await _unitOfWork.Repository<WorkerProfile>()
                                              .GetByIdAsync(request.RevieweeId);

                    if (workerProfile != null)
                    {
                        workerProfile.AverageRating = Math.Round(newAverage, 2); // Làm tròn 2 chữ số
                        _unitOfWork.Repository<WorkerProfile>().Update(workerProfile);
                        await _unitOfWork.SaveChangesAsync();
                    }
                }

                await transaction.CommitAsync();

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
            var reviews = await _unitOfWork.Repository<Review>().FindAsync(r => r.RevieweeId == revieweeId);

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
using Cleaning.BLL.Infrastructure.Dispatch;
﻿using AutoMapper;
using Cleaning.BLL.Common;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Interfaces;
using Cleaning.DAL.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cleaning.BLL.Features.Reviews
{
    public class ReviewService : IReviewService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IDispatchPublisher? _dispatchPublisher;

        public ReviewService(IUnitOfWork unitOfWork, IMapper mapper, IDispatchPublisher? dispatchPublisher = null)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _dispatchPublisher = dispatchPublisher;
        }

        public async Task<ReviewDto> CreateReviewAsync(Guid reviewerId, CreateReviewDto request)
        {
            if (reviewerId == request.RevieweeId)
                throw new AppException(AppErrors.ReviewSelfNotAllowed);

            var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(request.BookingId);
            if (booking == null)
                throw new AppException(AppErrors.BookingNotFound);

            if (booking.Status != BookingStatus.Completed)
                throw new AppException(AppErrors.ReviewBookingNotCompleted);

            if (booking.ClientId != reviewerId && booking.WorkerId != reviewerId)
                throw new AppException(AppErrors.Forbidden);

            var existingReviews = await _unitOfWork.Repository<Review>()
                .FindAsync(r => r.BookingId == request.BookingId &&
                                r.ReviewerId == reviewerId &&
                                r.RevieweeId == request.RevieweeId);

            if (existingReviews.Any())
                throw new AppException(AppErrors.ReviewAlreadyExists);

            var review = new Review
            {
                BookingId = request.BookingId,
                ReviewerId = reviewerId,
                RevieweeId = request.RevieweeId,
                Rating = request.Rating,
                Comment = request.Comment,
                CreatedAt = DateTime.UtcNow
            };

            using var transaction = await _unitOfWork.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                await _unitOfWork.Repository<Review>().AddAsync(review);
                await _unitOfWork.SaveChangesAsync();

                var workerProfile = await _unitOfWork.Repository<WorkerProfile>().GetByIdAsync(request.RevieweeId);

                if (workerProfile != null)
                {
                    var reviewCount = await _unitOfWork.Repository<Review>()
                        .GetQueryable()
                        .CountAsync(r => r.RevieweeId == request.RevieweeId);

                    if (reviewCount > 0)
                    {
                        decimal newAverage = await _unitOfWork.Repository<Review>()
                            .GetQueryable()
                            .Where(r => r.RevieweeId == request.RevieweeId)
                            .AverageAsync(r => (decimal)r.Rating);
                        workerProfile.AverageRating = Math.Round(newAverage, 2);

                        _unitOfWork.Repository<WorkerProfile>().Update(workerProfile);
                        await _unitOfWork.SaveChangesAsync();
                    }
                }

                await transaction.CommitAsync();

                if (_dispatchPublisher != null)
                    await _dispatchPublisher.BookingStatusChangedAsync(booking.Id, booking.ClientId, booking.Status.ToString());

                return _mapper.Map<ReviewDto>(review);
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

            return reviews.Select(_mapper.Map<ReviewDto>).OrderByDescending(r => r.CreatedAt);
        }
    }
}
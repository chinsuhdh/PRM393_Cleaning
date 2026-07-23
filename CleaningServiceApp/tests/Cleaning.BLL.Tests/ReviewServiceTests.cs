using Cleaning.BLL.Features.Reviews;
using Cleaning.BLL.Common;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;

namespace Cleaning.BLL.Tests;

public class ReviewServiceTests
{
    private static ReviewService CreateService(InMemoryUnitOfWork unitOfWork) =>
        new(unitOfWork, TestMapperFactory.Create());

    private static Booking CreateCompletedBooking(Guid clientId, Guid workerId) => new()
    {
        Id = Guid.NewGuid(),
        ClientId = clientId,
        WorkerId = workerId,
        Status = BookingStatus.Completed,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Fact(DisplayName = "[UT-REVIEW-01] Reviewing yourself is rejected")]
    public async Task CreateReviewAsync_SelfReview_Throws()
    {
        var userId = Guid.NewGuid();
        var unitOfWork = new InMemoryUnitOfWork().With(new List<Booking>()).With(new List<Review>());
        var service = CreateService(unitOfWork);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.CreateReviewAsync(userId, new CreateReviewDto { BookingId = Guid.NewGuid(), RevieweeId = userId, Rating = 5 }));

        Assert.Equal(AppErrors.ReviewSelfNotAllowed.Code, ex.Code);
    }

    [Fact(DisplayName = "[UT-REVIEW-02] Reviewing a non-existent booking is rejected")]
    public async Task CreateReviewAsync_BookingNotFound_Throws()
    {
        var clientId = Guid.NewGuid();
        var workerId = Guid.NewGuid();
        var unitOfWork = new InMemoryUnitOfWork().With(new List<Booking>()).With(new List<Review>());
        var service = CreateService(unitOfWork);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.CreateReviewAsync(clientId, new CreateReviewDto { BookingId = Guid.NewGuid(), RevieweeId = workerId, Rating = 5 }));

        Assert.Equal(AppErrors.BookingNotFound.Code, ex.Code);
    }

    [Fact(DisplayName = "[UT-REVIEW-03] Reviewing a booking that is not completed is rejected")]
    public async Task CreateReviewAsync_BookingNotCompleted_Throws()
    {
        var clientId = Guid.NewGuid();
        var workerId = Guid.NewGuid();
        var booking = CreateCompletedBooking(clientId, workerId);
        booking.Status = BookingStatus.Accepted;
        var unitOfWork = new InMemoryUnitOfWork().With(new List<Booking> { booking }).With(new List<Review>());
        var service = CreateService(unitOfWork);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.CreateReviewAsync(clientId, new CreateReviewDto { BookingId = booking.Id, RevieweeId = workerId, Rating = 5 }));

        Assert.Equal(AppErrors.ReviewBookingNotCompleted.Code, ex.Code);
    }

    [Fact(DisplayName = "[UT-REVIEW-04] A non-participant cannot review the booking")]
    public async Task CreateReviewAsync_NotParticipant_Throws()
    {
        var clientId = Guid.NewGuid();
        var workerId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();
        var booking = CreateCompletedBooking(clientId, workerId);
        var unitOfWork = new InMemoryUnitOfWork().With(new List<Booking> { booking }).With(new List<Review>());
        var service = CreateService(unitOfWork);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.CreateReviewAsync(outsiderId, new CreateReviewDto { BookingId = booking.Id, RevieweeId = workerId, Rating = 5 }));

        Assert.Equal(AppErrors.Forbidden.Code, ex.Code);
    }

    [Fact(DisplayName = "[UT-REVIEW-05] Reviewing the same user twice for the same booking is rejected")]
    public async Task CreateReviewAsync_DuplicateReview_Throws()
    {
        var clientId = Guid.NewGuid();
        var workerId = Guid.NewGuid();
        var booking = CreateCompletedBooking(clientId, workerId);
        var existingReview = new Review
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            ReviewerId = clientId,
            RevieweeId = workerId,
            Rating = 4,
            CreatedAt = DateTime.UtcNow
        };
        var unitOfWork = new InMemoryUnitOfWork()
            .With(new List<Booking> { booking })
            .With(new List<Review> { existingReview })
            .With(new List<WorkerProfile>());
        var service = CreateService(unitOfWork);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.CreateReviewAsync(clientId, new CreateReviewDto { BookingId = booking.Id, RevieweeId = workerId, Rating = 5 }));

        Assert.Equal(AppErrors.ReviewAlreadyExists.Code, ex.Code);
    }

    [Fact(DisplayName = "[UT-REVIEW-06] A valid review from a participant on a completed booking succeeds")]
    public async Task CreateReviewAsync_ValidReview_Succeeds()
    {
        var clientId = Guid.NewGuid();
        var workerId = Guid.NewGuid();
        var booking = CreateCompletedBooking(clientId, workerId);
        var unitOfWork = new InMemoryUnitOfWork()
            .With(new List<Booking> { booking })
            .With(new List<Review>())
            .With(new List<WorkerProfile>());
        var service = CreateService(unitOfWork);

        var result = await service.CreateReviewAsync(clientId, new CreateReviewDto
        {
            BookingId = booking.Id,
            RevieweeId = workerId,
            Rating = 5,
            Comment = "Great job"
        });

        Assert.Equal(booking.Id, result.BookingId);
        Assert.Equal(workerId, result.RevieweeId);
        Assert.Equal(5, result.Rating);
        Assert.Single(unitOfWork.Repository<Review>().GetQueryable());
    }
}

using Cleaning.BLL.Constants;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Cleaning.DAL.Interfaces;

namespace Cleaning.BLL.Services;

public sealed class BookingSweepService(
    IUnitOfWork unitOfWork,
    IBookingService bookingService,
    IDispatchPublisher? dispatchPublisher = null) : IBookingSweepService
{
    public async Task RunTickAsync(CancellationToken cancellationToken = default)
    {
        await AutoCancelUnansweredScheduledBookingsAsync(cancellationToken);
        await AutoRejectExpiredReschedulesAsync(cancellationToken);
        await SuspendWorkersOverThresholdAsync();
    }

    // Duty 1: a Scheduled booking still stuck in AwaitingWorker within 1 hour of its start time is
    // auto-cancelled. Reuses UpdateBookingStatusAsync — same state machine, BookingStatusLog,
    // BookingCancellation write, and JobCancelledAsync/BookingStatusChangedAsync dispatch that
    // already fire from any AwaitingWorker-origin cancel, so there's no new dispatch logic here.
    private async Task AutoCancelUnansweredScheduledBookingsAsync(CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddHours(1);
        var candidates = await unitOfWork.Repository<Booking>().FindAsync(booking =>
            booking.Status == BookingStatus.AwaitingWorker &&
            booking.BookingType == BookingType.Scheduled &&
            booking.ScheduledStartTime <= deadline);

        foreach (var booking in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await bookingService.UpdateBookingStatusAsync(booking.Id, booking.ClientId, new UpdateBookingStatusDto
            {
                NewStatus = BookingStatus.Cancelled,
                Reason = BookingReasons.SystemAutoCancelledUnanswered
            });
        }
    }

    private async Task AutoRejectExpiredReschedulesAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var expired = await unitOfWork.Repository<BookingRescheduleRequest>().FindAsync(request =>
            request.Status == RescheduleStatus.Pending && request.ExpiresAt != null && request.ExpiresAt <= now);

        foreach (var request in expired)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await bookingService.RespondRescheduleAsync(
                request.BookingId, request.Id, Guid.Empty, RescheduleAction.Reject, isSystemActor: true);
        }
    }

    private async Task SuspendWorkersOverThresholdAsync()
    {
        var cutoff = DateTime.UtcNow.AddDays(-CancellationConstants.SuspensionWindowDays);
        var recentCancellations = (await unitOfWork.Repository<BookingCancellation>().FindAsync(cancellation =>
                cancellation.ActorRole == UserRole.Worker &&
                cancellation.ReasonCode != null && cancellation.ReasonCode.StartsWith(WorkerCancelReasonCodes.Prefix) &&
                cancellation.CreatedAt >= cutoff))
            .ToList();
        if (recentCancellations.Count == 0) return;

        var countsByWorker = recentCancellations
            .GroupBy(cancellation => cancellation.CancelledBy)
            .ToDictionary(group => group.Key, group => group.Count());
        var candidateWorkerIds = countsByWorker.Keys.ToHashSet();

        var workers = await unitOfWork.Repository<WorkerProfile>().FindAsync(worker =>
            candidateWorkerIds.Contains(worker.UserId) && worker.SuspendedAt == null);

        foreach (var worker in workers)
        {
            if (countsByWorker[worker.UserId] < CancellationConstants.SuspensionStrikeThreshold) continue;

            worker.SuspendedAt = DateTime.UtcNow;
            worker.OnlineStatus = WorkerOnlineStatus.Offline;
            worker.UpdatedAt = DateTime.UtcNow;
            unitOfWork.Repository<WorkerProfile>().Update(worker);
            await unitOfWork.SaveChangesAsync();

            if (dispatchPublisher != null)
                await dispatchPublisher.WorkerSuspendedAsync(worker.UserId);
        }
    }
}

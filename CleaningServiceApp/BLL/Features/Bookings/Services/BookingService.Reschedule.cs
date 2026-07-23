using Cleaning.BLL.Common;
using Cleaning.BLL.Constants;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;

namespace Cleaning.BLL.Features.Bookings;

public partial class BookingService
{
    public async Task<BookingDto?> ProposeRescheduleAsync(Guid bookingId, Guid actorId, ProposeRescheduleDto request)
    {
        var booking = await _unitOfWork.ExecuteInTransactionAsync(
            _logger, AppErrors.RescheduleNotAllowed, async () =>
        {
            var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId);
            if (booking == null) throw new AppException(AppErrors.BookingNotFound);

            var isClient = booking.ClientId == actorId;
            var isWorker = booking.WorkerId == actorId;
            if (!isClient && !isWorker) throw new AppException(AppErrors.Forbidden);

            if (booking.BookingType != BookingType.Scheduled)
                throw new AppException(AppErrors.RescheduleImmediateNotAllowed);
            if (booking.Status != BookingStatus.Accepted)
                throw new AppException(AppErrors.RescheduleNotAllowed);

            var hasPending = await _unitOfWork.Repository<BookingRescheduleRequest>().ExistsAsync(
                r => r.BookingId == bookingId && r.Status == RescheduleStatus.Pending);
            if (hasPending) throw new AppException(AppErrors.RescheduleAlreadyPending);

            var newStart = request.NewStartTime.ToUniversalTime();
            BookingAvailabilityService.ValidateScheduledStartTime(newStart);
            var duration = booking.ScheduledEndTime - booking.ScheduledStartTime;

            await _unitOfWork.Repository<BookingRescheduleRequest>().AddAsync(new BookingRescheduleRequest
            {
                BookingId = booking.Id,
                RequestedBy = actorId,
                OldStartTime = booking.ScheduledStartTime,
                OldEndTime = booking.ScheduledEndTime,
                NewStartTime = newStart,
                NewEndTime = newStart.Add(duration),
                Status = RescheduleStatus.Pending,
                Reason = request.Message,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = booking.ScheduledStartTime.AddHours(-BookingTimingConstants.PreStartResponseCutoffHours)
            });

            var oldStatus = booking.Status;
            booking.Status = BookingStatus.RescheduleRequested;
            booking.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<Booking>().Update(booking);

            await _unitOfWork.Repository<BookingStatusLog>().AddAsync(new BookingStatusLog
            {
                BookingId = booking.Id,
                OldStatus = oldStatus,
                NewStatus = BookingStatus.RescheduleRequested,
                ChangedBy = actorId,
                Reason = BookingReasons.RescheduleProposed,
                CreatedAt = DateTime.UtcNow
            });

            await _unitOfWork.SaveChangesAsync();

            return booking;
        });

        if (_dispatchPublisher != null)
            await _dispatchPublisher.BookingStatusChangedAsync(booking.Id, booking.ClientId, booking.Status.ToString());

        return await GetBookingByIdAsync(booking.Id, actorId);
    }

    public async Task<BookingDto?> RespondRescheduleAsync(
        Guid bookingId, Guid requestId, Guid actorId, RescheduleAction action, bool isSystemActor = false)
    {
        var booking = await _unitOfWork.ExecuteInTransactionAsync(
            _logger, AppErrors.RescheduleRequestNotPending, async () =>
        {
            var reschedule = await _unitOfWork.Repository<BookingRescheduleRequest>().GetByIdAsync(requestId);
            if (reschedule == null || reschedule.BookingId != bookingId)
                throw new AppException(AppErrors.RescheduleRequestNotFound);
            if (reschedule.Status != RescheduleStatus.Pending)
                throw new AppException(AppErrors.RescheduleRequestNotPending);

            var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId);
            if (booking == null) throw new AppException(AppErrors.BookingNotFound);

            if (!isSystemActor)
            {
                var isParticipant = booking.ClientId == actorId || booking.WorkerId == actorId;
                if (!isParticipant) throw new AppException(AppErrors.Forbidden);

                var isRequester = reschedule.RequestedBy == actorId;
                if (action == RescheduleAction.Withdraw && !isRequester)
                    throw new AppException(AppErrors.Forbidden);
                if (action is RescheduleAction.Accept or RescheduleAction.Reject && isRequester)
                    throw new AppException(AppErrors.Forbidden);
            }

            reschedule.Status = action switch
            {
                RescheduleAction.Accept => RescheduleStatus.Accepted,
                RescheduleAction.Reject => RescheduleStatus.Rejected,
                RescheduleAction.Withdraw => RescheduleStatus.Cancelled,
                _ => throw new AppException(AppErrors.ValidationError)
            };
            reschedule.RespondedBy = isSystemActor ? null : actorId;
            reschedule.RespondedAt = DateTime.UtcNow;
            _unitOfWork.Repository<BookingRescheduleRequest>().Update(reschedule);

            var oldStatus = booking.Status;
            if (action == RescheduleAction.Accept)
            {
                booking.ScheduledStartTime = reschedule.NewStartTime;
                booking.ScheduledEndTime = reschedule.NewEndTime;
            }
            booking.Status = BookingStatus.Accepted;
            booking.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<Booking>().Update(booking);

            var reasonText = isSystemActor
                ? BookingReasons.SystemRescheduleExpired
                : action switch
                {
                    RescheduleAction.Accept => BookingReasons.RescheduleAccepted,
                    RescheduleAction.Reject => BookingReasons.RescheduleRejected,
                    RescheduleAction.Withdraw => BookingReasons.RescheduleWithdrawn,
                    _ => string.Empty
                };

            await _unitOfWork.Repository<BookingStatusLog>().AddAsync(new BookingStatusLog
            {
                BookingId = booking.Id,
                OldStatus = oldStatus,
                NewStatus = BookingStatus.Accepted,
                ChangedBy = isSystemActor ? null : actorId,
                Reason = reasonText,
                CreatedAt = DateTime.UtcNow
            });

            await _unitOfWork.SaveChangesAsync();

            return booking;
        });

        if (_dispatchPublisher != null)
            await _dispatchPublisher.BookingStatusChangedAsync(booking.Id, booking.ClientId, booking.Status.ToString());

        return isSystemActor ? null : await GetBookingByIdAsync(booking.Id, actorId);
    }
}

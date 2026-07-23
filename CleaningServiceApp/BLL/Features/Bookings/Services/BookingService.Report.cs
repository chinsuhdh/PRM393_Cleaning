using Cleaning.BLL.Common;
using Cleaning.BLL.Constants;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;

namespace Cleaning.BLL.Features.Bookings;

public partial class BookingService
{
    private static readonly BookingStatus[] ReportableStatuses =
    [
        BookingStatus.Accepted,
        BookingStatus.OnTheWay,
        BookingStatus.InProgress,
        BookingStatus.PendingPayment
    ];

    public async Task ReportBookingAsync(Guid bookingId, Guid actorId, ReportBookingDto request)
    {
        var (booking, assignedWorkerId) = await _unitOfWork.ExecuteInTransactionAsync(
            _logger, AppErrors.ReportNotAllowed, async () =>
        {
            var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId);
            if (booking == null) throw new AppException(AppErrors.BookingNotFound);

            var isClient = booking.ClientId == actorId;
            var isWorker = booking.WorkerId == actorId;
            if (!isClient && !isWorker) throw new AppException(AppErrors.Forbidden);

            if (!ReportableStatuses.Contains(booking.Status))
                throw new AppException(AppErrors.ReportNotAllowed);

            var validCodes = isClient ? ReportReasonCodes.ClientCodes : ReportReasonCodes.WorkerCodes;
            if (!validCodes.TryGetValue(request.ReasonCode, out var reasonLabel))
                throw new AppException(AppErrors.ReportReasonInvalid);

            var freeText = request.FreeText.Trim();
            if (freeText.Length < CancellationConstants.ReportMinFreeTextLength)
                throw new AppException(AppErrors.ReportFreeTextTooShort);

            var oldStatus = booking.Status;
            var assignedWorkerId = booking.WorkerId;
            var reasonText = $"{reasonLabel}: {freeText}";

            booking.Status = BookingStatus.Cancelled;
            booking.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<Booking>().Update(booking);

            await _unitOfWork.Repository<BookingCancellation>().AddAsync(new BookingCancellation
            {
                BookingId = booking.Id,
                CancelledBy = actorId,
                ActorRole = isClient ? UserRole.Client : UserRole.Worker,
                ReasonCode = request.ReasonCode,
                Reason = reasonText,
                CreatedAt = DateTime.UtcNow
            });

            await _unitOfWork.Repository<BookingStatusLog>().AddAsync(new BookingStatusLog
            {
                BookingId = booking.Id,
                OldStatus = oldStatus,
                NewStatus = BookingStatus.Cancelled,
                ChangedBy = actorId,
                Reason = reasonText,
                CreatedAt = DateTime.UtcNow
            });

            if (assignedWorkerId.HasValue)
            {
                var workerProfile = await _unitOfWork.Repository<WorkerProfile>().GetByIdAsync(assignedWorkerId.Value);
                if (workerProfile?.OnlineStatus == WorkerOnlineStatus.Busy)
                {
                    workerProfile.OnlineStatus = WorkerOnlineStatus.Online;
                    workerProfile.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Repository<WorkerProfile>().Update(workerProfile);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            return (booking, assignedWorkerId);
        });

        if (_dispatchPublisher != null)
        {
            await _dispatchPublisher.BookingStatusChangedAsync(booking.Id, booking.ClientId, booking.Status.ToString());
            if (assignedWorkerId.HasValue)
                await _dispatchPublisher.JobCancelledAsync(booking.Id, [assignedWorkerId.Value]);
        }
    }
}

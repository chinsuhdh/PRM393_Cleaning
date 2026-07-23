using Cleaning.BLL.Common;
using Cleaning.BLL.Constants;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;

namespace Cleaning.BLL.Features.Bookings;

public partial class BookingService
{
    public async Task<bool> CancelByClientAsync(Guid bookingId, Guid clientId)
    {
        var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId);
        if (booking == null || booking.ClientId != clientId || booking.Status != BookingStatus.AwaitingWorker)
            return false;

        return await UpdateBookingStatusAsync(
            bookingId, clientId, new UpdateBookingStatusDto { NewStatus = BookingStatus.Cancelled });
    }

    public async Task WorkerCancelAsync(Guid bookingId, Guid workerId, WorkerCancelBookingDto request)
    {
        if (!WorkerCancelReasonCodes.All.Contains(request.ReasonCode))
            throw new AppException(AppErrors.WorkerCancelReasonRequired);
        if (request.ReasonCode == WorkerCancelReasonCodes.Other && string.IsNullOrWhiteSpace(request.FreeText))
            throw new AppException(AppErrors.WorkerCancelReasonRequired);

        var (booking, _, justSuspended) = await _unitOfWork.ExecuteInTransactionAsync(
            _logger, AppErrors.BookingCancelNotAllowed, async () =>
        {
            var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId);
            if (booking == null || booking.WorkerId != workerId || booking.Status != BookingStatus.Accepted)
                throw new AppException(AppErrors.BookingCancelNotAllowed);

            var workerProfile = await _unitOfWork.Repository<WorkerProfile>().GetByIdAsync(workerId);
            if (workerProfile == null) throw new AppException(AppErrors.WorkerProfileNotFound);

            var priorCancelCount = await CountRecentPlainCancelsAsync(workerId);

            var reasonLabel = WorkerCancelReasonCodes.Labels[request.ReasonCode];
            var reasonText = request.ReasonCode == WorkerCancelReasonCodes.Other
                ? $"{reasonLabel}: {request.FreeText!.Trim()}"
                : reasonLabel;

            booking.WorkerId = null;
            booking.Status = BookingStatus.AwaitingWorker;
            booking.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<Booking>().Update(booking);

            await _unitOfWork.Repository<BookingCancellation>().AddAsync(new BookingCancellation
            {
                BookingId = booking.Id,
                CancelledBy = workerId,
                ActorRole = UserRole.Worker,
                ReasonCode = request.ReasonCode,
                Reason = reasonText,
                CreatedAt = DateTime.UtcNow
            });

            await _unitOfWork.Repository<BookingStatusLog>().AddAsync(new BookingStatusLog
            {
                BookingId = booking.Id,
                OldStatus = BookingStatus.Accepted,
                NewStatus = BookingStatus.AwaitingWorker,
                ChangedBy = workerId,
                Reason = reasonText,
                CreatedAt = DateTime.UtcNow
            });

            if (workerProfile.OnlineStatus == WorkerOnlineStatus.Busy)
                workerProfile.OnlineStatus = WorkerOnlineStatus.Online;

            var justSuspended = false;
            if (!workerProfile.SuspendedAt.HasValue &&
                priorCancelCount + 1 >= CancellationConstants.SuspensionStrikeThreshold)
            {
                workerProfile.SuspendedAt = DateTime.UtcNow;
                workerProfile.OnlineStatus = WorkerOnlineStatus.Offline;
                justSuspended = true;
            }
            workerProfile.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<WorkerProfile>().Update(workerProfile);

            await _unitOfWork.SaveChangesAsync();

            return (booking, workerProfile, justSuspended);
        });

        if (_dispatchPublisher != null)
        {
            await _dispatchPublisher.BookingStatusChangedAsync(booking.Id, booking.ClientId, booking.Status.ToString());

            await BroadcastBookingAsync(booking.Id);
            if (justSuspended)
                await _dispatchPublisher.WorkerSuspendedAsync(workerId);
        }
    }

    public async Task ClientCancelAsync(Guid bookingId, Guid clientId, ClientCancelBookingDto request)
    {
        if (!ClientCancelReasonCodes.All.Contains(request.ReasonCode))
            throw new AppException(AppErrors.ClientCancelReasonRequired);
        if (request.ReasonCode == ClientCancelReasonCodes.Other && string.IsNullOrWhiteSpace(request.FreeText))
            throw new AppException(AppErrors.ClientCancelReasonRequired);

        var booking = await _unitOfWork.ExecuteInTransactionAsync(
            _logger, AppErrors.BookingCancelNotAllowed, async () =>
        {
            var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId);
            if (booking == null || booking.ClientId != clientId || booking.Status != BookingStatus.Accepted)
                throw new AppException(AppErrors.BookingCancelNotAllowed);

            var reasonLabel = ClientCancelReasonCodes.Labels[request.ReasonCode];
            var reasonText = request.ReasonCode == ClientCancelReasonCodes.Other
                ? $"{reasonLabel}: {request.FreeText!.Trim()}"
                : reasonLabel;

            booking.WorkerId = null;
            booking.Status = BookingStatus.AwaitingWorker;
            booking.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<Booking>().Update(booking);

            await _unitOfWork.Repository<BookingCancellation>().AddAsync(new BookingCancellation
            {
                BookingId = booking.Id,
                CancelledBy = clientId,
                ActorRole = UserRole.Client,
                ReasonCode = request.ReasonCode,
                Reason = reasonText,
                CreatedAt = DateTime.UtcNow
            });

            await _unitOfWork.Repository<BookingStatusLog>().AddAsync(new BookingStatusLog
            {
                BookingId = booking.Id,
                OldStatus = BookingStatus.Accepted,
                NewStatus = BookingStatus.AwaitingWorker,
                ChangedBy = clientId,
                Reason = reasonText,
                CreatedAt = DateTime.UtcNow
            });

            await _unitOfWork.SaveChangesAsync();

            return booking;
        });

        if (_dispatchPublisher != null)
        {
            await _dispatchPublisher.BookingStatusChangedAsync(booking.Id, booking.ClientId, booking.Status.ToString());
            await BroadcastBookingAsync(booking.Id);
        }
    }

    public async Task<int> CountRecentPlainCancelsAsync(Guid workerId)
    {
        var cutoff = DateTime.UtcNow.AddDays(-CancellationConstants.SuspensionWindowDays);
        var cancellations = await _unitOfWork.Repository<BookingCancellation>().FindAsync(c =>
            c.CancelledBy == workerId &&
            c.ActorRole == UserRole.Worker &&
            c.ReasonCode != null && c.ReasonCode.StartsWith(WorkerCancelReasonCodes.Prefix) &&
            c.CreatedAt >= cutoff);
        return cancellations.Count();
    }
}

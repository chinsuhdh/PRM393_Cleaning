using Cleaning.BLL.Constants;
using Cleaning.BLL.DTOs;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Microsoft.Extensions.Logging;

namespace Cleaning.BLL.Services;

public partial class BookingService
{
    public async Task BroadcastBookingAsync(Guid bookingId)
    {
        if (_dispatchPublisher == null) return;
        var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId);
        if (booking == null || booking.Status != BookingStatus.AwaitingWorker) return;

        // Marks "when the current search window started" for the client's countdown: a fresh
        // broadcast (first post, a manual retry after timeout, or a worker releasing the job back to
        // AwaitingWorker) should restart the search timer, not leave it stuck at whenever the booking
        // was originally created.
        booking.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<Booking>().Update(booking);
        await _unitOfWork.SaveChangesAsync();

        await HydrateAsync([booking]);
        await _dispatchPublisher.JobPostedAsync(_mapper.Map<BookingDto>(booking), await EligibleWorkerIdsAsync(booking));
    }

    public async Task<IEnumerable<BookingDto>> GetAvailableBookingsAsync(Guid workerId)
    {
        var candidates = (await _unitOfWork.Repository<Booking>()
            .FindAsync(booking => booking.Status == BookingStatus.AwaitingWorker && booking.WorkerId == null))
            .ToList();
        var eligible = new List<Booking>();
        foreach (var booking in candidates)
            if (await IsEligibleAsync(booking, workerId)) eligible.Add(booking);
        await HydrateAsync(eligible);

        var worker = await _unitOfWork.Repository<WorkerProfile>().GetByIdAsync(workerId);
        var workerLat = worker?.BaseLatitude ?? worker?.CurrentLat;
        var workerLng = worker?.BaseLongitude ?? worker?.CurrentLng;

        var dtos = eligible.Select(_mapper.Map<BookingDto>).ToList();
        if (workerLat.HasValue && workerLng.HasValue)
        {
            foreach (var dto in dtos)
            {
                if (dto.Latitude.HasValue && dto.Longitude.HasValue)
                {
                    dto.DistanceKm = (decimal)GeoConstants.DistanceKm(
                        (double)workerLat.Value, (double)workerLng.Value,
                        (double)dto.Latitude.Value, (double)dto.Longitude.Value);
                }
            }
        }

        return dtos
            .OrderBy(dto => dto.BookingType == nameof(BookingType.Immediate) ? 0 : 1)
            .ThenBy(dto => dto.BookingType == nameof(BookingType.Immediate) ? dto.DistanceKm ?? decimal.MaxValue : 0)
            .ThenBy(dto => dto.ScheduledStartTime);
    }

    /// Anonymous coordinates only for nearby online, in-radius workers eligible for this booking —
    /// reuses the same eligibility view the dispatch broadcast itself is scoped to, rather than
    /// duplicating that logic. Only meaningful while the booking is still an Immediate search; any
    /// other state (already assigned, scheduled, not this client's own booking) yields an empty list
    /// rather than an error, matching how the search map already tolerates missing data.
    public async Task<IReadOnlyList<NearbyWorkerLocationDto>> GetNearbyOnlineWorkerLocationsAsync(
        Guid bookingId, Guid requestingClientId)
    {
        var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId);
        if (booking == null || booking.ClientId != requestingClientId ||
            booking.BookingType != BookingType.Immediate || booking.Status != BookingStatus.AwaitingWorker)
            return [];

        var rows = await _unitOfWork.Repository<VOnlineWorkersForImmediateBooking>()
            .FindAsync(row => row.BookingId == bookingId);
        return rows
            .Where(row => row.CurrentLat.HasValue && row.CurrentLng.HasValue)
            .Select(row => new NearbyWorkerLocationDto { Latitude = row.CurrentLat!.Value, Longitude = row.CurrentLng!.Value })
            .ToList();
    }

    public async Task<bool> HideBookingAsync(Guid bookingId, Guid workerId)
    {
        var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId);
        if (booking == null || booking.ClientId == workerId) return false;
        var repository = _unitOfWork.Repository<WorkerHiddenBooking>();
        if (await repository.ExistsAsync(item => item.WorkerId == workerId && item.BookingId == bookingId))
            return true;
        await repository.AddAsync(new WorkerHiddenBooking
        {
            WorkerId = workerId,
            BookingId = bookingId,
            CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AcceptBookingAsync(Guid bookingId, Guid workerId)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId);
            if (booking == null || booking.WorkerId != null ||
                booking.Status != BookingStatus.AwaitingWorker ||
                !await IsEligibleAsync(booking, workerId) ||
                await HasScheduleConflictAsync(booking, workerId))
                return false;

            booking.WorkerId = workerId;
            booking.Status = BookingStatus.Accepted;
            booking.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<Booking>().Update(booking);

            if (booking.BookingType == BookingType.Immediate)
            {
                var profile = await _unitOfWork.Repository<WorkerProfile>().GetByIdAsync(workerId);
                if (profile != null)
                {
                    profile.OnlineStatus = WorkerOnlineStatus.Busy;
                    profile.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Repository<WorkerProfile>().Update(profile);
                }
            }
            await _unitOfWork.Repository<BookingStatusLog>().AddAsync(new BookingStatusLog
            {
                BookingId = booking.Id,
                OldStatus = BookingStatus.AwaitingWorker,
                NewStatus = BookingStatus.Accepted,
                ChangedBy = workerId,
                Reason = BookingReasons.WorkerAcceptedBooking,
                CreatedAt = DateTime.UtcNow
            });
            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();
            if (_dispatchPublisher != null)
            {
                await _dispatchPublisher.JobTakenAsync(booking.Id, await EligibleWorkerIdsAsync(booking, includeTaken: true));
                await _dispatchPublisher.BookingStatusChangedAsync(booking.Id, nameof(BookingStatus.Accepted));
            }
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Worker {WorkerId} could not accept booking {BookingId}", workerId, bookingId);
            return false;
        }
    }

    private async Task<bool> IsEligibleAsync(Booking booking, Guid workerId)
    {
        if (booking.ClientId == workerId || booking.Status != BookingStatus.AwaitingWorker ||
            booking.WorkerId != null) return false;
        if (await _unitOfWork.Repository<WorkerHiddenBooking>().ExistsAsync(
                hidden => hidden.WorkerId == workerId && hidden.BookingId == booking.Id)) return false;
        if (!await _unitOfWork.Repository<Cleaning.DAL.Entities.WorkerService>().ExistsAsync(
                skill => skill.WorkerId == workerId && skill.ServiceId == booking.ServiceId && skill.IsVerified))
            return false;
        var worker = await _unitOfWork.Repository<WorkerProfile>().GetByIdAsync(workerId);
        if (worker == null || worker.VerificationStatus != BookingDomainConstants.WorkerVerificationStatusApproved || worker.SuspendedAt.HasValue)
            return false;

        if (booking.BookingType == BookingType.Immediate && worker.OnlineStatus == WorkerOnlineStatus.Offline)
            return false;

        if (booking.BookingType == BookingType.Scheduled && await HasScheduleConflictAsync(booking, workerId))
            return false;
        var address = booking.AddressId.HasValue
            ? await _unitOfWork.Repository<UserAddress>().GetByIdAsync(booking.AddressId.Value)
            : null;
        if (!IsInsideArea(worker, address)) return false;
        return true;
    }

    /// Real time conflicts: the worker's own accepted/active bookings, or a blocked availability range.
    /// Always blocks *accepting* a job (both booking types); also gates *browse* eligibility (E.1) but
    /// only for Scheduled bookings — see IsEligibleAsync.
    private async Task<bool> HasScheduleConflictAsync(Booking booking, Guid workerId)
    {
        if (await _unitOfWork.Repository<WorkerAvailability>().ExistsAsync(block =>
                block.WorkerId == workerId && block.Status == AvailabilityStatus.Blocked &&
                block.StartTime < booking.ScheduledEndTime && block.EndTime > booking.ScheduledStartTime))
            return true;
        if (await _unitOfWork.Repository<Booking>().ExistsAsync(existing =>
                existing.Id != booking.Id && existing.WorkerId == workerId &&
                (existing.Status == BookingStatus.Accepted || existing.Status == BookingStatus.OnTheWay ||
                 existing.Status == BookingStatus.InProgress) &&
                existing.ScheduledStartTime < booking.ScheduledEndTime &&
                existing.ScheduledEndTime > booking.ScheduledStartTime))
            return true;
        return false;
    }

    private async Task<IReadOnlyCollection<Guid>> EligibleWorkerIdsAsync(Booking booking, bool includeTaken = false)
    {
        var workers = await _unitOfWork.Repository<WorkerProfile>().GetAllAsync();
        var result = new List<Guid>();
        var originalStatus = booking.Status;
        var originalWorker = booking.WorkerId;
        if (includeTaken)
        {
            booking.Status = BookingStatus.AwaitingWorker;
            booking.WorkerId = null;
        }
        foreach (var worker in workers)
            if (await IsEligibleAsync(booking, worker.UserId)) result.Add(worker.UserId);
        booking.Status = originalStatus;
        booking.WorkerId = originalWorker;
        return result;
    }

    private static bool IsInsideArea(WorkerProfile worker, UserAddress? address)
    {
        if (address?.Latitude == null || address.Longitude == null) return true;
        var latitude = worker.BaseLatitude ?? worker.CurrentLat;
        var longitude = worker.BaseLongitude ?? worker.CurrentLng;
        if (latitude == null || longitude == null) return false;
        return GeoConstants.DistanceKm(
            (double)latitude.Value, (double)longitude.Value,
            (double)address.Latitude.Value, (double)address.Longitude.Value) <= (double)worker.ServiceRadiusKm;
    }
}

using Cleaning.BLL.Common;
using Cleaning.BLL.Constants;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Microsoft.Extensions.Logging;

namespace Cleaning.BLL.Features.Bookings;

public partial class BookingService
{
    public async Task BroadcastBookingAsync(Guid bookingId)
    {
        if (_dispatchPublisher == null) return;
        var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId);
        if (booking == null || booking.Status != BookingStatus.AwaitingWorker) return;

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
        var workerLat = worker?.CurrentLat ?? worker?.BaseLatitude;
        var workerLng = worker?.CurrentLng ?? worker?.BaseLongitude;

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
                    dto.EstimatedMinutes = (decimal)GeoConstants.EstimatedMinutes((double)dto.DistanceKm.Value);
                }
            }
        }

        return dtos
            .OrderBy(dto => dto.BookingType == nameof(BookingType.Immediate) ? 0 : 1)
            .ThenBy(dto => dto.BookingType == nameof(BookingType.Immediate) ? dto.DistanceKm ?? decimal.MaxValue : 0)
            .ThenBy(dto => dto.ScheduledStartTime);
    }

    public async Task<IReadOnlyList<NearbyWorkerLocationDto>> GetNearbyOnlineWorkerLocationsAsync(
        Guid bookingId, Guid requestingClientId)
    {
        var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId);
        if (booking == null || booking.ClientId != requestingClientId || booking.Status != BookingStatus.AwaitingWorker)
            return [];

        if (booking.BookingType == BookingType.Immediate)
        {
            var rows = await _unitOfWork.Repository<VOnlineWorkersForImmediateBooking>()
                .FindAsync(row => row.BookingId == bookingId);
            return rows
                .Where(row => row.CurrentLat.HasValue && row.CurrentLng.HasValue)
                .Select(row => new NearbyWorkerLocationDto { Latitude = row.CurrentLat!.Value, Longitude = row.CurrentLng!.Value })
                .ToList();
        }

        if (booking.BookingType == BookingType.Scheduled)
        {
            var rows = await _unitOfWork.Repository<VAvailableWorkersForScheduledBooking>()
                .FindAsync(row => row.BookingId == bookingId);
            return rows
                .Where(row => row.CurrentLat.HasValue && row.CurrentLng.HasValue)
                .Select(row => new NearbyWorkerLocationDto { Latitude = row.CurrentLat!.Value, Longitude = row.CurrentLng!.Value })
                .ToList();
        }

        return [];
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
                await _dispatchPublisher.BookingStatusChangedAsync(booking.Id, booking.ClientId, nameof(BookingStatus.Accepted));
            }
            return true;
        }
        catch (AppException)
        {
            await transaction.RollbackAsync();
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Worker {WorkerId} could not accept booking {BookingId}", workerId, bookingId);
            throw new AppException(AppErrors.BookingAcceptFailed, ex);
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
        var originalStatus = booking.Status;
        var originalWorker = booking.WorkerId;
        if (includeTaken)
        {
            booking.Status = BookingStatus.AwaitingWorker;
            booking.WorkerId = null;
        }

        IReadOnlyCollection<Guid> result;
        if (booking.Status != BookingStatus.AwaitingWorker || booking.WorkerId != null)
        {
            result = [];
        }
        else
        {
            var workers = (await _unitOfWork.Repository<WorkerProfile>().GetAllAsync())
                .Where(w => w.UserId != booking.ClientId
                    && w.VerificationStatus == BookingDomainConstants.WorkerVerificationStatusApproved
                    && !w.SuspendedAt.HasValue
                    && (booking.BookingType != BookingType.Immediate || w.OnlineStatus != WorkerOnlineStatus.Offline))
                .ToList();
            var candidateIds = workers.Select(w => w.UserId).ToHashSet();

            var hiddenWorkerIds = (await _unitOfWork.Repository<WorkerHiddenBooking>()
                .FindAsync(h => h.BookingId == booking.Id && candidateIds.Contains(h.WorkerId)))
                .Select(h => h.WorkerId)
                .ToHashSet();

            var verifiedSkillWorkerIds = (await _unitOfWork.Repository<Cleaning.DAL.Entities.WorkerService>()
                .FindAsync(s => s.ServiceId == booking.ServiceId && s.IsVerified && candidateIds.Contains(s.WorkerId)))
                .Select(s => s.WorkerId)
                .ToHashSet();

            var conflictedWorkerIds = new HashSet<Guid>();
            if (booking.BookingType == BookingType.Scheduled)
            {
                var blockedIds = (await _unitOfWork.Repository<WorkerAvailability>().FindAsync(block =>
                    block.Status == AvailabilityStatus.Blocked &&
                    block.StartTime < booking.ScheduledEndTime && block.EndTime > booking.ScheduledStartTime &&
                    candidateIds.Contains(block.WorkerId)))
                    .Select(block => block.WorkerId);
                var conflictingBookingWorkerIds = (await _unitOfWork.Repository<Booking>().FindAsync(existing =>
                    existing.Id != booking.Id && existing.WorkerId != null &&
                    (existing.Status == BookingStatus.Accepted || existing.Status == BookingStatus.OnTheWay ||
                     existing.Status == BookingStatus.InProgress) &&
                    existing.ScheduledStartTime < booking.ScheduledEndTime && existing.ScheduledEndTime > booking.ScheduledStartTime))
                    .Select(existing => existing.WorkerId!.Value);
                conflictedWorkerIds = blockedIds.Concat(conflictingBookingWorkerIds).ToHashSet();
            }

            var address = booking.AddressId.HasValue
                ? await _unitOfWork.Repository<UserAddress>().GetByIdAsync(booking.AddressId.Value)
                : null;

            result = workers
                .Where(w => !hiddenWorkerIds.Contains(w.UserId)
                    && verifiedSkillWorkerIds.Contains(w.UserId)
                    && !conflictedWorkerIds.Contains(w.UserId)
                    && IsInsideArea(w, address))
                .Select(w => w.UserId)
                .ToList();
        }

        booking.Status = originalStatus;
        booking.WorkerId = originalWorker;
        return result;
    }

    private static bool IsInsideArea(WorkerProfile worker, UserAddress? address)
    {
        if (address?.Latitude == null || address.Longitude == null) return false;
        var latitude = worker.CurrentLat ?? worker.BaseLatitude;
        var longitude = worker.CurrentLng ?? worker.BaseLongitude;
        if (latitude == null || longitude == null) return false;
        return GeoConstants.DistanceKm(
            (double)latitude.Value, (double)longitude.Value,
            (double)address.Latitude.Value, (double)address.Longitude.Value) <= (double)worker.ServiceRadiusKm;
    }
}

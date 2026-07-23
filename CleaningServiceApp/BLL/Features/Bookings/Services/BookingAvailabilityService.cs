using Cleaning.BLL.Common;
using Cleaning.BLL.Constants;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Cleaning.DAL.Interfaces;

namespace Cleaning.BLL.Features.Bookings;

public sealed class BookingAvailabilityService(IUnitOfWork unitOfWork) : IBookingAvailabilityService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<BookingAvailabilityDto> GetAsync(Guid clientId, BookingAvailabilityRequestDto request)
    {
        var now = DateTime.UtcNow;
        var (service, address) = await ValidateAsync(clientId, request);
        var slots = await FindAvailableSlotsAsync(address, request, now);
        return new BookingAvailabilityDto
        {
            BookingType = request.BookingType,
            GeneratedAt = now,
            ValidUntil = now.AddMinutes(BookingTimingConstants.QuoteValidityMinutes),
            Slots = slots,
            EmptyReasonCode = slots.Count == 0 ? AppErrors.NoAvailableWorker.Code : null,
            EmptyMessage = slots.Count == 0 ? AppErrors.NoAvailableWorker.Message : null
        };
    }

    public async Task<(Service Service, UserAddress Address)> ValidateAsync(
        Guid clientId,
        BookingAvailabilityRequestDto request)
    {
        var service = await _unitOfWork.Repository<Service>().GetByIdAsync(request.ServiceId);
        if (service == null || !service.IsActive || service.ArchivedAt.HasValue)
            throw new AppException(AppErrors.ServiceUnavailable);

        var address = await _unitOfWork.Repository<UserAddress>().GetByIdAsync(request.AddressId);
        if (address == null || address.UserId != clientId)
            throw new AppException(AppErrors.AddressForbidden);

        if (request.DurationHours < service.MinimumHours)
            throw new AppException(AppErrors.DurationInvalid);

        if (request.BookingType == BookingType.Scheduled)
        {
            var from = request.From?.ToUniversalTime()
                ?? throw new AppException(AppErrors.StartRequired);
            ValidateScheduledStartTime(from);
        }

        return (service, address);
    }

    public static void ValidateScheduledStartTime(DateTime from)
    {
        if (from < DateTime.UtcNow.AddHours(BookingTimingConstants.ScheduledLeadHours))
            throw new AppException(AppErrors.StartTooSoon);
        if (from > DateTime.UtcNow.AddDays(BookingTimingConstants.MaxAdvanceSchedulingDays))
            throw new AppException(AppErrors.TimeSlotInvalid);
        if (from.Minute is not (0 or 30) || from.Second != 0)
            throw new AppException(AppErrors.TimeSlotInvalid);
    }

    private async Task<List<BookingSlotDto>> FindAvailableSlotsAsync(
        UserAddress address,
        BookingAvailabilityRequestDto request,
        DateTime now)
    {
        var starts = GetCandidateStarts(request, now).ToList();
        if (starts.Count == 0) return [];

        var skills = await _unitOfWork.Repository<DAL.Entities.WorkerService>()
            .FindAsync(item => item.ServiceId == request.ServiceId && item.IsVerified);
        var skilledWorkerIds = skills.Select(item => item.WorkerId).ToHashSet();
        if (skilledWorkerIds.Count == 0) return [];

        var workers = (await _unitOfWork.Repository<WorkerProfile>().FindAsync(worker =>
                skilledWorkerIds.Contains(worker.UserId) &&
                worker.VerificationStatus == BookingDomainConstants.WorkerVerificationStatusApproved &&
                worker.SuspendedAt == null))
            .Where(worker => IsWorkerWithinServiceRadius(worker, address))
            .ToList();
        if (workers.Count == 0) return [];

        if (request.BookingType == BookingType.Immediate)
        {
            workers = workers.Where(worker =>
                worker.OnlineStatus == WorkerOnlineStatus.Online &&
                IsLocationFresh(worker, now)).ToList();
        }

        var workerIds = workers.Select(worker => worker.UserId).ToHashSet();
        var availabilityWindows = request.BookingType == BookingType.Scheduled
            ? (await _unitOfWork.Repository<WorkerAvailability>().FindAsync(window =>
                workerIds.Contains(window.WorkerId) &&
                window.Status == AvailabilityStatus.Available)).ToList()
            : [];

        var latestEnd = starts.Max(start => start.AddHours((double)request.DurationHours));
        var earliestStart = starts.Min();
        var blockingBookings = await _unitOfWork.Repository<Booking>().FindAsync(booking =>
            (booking.Status == BookingStatus.AwaitingWorker ||
                booking.Status == BookingStatus.Accepted ||
                booking.Status == BookingStatus.OnTheWay ||
                booking.Status == BookingStatus.InProgress) &&
            booking.ScheduledStartTime < latestEnd.AddMinutes(BookingTimingConstants.TravelBufferMinutes) &&
            booking.ScheduledEndTime > earliestStart.AddMinutes(-BookingTimingConstants.TravelBufferMinutes));

        var slots = new List<BookingSlotDto>();
        foreach (var start in starts)
        {
            var end = start.AddHours((double)request.DurationHours);

            var candidateWorkers = request.BookingType == BookingType.Immediate
                ? workers
                : workers.Where(worker => availabilityWindows.Any(window =>
                    window.WorkerId == worker.UserId &&
                    window.StartTime <= start &&
                    window.EndTime >= end)).ToList();

            if (candidateWorkers.Count == 0) continue;

            var freeWorkers = candidateWorkers.Count(worker => !blockingBookings.Any(booking =>
                booking.WorkerId == worker.UserId && OverlapsWithBuffer(booking, start, end)));
            var unassignedCapacityUsed = blockingBookings.Count(booking =>
                booking.WorkerId == null &&
                booking.ServiceId == request.ServiceId &&
                OverlapsWithBuffer(booking, start, end));

            if (freeWorkers > unassignedCapacityUsed)
            {
                slots.Add(new BookingSlotDto { StartTime = start, EndTime = end });
            }
        }

        return slots.Take(request.BookingType == BookingType.Immediate
            ? BookingTimingConstants.ImmediateSlotCap
            : BookingTimingConstants.ScheduledSlotCap).ToList();
    }

    private static IEnumerable<DateTime> GetCandidateStarts(BookingAvailabilityRequestDto request, DateTime now)
    {
        if (request.BookingType == BookingType.Immediate)
        {
            yield return RoundUp(now.AddMinutes(BookingTimingConstants.ImmediateLeadMinutes),
                TimeSpan.FromMinutes(BookingTimingConstants.ImmediateSlotRoundingMinutes));
            yield break;
        }

        var from = (request.From ?? now.AddHours(BookingTimingConstants.ScheduledLeadHours)).ToUniversalTime();
        var to = (request.To ?? from).ToUniversalTime();
        if (to <= from)
        {
            yield return from;
            yield break;
        }

        for (var start = RoundUp(from, TimeSpan.FromMinutes(BookingTimingConstants.SlotIntervalMinutes)); start <= to; start = start.AddMinutes(BookingTimingConstants.SlotIntervalMinutes))
        {
            yield return start;
        }
    }

    private static DateTime RoundUp(DateTime dateTime, TimeSpan interval)
    {
        var ticks = (dateTime.Ticks + interval.Ticks - 1) / interval.Ticks * interval.Ticks;
        return new DateTime(ticks, DateTimeKind.Utc);
    }

    private static bool OverlapsWithBuffer(Booking booking, DateTime start, DateTime end) =>
        booking.ScheduledStartTime < end.AddMinutes(BookingTimingConstants.TravelBufferMinutes) &&
        booking.ScheduledEndTime > start.AddMinutes(-BookingTimingConstants.TravelBufferMinutes);

    private static bool IsLocationFresh(WorkerProfile worker, DateTime now) =>
        worker.LocationUpdatedAt.HasValue &&
        worker.LocationUpdatedAt.Value >= now.AddMinutes(-BookingTimingConstants.LocationFreshnessMinutes);

    private static bool IsWorkerWithinServiceRadius(WorkerProfile worker, UserAddress address)
    {
        if (!address.Latitude.HasValue || !address.Longitude.HasValue)
            return false;

        var workerLat = worker.CurrentLat ?? worker.BaseLatitude;
        var workerLng = worker.CurrentLng ?? worker.BaseLongitude;
        if (!workerLat.HasValue || !workerLng.HasValue)
            return false;

        return GeoConstants.DistanceKm(
            (double)address.Latitude.Value,
            (double)address.Longitude.Value,
            (double)workerLat.Value,
            (double)workerLng.Value) <= (double)worker.ServiceRadiusKm;
    }
}

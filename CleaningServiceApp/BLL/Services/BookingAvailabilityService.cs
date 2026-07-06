using System.Text.Json;
using Cleaning.BLL.Common;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Cleaning.DAL.Interfaces;

namespace Cleaning.BLL.Services;

public sealed class BookingAvailabilityService(IUnitOfWork unitOfWork) : IBookingAvailabilityService
{
    private const int ImmediateLeadMinutes = 15;
    private const int ScheduledLeadHours = 2;
    private const int TravelBufferMinutes = 30;
    private const int LocationFreshnessMinutes = 10;
    private const int SlotIntervalMinutes = 30;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<BookingAvailabilityDto> GetAsync(Guid clientId, BookingAvailabilityRequestDto request)
    {
        var now = DateTime.UtcNow;
        var (service, address) = await ValidateAsync(clientId, request);
        var slots = await FindAvailableSlotsAsync(service, address, request, now);
        return new BookingAvailabilityDto
        {
            BookingType = request.BookingType,
            GeneratedAt = now,
            ValidUntil = now.AddMinutes(2),
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
            if (from < DateTime.UtcNow.AddHours(ScheduledLeadHours))
                throw new AppException(AppErrors.StartTooSoon);
            if (from > DateTime.UtcNow.AddDays(30))
                throw new AppException(AppErrors.TimeSlotInvalid);
            if (from.Minute is not (0 or 30) || from.Second != 0)
                throw new AppException(AppErrors.TimeSlotInvalid);
        }

        return (service, address);
    }

    private async Task<List<BookingSlotDto>> FindAvailableSlotsAsync(
        Service service,
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
                worker.VerificationStatus == "approved" &&
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
            booking.ScheduledStartTime < latestEnd.AddMinutes(TravelBufferMinutes) &&
            booking.ScheduledEndTime > earliestStart.AddMinutes(-TravelBufferMinutes));

        var slots = new List<BookingSlotDto>();
        foreach (var start in starts)
        {
            var end = start.AddHours((double)request.DurationHours);
            if (!IsWithinOperatingSchedule(service, start, end)) continue;

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

        return slots.Take(request.BookingType == BookingType.Immediate ? 1 : 12).ToList();
    }

    private static IEnumerable<DateTime> GetCandidateStarts(BookingAvailabilityRequestDto request, DateTime now)
    {
        if (request.BookingType == BookingType.Immediate)
        {
            yield return RoundUp(now.AddMinutes(ImmediateLeadMinutes), TimeSpan.FromMinutes(5));
            yield break;
        }

        var from = (request.From ?? now.AddHours(ScheduledLeadHours)).ToUniversalTime();
        var to = (request.To ?? from).ToUniversalTime();
        if (to <= from)
        {
            yield return from;
            yield break;
        }

        for (var start = RoundUp(from, TimeSpan.FromMinutes(SlotIntervalMinutes)); start <= to; start = start.AddMinutes(SlotIntervalMinutes))
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
        booking.ScheduledStartTime < end.AddMinutes(TravelBufferMinutes) &&
        booking.ScheduledEndTime > start.AddMinutes(-TravelBufferMinutes);

    private static bool IsLocationFresh(WorkerProfile worker, DateTime now) =>
        worker.LocationUpdatedAt.HasValue &&
        worker.LocationUpdatedAt.Value >= now.AddMinutes(-LocationFreshnessMinutes);

    private static bool IsWorkerWithinServiceRadius(WorkerProfile worker, UserAddress address)
    {
        if (!address.Latitude.HasValue || !address.Longitude.HasValue)
            return true;

        var workerLat = worker.CurrentLat ?? worker.BaseLatitude;
        var workerLng = worker.CurrentLng ?? worker.BaseLongitude;
        if (!workerLat.HasValue || !workerLng.HasValue)
            return false;

        return DistanceKm(
            (double)address.Latitude.Value,
            (double)address.Longitude.Value,
            (double)workerLat.Value,
            (double)workerLng.Value) <= (double)worker.ServiceRadiusKm;
    }

    private static double DistanceKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadiusKm = 6371;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLng = DegreesToRadians(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;

    public static bool IsWithinOperatingSchedule(Service service, DateTime start, DateTime end)
    {
        if (string.IsNullOrWhiteSpace(service.OperatingSchedule) || service.OperatingSchedule == "{}")
            return true;

        try
        {
            using var json = JsonDocument.Parse(service.OperatingSchedule);
            var dayKey = start.DayOfWeek.ToString().ToLowerInvariant();
            if (!json.RootElement.TryGetProperty(dayKey, out var day) &&
                !json.RootElement.TryGetProperty(((int)start.DayOfWeek).ToString(), out day))
                return false;

            if (day.ValueKind == JsonValueKind.False) return false;
            if (day.ValueKind == JsonValueKind.Object &&
                day.TryGetProperty("open", out var open) &&
                day.TryGetProperty("close", out var close) &&
                TimeSpan.TryParse(open.GetString(), out var openTime) &&
                TimeSpan.TryParse(close.GetString(), out var closeTime))
            {
                var startTime = start.TimeOfDay;
                var endTime = end.TimeOfDay;
                return start.Date == end.Date &&
                        startTime >= openTime &&
                        endTime <= closeTime;
            }

            return day.ValueKind == JsonValueKind.True;
        }
        catch (JsonException)
        {
            return true;
        }
    }
}

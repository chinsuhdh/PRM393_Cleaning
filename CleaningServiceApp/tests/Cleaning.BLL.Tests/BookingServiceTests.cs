using AutoMapper;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Common;
using Cleaning.BLL.Mapping;
using Cleaning.BLL.Services;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cleaning.BLL.Tests;

public sealed class BookingServiceTests
{
    [Fact(DisplayName = "[UT-BOOK-001-01] Immediate availability checks approved skill, fresh online location, radius, and state")]
    public async Task GetAvailabilityAsync_ImmediateWorker_ReturnsSlot()
    {
        var scenario = BookingScenario.Create();
        scenario.Worker.CurrentLat = scenario.Address.Latitude;
        scenario.Worker.CurrentLng = scenario.Address.Longitude;
        scenario.Worker.LocationUpdatedAt = DateTime.UtcNow;

        var result = await scenario.BookingService.GetAvailabilityAsync(scenario.ClientId, scenario.AvailabilityRequest(BookingType.Immediate));

        Assert.Single(result.Slots);
        Assert.Null(result.EmptyReasonCode);
    }

    [Fact(DisplayName = "[UT-BOOK-001-02] Wrong address owner is rejected before availability is exposed")]
    public async Task GetAvailabilityAsync_WrongAddressOwner_ThrowsForbidden()
    {
        var scenario = BookingScenario.Create();
        scenario.Address.UserId = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<AppException>(() =>
            scenario.BookingService.GetAvailabilityAsync(scenario.ClientId, scenario.AvailabilityRequest(BookingType.Scheduled)));

        Assert.Equal(403, error.StatusCode);
        Assert.Equal("BOOKING_ADDRESS_FORBIDDEN", error.Code);
    }

    [Fact(DisplayName = "[UT-BOOK-001-02] Immediate availability rejects stale worker location and out-of-radius workers")]
    public async Task GetAvailabilityAsync_ImmediateStaleOrTooFar_ReturnsEmpty()
    {
        var scenario = BookingScenario.Create();
        scenario.Worker.CurrentLat = 21.0278m;
        scenario.Worker.CurrentLng = 105.8342m;
        scenario.Worker.ServiceRadiusKm = 1;
        scenario.Worker.LocationUpdatedAt = DateTime.UtcNow.AddMinutes(-20);

        var result = await scenario.BookingService.GetAvailabilityAsync(scenario.ClientId, scenario.AvailabilityRequest(BookingType.Immediate));

        Assert.Empty(result.Slots);
        Assert.Equal("BOOKING_NO_AVAILABLE_WORKER", result.EmptyReasonCode);
    }

    [Fact(DisplayName = "[UT-BOOK-001-02] Scheduled availability respects service operating hours")]
    public async Task GetAvailabilityAsync_OutsideOperatingHours_ReturnsEmpty()
    {
        var scenario = BookingScenario.Create();
        scenario.ServiceEntity.OperatingSchedule = "{\"monday\":{\"open\":\"08:00\",\"close\":\"17:00\"}}";
        var mondayNight = new DateTime(2026, 7, 6, 18, 0, 0, DateTimeKind.Utc);

        var result = await scenario.BookingService.GetAvailabilityAsync(scenario.ClientId,
            scenario.AvailabilityRequest(BookingType.Scheduled, mondayNight, mondayNight));

        Assert.Empty(result.Slots);
        Assert.Equal("BOOKING_NO_AVAILABLE_WORKER", result.EmptyReasonCode);
    }

    [Fact(DisplayName = "[UT-BOOK-001-03] Existing booking overlaps and travel buffers consume capacity")]
    public async Task GetAvailabilityAsync_OverlapOrTravelBuffer_ReturnsEmpty()
    {
        var scenario = BookingScenario.Create();
        var start = new DateTime(2026, 7, 6, 9, 0, 0, DateTimeKind.Utc);
        scenario.Bookings.Add(new Booking
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            WorkerId = scenario.Worker.UserId,
            ServiceId = scenario.ServiceEntity.Id,
            ScheduledStartTime = start.AddHours(-2),
            ScheduledEndTime = start.AddMinutes(-15),
            DurationHours = 2,
            Status = BookingStatus.Accepted
        });

        var result = await scenario.BookingService.GetAvailabilityAsync(scenario.ClientId,
            scenario.AvailabilityRequest(BookingType.Scheduled, start, start));

        Assert.Empty(result.Slots);
        Assert.Equal("BOOKING_NO_AVAILABLE_WORKER", result.EmptyReasonCode);
    }

    [Fact(DisplayName = "[UT-BOOK-001-04] Scheduled booking requires at least two hours notice")]
    public async Task GetAvailabilityAsync_ScheduledTooSoon_ThrowsValidationError()
    {
        var scenario = BookingScenario.Create();
        var start = DateTime.UtcNow.AddMinutes(90);

        var error = await Assert.ThrowsAsync<AppException>(() =>
            scenario.BookingService.GetAvailabilityAsync(scenario.ClientId,
                scenario.AvailabilityRequest(BookingType.Scheduled, start, start)));

        Assert.Equal("BOOKING_START_TOO_SOON", error.Code);
    }

    [Fact(DisplayName = "[UT-BOOK-001-05] Immediate booking is queued for worker dispatch")]
    public async Task CreateBookingAsync_Immediate_SetsAwaitingWorker()
    {
        var scenario = BookingScenario.Create();

        var result = await scenario.BookingService.CreateBookingAsync(
            scenario.ClientId,
            "immediate-key",
            scenario.CreateRequest(BookingType.Immediate));

        Assert.Equal(nameof(BookingType.Immediate), result.BookingType);
        Assert.Equal(nameof(BookingStatus.AwaitingWorker), result.Status);
        Assert.Single(scenario.Bookings);
    }

    [Fact(DisplayName = "[UT-BOOK-001-06] Scheduled booking persists selected start and awaits payment")]
    public async Task CreateBookingAsync_Scheduled_SetsPendingPayment()
    {
        var scenario = BookingScenario.Create();
        var start = DateTime.UtcNow.AddHours(3);

        var result = await scenario.BookingService.CreateBookingAsync(
            scenario.ClientId,
            "scheduled-key",
            scenario.CreateRequest(BookingType.Scheduled, start));

        Assert.Equal(nameof(BookingType.Scheduled), result.BookingType);
        Assert.Equal(nameof(BookingStatus.PendingPayment), result.Status);
        Assert.Equal(start, result.ScheduledStartTime, TimeSpan.FromSeconds(1));
    }

    [Fact(DisplayName = "[UT-BOOK-001-07] Immediate booking is created even with no worker online yet, then dispatched")]
    public async Task CreateBookingAsync_ImmediateWithoutOnlineWorker_StillCreatesAwaitingWorker()
    {
        // New flow: creation never blocks on worker availability. The booking is created and left
        // AwaitingWorker so dispatch can offer it to workers who come online and accept.
        var scenario = BookingScenario.Create();
        scenario.Worker.OnlineStatus = WorkerOnlineStatus.Offline;

        var result = await scenario.BookingService.CreateBookingAsync(
            scenario.ClientId,
            "no-worker-key",
            scenario.CreateRequest(BookingType.Immediate));

        Assert.Equal(nameof(BookingStatus.AwaitingWorker), result.Status);
        Assert.Single(scenario.Bookings);
    }

    [Fact(DisplayName = "[UT-BOOK-003-03] Scheduled booking is created even when no worker is eligible yet")]
    public async Task CreateBookingAsync_ScheduledNoEligibleWorker_StillCreatesPendingPayment()
    {
        // Previously an ineligible worker meant no slot and a rejected booking. Now the booking is created
        // and matched later by dispatch.
        var scenario = BookingScenario.Create();
        scenario.Worker.VerificationStatus = "pending"; // not eligible for matching
        var start = DateTime.UtcNow.AddHours(3);

        var result = await scenario.BookingService.CreateBookingAsync(
            scenario.ClientId,
            "scheduled-no-worker-key",
            scenario.CreateRequest(BookingType.Scheduled, start));

        Assert.Equal(nameof(BookingStatus.PendingPayment), result.Status);
        Assert.Single(scenario.Bookings);
    }

    private sealed class BookingScenario
    {
        public Guid ClientId { get; } = Guid.NewGuid();
        public BookingService BookingService { get; private set; } = null!;
        public Service ServiceEntity { get; private set; } = null!;
        public UserAddress Address { get; private set; } = null!;
        public WorkerProfile Worker { get; private set; } = null!;
        public List<Booking> Bookings { get; } = [];

        public static BookingScenario Create()
        {
            var scenario = new BookingScenario();
            var serviceId = Guid.NewGuid();
            var addressId = Guid.NewGuid();
            var workerId = Guid.NewGuid();

            scenario.ServiceEntity = new Service
            {
                Id = serviceId,
                Name = "D?n nh?",
                IsActive = true,
                MinimumHours = 2,
                BasePrice = 100_000,
                OperatingSchedule = "{}"
            };
            scenario.Address = new UserAddress
            {
                Id = addressId,
                UserId = scenario.ClientId,
                Label = "Nh?",
                AddressText = "Qu?n 1",
                Latitude = 10.7769m,
                Longitude = 106.7009m
            };
            scenario.Worker = new WorkerProfile
            {
                UserId = workerId,
                VerificationStatus = "approved",
                ImmediateBookingEnabled = true,
                OnlineStatus = WorkerOnlineStatus.Online,
                CurrentLat = 10.7769m,
                CurrentLng = 106.7009m,
                BaseLatitude = 10.7769m,
                BaseLongitude = 106.7009m,
                ServiceRadiusKm = 10,
                LocationUpdatedAt = DateTime.UtcNow
            };

            var unitOfWork = new InMemoryUnitOfWork()
                .With([scenario.ServiceEntity])
                .With([scenario.Address])
                .With([new Cleaning.DAL.Entities.WorkerService
                {
                    WorkerId = workerId,
                    ServiceId = serviceId,
                    IsVerified = true
                }])
                .With([scenario.Worker])
                .With([new WorkerAvailability
                {
                    Id = Guid.NewGuid(),
                    WorkerId = workerId,
                    StartTime = DateTime.UtcNow.AddYears(-1),
                    EndTime = DateTime.UtcNow.AddYears(1),
                    Status = AvailabilityStatus.Available
                }])
                .With(scenario.Bookings)
                .With(new List<BookingStatusLog>());
            var availabilityService = new BookingAvailabilityService(unitOfWork);
            var mapper = new MapperConfiguration(
                configuration => configuration.AddProfile<BookingMappingProfile>(),
                NullLoggerFactory.Instance).CreateMapper();
            var creationService = new BookingCreationService(
                unitOfWork,
                availabilityService,
                NullLogger<BookingCreationService>.Instance,
                mapper);
            scenario.BookingService = new BookingService(
                unitOfWork,
                NullLogger<BookingService>.Instance,
                availabilityService,
                creationService,
                mapper);
            return scenario;
        }

        public BookingAvailabilityRequestDto AvailabilityRequest(
            BookingType bookingType,
            DateTime? from = null,
            DateTime? to = null) => new()
        {
            ServiceId = ServiceEntity.Id,
            AddressId = Address.Id,
            BookingType = bookingType,
            DurationHours = 2,
            From = from ?? new DateTime(2026, 7, 6, 9, 0, 0, DateTimeKind.Utc),
            To = to ?? new DateTime(2026, 7, 6, 9, 0, 0, DateTimeKind.Utc)
        };

        public CreateBookingDto CreateRequest(BookingType bookingType, DateTime? start = null) => new()
        {
            ServiceId = ServiceEntity.Id,
            AddressId = Address.Id,
            BookingType = bookingType,
            ScheduledStartTime = start,
            DurationHours = 2
        };
    }

}

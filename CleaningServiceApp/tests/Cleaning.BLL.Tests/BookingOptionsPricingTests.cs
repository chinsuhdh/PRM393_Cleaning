using System.Text.Json;
using AutoMapper;
using Cleaning.BLL.Common;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Mapping;
using Cleaning.BLL.Services;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cleaning.BLL.Tests;

// Covers BOOK-002 (service-defined questions), BOOK-003 (valid-slot exposure/revalidation),
// and BOOK-004 (server-calculated pricing). The API is authoritative for validation and pricing,
// so these assertions run against the BLL services that back the booking endpoints.
public sealed class BookingOptionsPricingTests
{
    // ----------------------- BOOK-002: service-defined questions -----------------------

    [Fact(DisplayName = "[UT-BOOK-002-01] Valid answers matching the schema are normalized and persisted")]
    public async Task Create_ValidAnswers_PersistsNormalizedOptionAnswers()
    {
        var scenario = FeatureScenario.Create();
        scenario.ServiceEntity.BookingFormSchema = RoomsLevelNoteSchema;

        var result = await scenario.BookingService.CreateBookingAsync(
            scenario.ClientId,
            "opt-valid",
            scenario.CreateRequest(BookingType.Immediate, answers: Answers(new { rooms = 3, level = "deep", note = "cửa sổ" })));

        var persisted = Assert.Single(scenario.Bookings);
        using var stored = JsonDocument.Parse(persisted.OptionAnswers);
        Assert.Equal(3, stored.RootElement.GetProperty("rooms").GetInt32());
        Assert.Equal("deep", stored.RootElement.GetProperty("level").GetString());
        // The DTO surfaces the normalized answers for the client summary.
        using var echoed = JsonDocument.Parse(result.OptionAnswers);
        Assert.True(echoed.RootElement.TryGetProperty("rooms", out _));
    }

    [Fact(DisplayName = "[UT-BOOK-002-02] A missing required answer is rejected before any booking is written")]
    public async Task Create_MissingRequiredAnswer_ThrowsInvalid()
    {
        var scenario = FeatureScenario.Create();
        scenario.ServiceEntity.BookingFormSchema = RoomsLevelNoteSchema;

        var error = await Assert.ThrowsAsync<AppException>(() =>
            scenario.BookingService.CreateBookingAsync(
                scenario.ClientId,
                "opt-missing",
                scenario.CreateRequest(BookingType.Immediate, answers: Answers(new { level = "deep" }))));

        Assert.Equal("BOOKING_OPTION_ANSWERS_INVALID", error.Code);
        Assert.Empty(scenario.Bookings);
    }

    [Theory(DisplayName = "[UT-BOOK-002-03] Wrong type, out-of-range, bad choice, and unknown keys are rejected")]
    [InlineData("{\"rooms\":\"three\",\"level\":\"deep\"}")]   // wrong type
    [InlineData("{\"rooms\":99,\"level\":\"deep\"}")]           // out of range
    [InlineData("{\"rooms\":3,\"level\":\"sparkle\"}")]         // not an allowed choice
    [InlineData("{\"rooms\":3,\"level\":\"deep\",\"bogus\":1}")] // unknown key
    public async Task Create_InvalidAnswers_ThrowsInvalid(string answersJson)
    {
        var scenario = FeatureScenario.Create();
        scenario.ServiceEntity.BookingFormSchema = RoomsLevelNoteSchema;
        var answers = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(answersJson)!;

        var error = await Assert.ThrowsAsync<AppException>(() =>
            scenario.BookingService.CreateBookingAsync(
                scenario.ClientId,
                "opt-bad",
                scenario.CreateRequest(BookingType.Immediate, answers: answers)));

        Assert.Equal("BOOKING_OPTION_ANSWERS_INVALID", error.Code);
        Assert.Empty(scenario.Bookings);
    }

    [Fact(DisplayName = "[UT-BOOK-002-04] A service without a schema accepts an empty answer set")]
    public async Task Create_NoSchema_NoAnswers_Succeeds()
    {
        var scenario = FeatureScenario.Create();

        var result = await scenario.BookingService.CreateBookingAsync(
            scenario.ClientId, "opt-none", scenario.CreateRequest(BookingType.Immediate));

        Assert.Equal(nameof(BookingStatus.AwaitingWorker), result.Status);
    }

    // ----------------------- BOOK-004: server-calculated pricing -----------------------

    [Fact(DisplayName = "[UT-BOOK-004-01] Quote returns a server breakdown in VND")]
    public async Task Quote_ReturnsServerBreakdown()
    {
        var scenario = FeatureScenario.Create();

        var breakdown = await scenario.BookingService.GetQuoteAsync(
            scenario.ClientId,
            new BookingQuoteRequestDto { ServiceId = scenario.ServiceEntity.Id, DurationHours = 2, DiscountAmount = 0 });

        Assert.Equal(100_000m, breakdown.UnitPrice);
        Assert.Equal(2m, breakdown.DurationHours);
        Assert.Equal(200_000m, breakdown.LineTotal);
        Assert.Equal(200_000m, breakdown.TotalPrice);
        Assert.Equal("VND", breakdown.Currency);
    }

    [Fact(DisplayName = "[UT-BOOK-004-02] A discount larger than the line total floors the total at zero")]
    public async Task Quote_DiscountExceedsTotal_FloorsAtZero()
    {
        var scenario = FeatureScenario.Create();

        var breakdown = await scenario.BookingService.GetQuoteAsync(
            scenario.ClientId,
            new BookingQuoteRequestDto { ServiceId = scenario.ServiceEntity.Id, DurationHours = 2, DiscountAmount = 500_000 });

        Assert.Equal(0m, breakdown.TotalPrice);
        Assert.True(breakdown.TotalPrice >= 0);
    }

    [Fact(DisplayName = "[UT-BOOK-004-03] A created booking persists a breakdown whose total equals TotalPrice")]
    public async Task Create_PersistsPricingBreakdownMatchingTotal()
    {
        var scenario = FeatureScenario.Create();

        var result = await scenario.BookingService.CreateBookingAsync(
            scenario.ClientId,
            "price-key",
            scenario.CreateRequest(BookingType.Immediate, discount: 50_000));

        var persisted = Assert.Single(scenario.Bookings);
        Assert.NotEqual("{}", persisted.PricingBreakdown);
        Assert.NotNull(result.PricingBreakdown);
        Assert.Equal(150_000m, result.PricingBreakdown!.TotalPrice);
        Assert.Equal(result.PricingBreakdown.TotalPrice, result.TotalPrice);
        Assert.Equal(persisted.TotalPrice, result.PricingBreakdown.TotalPrice);
    }

    [Fact(DisplayName = "[UT-BOOK-004-04] A quote below the minimum duration is rejected")]
    public async Task Quote_BelowMinimumHours_ThrowsDurationInvalid()
    {
        var scenario = FeatureScenario.Create();

        var error = await Assert.ThrowsAsync<AppException>(() =>
            scenario.BookingService.GetQuoteAsync(
                scenario.ClientId,
                new BookingQuoteRequestDto { ServiceId = scenario.ServiceEntity.Id, DurationHours = 1 }));

        Assert.Equal("BOOKING_DURATION_INVALID", error.Code);
    }

    [Fact(DisplayName = "[UT-BOOK-004-05] A quote for an inactive service is unavailable")]
    public async Task Quote_InactiveService_ThrowsUnavailable()
    {
        var scenario = FeatureScenario.Create();
        scenario.ServiceEntity.IsActive = false;

        var error = await Assert.ThrowsAsync<AppException>(() =>
            scenario.BookingService.GetQuoteAsync(
                scenario.ClientId,
                new BookingQuoteRequestDto { ServiceId = scenario.ServiceEntity.Id, DurationHours = 2 }));

        Assert.Equal("BOOKING_SERVICE_UNAVAILABLE", error.Code);
    }

    // ----------------------- BOOK-003: valid-slot exposure and revalidation -----------------------

    [Fact(DisplayName = "[UT-BOOK-003-01] Availability exposes a short validity window (GeneratedAt + 2 minutes)")]
    public async Task Availability_ExposesValidUntilWindow()
    {
        var scenario = FeatureScenario.Create();

        var result = await scenario.BookingService.GetAvailabilityAsync(
            scenario.ClientId, scenario.AvailabilityRequest(BookingType.Immediate));

        Assert.NotEqual(default, result.GeneratedAt);
        Assert.Equal(result.GeneratedAt.AddMinutes(2), result.ValidUntil);
    }

    [Fact(DisplayName = "[UT-BOOK-003-02] A scheduled booking outside the service operating hours is rejected as an illegal time")]
    public async Task Create_ScheduledOutsideOperatingHours_ThrowsOutsideOperatingHours()
    {
        var scenario = FeatureScenario.Create();
        // Service is only open Monday 08:00-17:00; a Monday-night start is outside operating hours.
        // This is a time-legality rejection — NOT a worker-availability check.
        scenario.ServiceEntity.OperatingSchedule = "{\"monday\":{\"open\":\"08:00\",\"close\":\"17:00\"}}";
        var mondayNight = new DateTime(2026, 7, 6, 18, 0, 0, DateTimeKind.Utc);

        var error = await Assert.ThrowsAsync<AppException>(() =>
            scenario.BookingService.CreateBookingAsync(
                scenario.ClientId, "out-of-hours", scenario.CreateRequest(BookingType.Scheduled, start: mondayNight)));

        Assert.Equal("BOOKING_OUTSIDE_OPERATING_HOURS", error.Code);
        Assert.Empty(scenario.Bookings);
    }

    // ----------------------- helpers -----------------------

    private const string RoomsLevelNoteSchema =
        "{\"questions\":[" +
        "{\"key\":\"rooms\",\"type\":\"number\",\"required\":true,\"min\":1,\"max\":10}," +
        "{\"key\":\"level\",\"type\":\"choice\",\"required\":true,\"options\":[\"light\",\"deep\"]}," +
        "{\"key\":\"note\",\"type\":\"text\",\"required\":false,\"maxLength\":50}]}";

    private static Dictionary<string, JsonElement> Answers(object value) =>
        JsonSerializer.SerializeToElement(value)
            .EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value);

    private sealed class FeatureScenario
    {
        public Guid ClientId { get; } = Guid.NewGuid();
        public BookingService BookingService { get; private set; } = null!;
        public Service ServiceEntity { get; private set; } = null!;
        public UserAddress Address { get; private set; } = null!;
        public List<Booking> Bookings { get; } = [];

        public static FeatureScenario Create()
        {
            var scenario = new FeatureScenario();
            var serviceId = Guid.NewGuid();
            var addressId = Guid.NewGuid();
            var workerId = Guid.NewGuid();

            scenario.ServiceEntity = new Service
            {
                Id = serviceId,
                Name = "Dọn nhà",
                IsActive = true,
                MinimumHours = 2,
                BasePrice = 100_000,
                OperatingSchedule = "{}",
                BookingFormSchema = "{}"
            };
            scenario.Address = new UserAddress
            {
                Id = addressId,
                UserId = scenario.ClientId,
                Label = "Nhà",
                AddressText = "Quận 1",
                Latitude = 10.7769m,
                Longitude = 106.7009m
            };
            var worker = new WorkerProfile
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
                .With([new Cleaning.DAL.Entities.WorkerService { WorkerId = workerId, ServiceId = serviceId, IsVerified = true }])
                .With([worker])
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
                unitOfWork, availabilityService, NullLogger<BookingCreationService>.Instance, mapper);
            scenario.BookingService = new BookingService(
                unitOfWork, NullLogger<BookingService>.Instance, availabilityService, creationService, mapper);
            return scenario;
        }

        public BookingAvailabilityRequestDto AvailabilityRequest(BookingType bookingType) => new()
        {
            ServiceId = ServiceEntity.Id,
            AddressId = Address.Id,
            BookingType = bookingType,
            DurationHours = 2,
            From = new DateTime(2026, 7, 6, 9, 0, 0, DateTimeKind.Utc),
            To = new DateTime(2026, 7, 6, 9, 0, 0, DateTimeKind.Utc)
        };

        public CreateBookingDto CreateRequest(
            BookingType bookingType,
            DateTime? start = null,
            decimal discount = 0,
            Dictionary<string, JsonElement>? answers = null) => new()
            {
                ServiceId = ServiceEntity.Id,
                AddressId = Address.Id,
                BookingType = bookingType,
                ScheduledStartTime = start,
                DurationHours = 2,
                DiscountAmount = discount,
                OptionAnswers = answers
            };
    }
}

using Cleaning.BLL.Features.Bookings;
using System.Text.Json;
using Cleaning.BLL.Common;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Microsoft.Extensions.Logging.Abstractions;

// Legacy DiscountAmount is exercised deliberately to prove the server ignores it.
#pragma warning disable CS0618

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
            scenario.CreateRequest(BookingType.Immediate, answers: Answers(new { rooms = 3, level = "deep", note = "cá»­a sá»•" })));

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
            new BookingQuoteRequestDto { ServiceId = scenario.ServiceEntity.Id });

        Assert.Equal(100_000m, breakdown.UnitPrice);
        Assert.Equal(2m, breakdown.DurationHours);
        Assert.Equal(200_000m, breakdown.LineTotal);
        Assert.Equal(200_000m, breakdown.TotalPrice);
        Assert.Equal("VND", breakdown.Currency);
    }

    [Fact(DisplayName = "[UT-BOOK-004-02] Client-supplied legacy discount cannot change the quote")]
    public async Task Quote_ClientDiscount_IsIgnored()
    {
        var scenario = FeatureScenario.Create();

        var breakdown = await scenario.BookingService.GetQuoteAsync(
            scenario.ClientId,
            new BookingQuoteRequestDto { ServiceId = scenario.ServiceEntity.Id, DurationHours = 2, DiscountAmount = 500_000 });

        Assert.Equal(200_000m, breakdown.TotalPrice);
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
        Assert.Equal(200_000m, result.PricingBreakdown!.TotalPrice);
        Assert.Equal(result.PricingBreakdown.TotalPrice, result.TotalPrice);
        Assert.Equal(persisted.TotalPrice, result.PricingBreakdown.TotalPrice);
    }

    [Fact(DisplayName = "[UT-BOOK-004-04] Quote duration is derived from the service minimum")]
    public async Task Quote_DerivesMinimumDuration()
    {
        var scenario = FeatureScenario.Create();

        var quote = await scenario.BookingService.GetQuoteAsync(
            scenario.ClientId,
            new BookingQuoteRequestDto { ServiceId = scenario.ServiceEntity.Id, DurationHours = 1 });
        Assert.Equal(2m, quote.DurationHours);
    }

    [Fact(DisplayName = "[UT-BOOK-004-06] Requesting more duration than the computed minimum adds an hourly surcharge")]
    public async Task Quote_DurationAboveMinimum_AddsExtraHourSurcharge()
    {
        var scenario = FeatureScenario.Create();

        var quote = await scenario.BookingService.GetQuoteAsync(
            scenario.ClientId,
            new BookingQuoteRequestDto { ServiceId = scenario.ServiceEntity.Id, DurationHours = 3 });

        Assert.Equal(3m, quote.DurationHours);
        Assert.Equal(300_000m, quote.TotalPrice);
        Assert.Contains(quote.Breakdown, line => line.Amount == 100_000m);
    }

    [Fact(DisplayName = "[UT-BOOK-004-07] A created booking is charged for the client-requested extra duration")]
    public async Task Create_DurationAboveMinimum_ChargesExtraHour()
    {
        var scenario = FeatureScenario.Create();

        var result = await scenario.BookingService.CreateBookingAsync(
            scenario.ClientId,
            "extra-duration",
            scenario.CreateRequest(BookingType.Immediate, durationHours: 3));

        var persisted = Assert.Single(scenario.Bookings);
        Assert.Equal(3m, persisted.DurationHours);
        Assert.Equal(300_000m, persisted.TotalPrice);
        Assert.Equal(300_000m, result.TotalPrice);
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

    [Fact(DisplayName = "[UT-BOOK-003-02] OperatingSchedule is dormant: a scheduled booking at any hour is accepted")]
    public async Task Create_ScheduledOutsideOperatingHours_IsAccepted()
    {
        var scenario = FeatureScenario.Create();
        // Spec D.6: "Any hour of day is allowed" -- OperatingSchedule stays on the entity but must not
        // block creation, even when the start time falls outside the configured hours.
        scenario.ServiceEntity.OperatingSchedule = "{\"monday\":{\"open\":\"08:00\",\"close\":\"17:00\"}}";
        var nightStart = DateTime.UtcNow.Date.AddDays(3).AddHours(18);

        var result = await scenario.BookingService.CreateBookingAsync(
            scenario.ClientId, "any-hour", scenario.CreateRequest(BookingType.Scheduled, start: nightStart));

        Assert.Equal(nameof(BookingStatus.AwaitingWorker), result.Status);
        Assert.Single(scenario.Bookings);
    }

    // ----------------------- EPIC D: answer-delta pricing, promotions, staleness -----------------------

    [Fact(DisplayName = "[UT-BOOK-004-06] Answer deltas price the quote and derive the duration (D.3/D.4)")]
    public async Task Quote_AnswerDeltas_PriceAndDeriveDuration()
    {
        var scenario = FeatureScenario.Create();
        scenario.ServiceEntity.BookingFormSchema = EpicDSchema;

        var quote = await scenario.BookingService.GetQuoteAsync(
            scenario.ClientId,
            new BookingQuoteRequestDto
            {
                ServiceId = scenario.ServiceEntity.Id,
                OptionAnswers = Answers(new { rooms = 3, addons = new[] { "fridge" }, pets = true })
            });

        // Minutes: 2h base (120) + 3 rooms x 45 (135) + fridge (20) = 275, rounded up to 300 = 5.0h.
        Assert.Equal(5.0m, quote.DurationHours);
        // VND: 100k x 2h base (200k) + 3 rooms x 40k (120k) + fridge (30k) = 350k.
        Assert.Equal(350_000m, quote.TotalPrice);
        Assert.Collection(quote.Breakdown,
            line => { Assert.Contains("base", line.Label); Assert.Equal(200_000m, line.Amount); },
            line => Assert.Equal(120_000m, line.Amount),
            line => { Assert.Equal("Inside fridge", line.Label); Assert.Equal(30_000m, line.Amount); });
    }

    [Fact(DisplayName = "[UT-BOOK-004-07] Quote echoes the current service version for staleness detection")]
    public async Task Quote_EchoesServiceVersion()
    {
        var scenario = FeatureScenario.Create();
        scenario.ServiceEntity.Version = 7;

        var quote = await scenario.BookingService.GetQuoteAsync(
            scenario.ClientId, new BookingQuoteRequestDto { ServiceId = scenario.ServiceEntity.Id });

        Assert.Equal(7, quote.ServiceVersion);
    }

    [Fact(DisplayName = "[UT-BOOK-004-08] An active percentage promotion is applied as a negative breakdown line")]
    public async Task Quote_ActivePercentagePromotion_AddsNegativeLine()
    {
        var scenario = FeatureScenario.Create();
        var promotion = scenario.AddPromotion(discountType: "percentage", value: 10);

        var quote = await scenario.BookingService.GetQuoteAsync(
            scenario.ClientId, new BookingQuoteRequestDto { ServiceId = scenario.ServiceEntity.Id });

        Assert.Equal(20_000m, quote.DiscountAmount);
        Assert.Equal(180_000m, quote.TotalPrice);
        var discountLine = Assert.Single(quote.Breakdown, line => line.Amount < 0);
        Assert.Equal(promotion.Name, discountLine.Label);
        Assert.Equal(-20_000m, discountLine.Amount);
    }

    [Fact(DisplayName = "[UT-BOOK-004-09] A fixed promotion larger than the subtotal clamps the total at zero")]
    public async Task Quote_OversizedFixedPromotion_ClampsAtZero()
    {
        var scenario = FeatureScenario.Create();
        scenario.AddPromotion(discountType: "fixed", value: 500_000);

        var quote = await scenario.BookingService.GetQuoteAsync(
            scenario.ClientId, new BookingQuoteRequestDto { ServiceId = scenario.ServiceEntity.Id });

        Assert.Equal(200_000m, quote.DiscountAmount);
        Assert.Equal(0m, quote.TotalPrice);
    }

    [Theory(DisplayName = "[UT-BOOK-004-10] Draft, upcoming, expired, and archived promotions are ignored")]
    [InlineData("draft", -1, 1, false)]
    [InlineData("active", 1, 2, false)]  // not started yet
    [InlineData("active", -2, -1, false)] // already ended
    [InlineData("active", -1, 1, true)]  // archived
    public async Task Quote_InactivePromotion_IsIgnored(string status, int startsDays, int endsDays, bool archived)
    {
        var scenario = FeatureScenario.Create();
        scenario.AddPromotion(
            discountType: "percentage",
            value: 10,
            status: status,
            startsAt: DateTime.UtcNow.AddDays(startsDays),
            endsAt: DateTime.UtcNow.AddDays(endsDays),
            archivedAt: archived ? DateTime.UtcNow : null);

        var quote = await scenario.BookingService.GetQuoteAsync(
            scenario.ClientId, new BookingQuoteRequestDto { ServiceId = scenario.ServiceEntity.Id });

        Assert.Equal(0m, quote.DiscountAmount);
        Assert.Equal(200_000m, quote.TotalPrice);
    }

    [Fact(DisplayName = "[UT-BOOK-004-11] Creating with a stale service version fails with QUOTE_STALE and writes nothing")]
    public async Task Create_StaleServiceVersion_ThrowsQuoteStale()
    {
        var scenario = FeatureScenario.Create();
        scenario.ServiceEntity.Version = 2; // admin bumped the service after the client quoted

        var error = await Assert.ThrowsAsync<AppException>(() =>
            scenario.BookingService.CreateBookingAsync(
                scenario.ClientId, "stale-key", scenario.CreateRequest(BookingType.Immediate)));

        Assert.Equal("QUOTE_STALE", error.Code);
        Assert.Empty(scenario.Bookings);
    }

    [Fact(DisplayName = "[UT-BOOK-004-12] Create persists the derived duration and matching end time")]
    public async Task Create_PersistsDerivedDurationAndEndTime()
    {
        var scenario = FeatureScenario.Create();
        scenario.ServiceEntity.BookingFormSchema = EpicDSchema;
        var start = DateTime.UtcNow.Date.AddDays(3).AddHours(9);

        var result = await scenario.BookingService.CreateBookingAsync(
            scenario.ClientId,
            "derived-duration",
            scenario.CreateRequest(
                BookingType.Scheduled,
                start: start,
                answers: Answers(new { rooms = 3, addons = new[] { "fridge" } })));

        var persisted = Assert.Single(scenario.Bookings);
        Assert.Equal(5.0m, persisted.DurationHours);
        Assert.Equal(start.AddHours(5), persisted.ScheduledEndTime);
        Assert.Equal(350_000m, persisted.TotalPrice);
        Assert.Equal(350_000m, result.TotalPrice);
    }

    // ----------------------- EPIC D: new question types (BOOK-002) -----------------------

    [Theory(DisplayName = "[UT-BOOK-002-05] yes_no and multi_choice reject malformed answers")]
    [InlineData("{\"rooms\":2,\"pets\":\"yes\"}")]         // yes_no must be a boolean
    [InlineData("{\"rooms\":2,\"addons\":\"fridge\"}")]     // multi_choice must be an array
    [InlineData("{\"rooms\":2,\"addons\":[\"jacuzzi\"]}")] // option id not in the schema
    public async Task Create_MalformedNewTypeAnswers_ThrowsInvalid(string answersJson)
    {
        var scenario = FeatureScenario.Create();
        scenario.ServiceEntity.BookingFormSchema = EpicDSchema;
        var answers = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(answersJson)!;

        var error = await Assert.ThrowsAsync<AppException>(() =>
            scenario.BookingService.CreateBookingAsync(
                scenario.ClientId, "new-type-bad", scenario.CreateRequest(BookingType.Immediate, answers: answers)));

        Assert.Equal("BOOKING_OPTION_ANSWERS_INVALID", error.Code);
        Assert.Empty(scenario.Bookings);
    }

    [Fact(DisplayName = "[UT-BOOK-002-06] yes_no, multi_choice, and text answers round-trip into OptionAnswers")]
    public async Task Create_NewTypeAnswers_RoundTrip()
    {
        var scenario = FeatureScenario.Create();
        scenario.ServiceEntity.BookingFormSchema = EpicDSchema;

        await scenario.BookingService.CreateBookingAsync(
            scenario.ClientId,
            "new-type-ok",
            scenario.CreateRequest(
                BookingType.Immediate,
                answers: Answers(new { rooms = 2, pets = true, addons = new[] { "fridge", "windows" }, note = "nhieu cua kinh" })));

        var persisted = Assert.Single(scenario.Bookings);
        using var stored = JsonDocument.Parse(persisted.OptionAnswers);
        Assert.True(stored.RootElement.GetProperty("pets").GetBoolean());
        Assert.Equal(2, stored.RootElement.GetProperty("addons").GetArrayLength());
        Assert.Equal("nhieu cua kinh", stored.RootElement.GetProperty("note").GetString());
    }

    [Fact(DisplayName = "[UT-BOOK-002-07] A required photos question never blocks creation (photos upload after create)")]
    public async Task Create_MissingRequiredPhotos_StillSucceeds()
    {
        var scenario = FeatureScenario.Create();
        scenario.ServiceEntity.BookingFormSchema = RequiredPhotosSchema;

        var result = await scenario.BookingService.CreateBookingAsync(
            scenario.ClientId,
            "photos-later",
            scenario.CreateRequest(BookingType.Immediate, answers: Answers(new { rooms = 2 })));

        Assert.Equal(nameof(BookingStatus.AwaitingWorker), result.Status);
        Assert.Single(scenario.Bookings);
    }

    [Fact(DisplayName = "[UT-BOOK-002-08] An unknown question type is skipped, and its answer is dropped")]
    public async Task Create_UnknownQuestionType_IsSkipped()
    {
        var scenario = FeatureScenario.Create();
        scenario.ServiceEntity.BookingFormSchema = UnknownTypeSchema;

        await scenario.BookingService.CreateBookingAsync(
            scenario.ClientId,
            "forward-compat",
            scenario.CreateRequest(BookingType.Immediate, answers: Answers(new { rooms = 2, future = "x" })));

        // Spec D.3 forward compatibility: the unknown question neither blocks (despite required:true)
        // nor persists its answer.
        var persisted = Assert.Single(scenario.Bookings);
        using var stored = JsonDocument.Parse(persisted.OptionAnswers);
        Assert.Equal(2, stored.RootElement.GetProperty("rooms").GetInt32());
        Assert.False(stored.RootElement.TryGetProperty("future", out _));
    }

    // ----------------------- one in-flight Immediate booking at a time -----------------------

    [Fact(DisplayName = "[UT-BOOK-004-13] Creating a second Immediate booking while one is still " +
        "AwaitingWorker is rejected")]
    public async Task Create_SecondImmediate_WhileFirstStillAwaitingWorker_Throws()
    {
        var scenario = FeatureScenario.Create();
        await scenario.BookingService.CreateBookingAsync(
            scenario.ClientId, "first-immediate", scenario.CreateRequest(BookingType.Immediate));

        var error = await Assert.ThrowsAsync<AppException>(() =>
            scenario.BookingService.CreateBookingAsync(
                scenario.ClientId, "second-immediate", scenario.CreateRequest(BookingType.Immediate)));

        Assert.Equal("BOOKING_IMMEDIATE_ALREADY_ACTIVE", error.Code);
        Assert.Single(scenario.Bookings);
    }

    [Fact(DisplayName = "[UT-BOOK-004-14] A Scheduled booking is unaffected by an in-flight Immediate " +
        "booking, and a new Immediate booking is allowed once the first is no longer AwaitingWorker")]
    public async Task Create_ScheduledUnaffected_AndImmediateAllowedOnceFirstResolved()
    {
        var scenario = FeatureScenario.Create();
        await scenario.BookingService.CreateBookingAsync(
            scenario.ClientId, "first-immediate", scenario.CreateRequest(BookingType.Immediate));

        // Scheduled bookings are never blocked by an in-flight Immediate search.
        await scenario.BookingService.CreateBookingAsync(
            scenario.ClientId, "scheduled-ok",
            scenario.CreateRequest(BookingType.Scheduled, start: DateTime.UtcNow.Date.AddDays(3).AddHours(9)));
        Assert.Equal(2, scenario.Bookings.Count);

        // Once the first Immediate booking leaves AwaitingWorker, a new one is allowed again.
        scenario.Bookings.Single(b => b.BookingType == BookingType.Immediate).Status = BookingStatus.Cancelled;
        await scenario.BookingService.CreateBookingAsync(
            scenario.ClientId, "second-immediate-after-cancel", scenario.CreateRequest(BookingType.Immediate));
        Assert.Equal(3, scenario.Bookings.Count);
    }

    // ----------------------- helpers -----------------------

    private const string RoomsLevelNoteSchema =
        "{\"questions\":[" +
        "{\"key\":\"rooms\",\"type\":\"number\",\"required\":true,\"min\":1,\"max\":10}," +
        "{\"key\":\"level\",\"type\":\"choice\",\"required\":true,\"options\":[\"light\",\"deep\"]}," +
        "{\"key\":\"note\",\"type\":\"text\",\"required\":false,\"maxLength\":50}]}";

    // The D.3 example schema: stepper with unit deltas, multi_choice with option deltas, yes_no, text, photos.
    private const string EpicDSchema =
        "{\"questions\":[" +
        "{\"id\":\"rooms\",\"type\":\"stepper\",\"label\":\"How many rooms?\",\"min\":1,\"max\":10,\"required\":true," +
        "\"unit\":{\"priceDelta\":40000,\"durationDelta\":45}}," +
        "{\"id\":\"addons\",\"type\":\"multi_choice\",\"label\":\"Extra tasks\",\"options\":[" +
        "{\"id\":\"fridge\",\"label\":\"Inside fridge\",\"priceDelta\":30000,\"durationDelta\":20}," +
        "{\"id\":\"windows\",\"label\":\"Windows\",\"priceDelta\":50000,\"durationDelta\":30}]}," +
        "{\"id\":\"pets\",\"type\":\"yes_no\",\"label\":\"Do you have pets?\"}," +
        "{\"id\":\"note\",\"type\":\"text\",\"label\":\"Note\",\"maxLength\":500}," +
        "{\"id\":\"photos\",\"type\":\"photos\",\"label\":\"Photos\",\"max\":5}]}";

    private const string RequiredPhotosSchema =
        "{\"questions\":[" +
        "{\"id\":\"rooms\",\"type\":\"stepper\",\"required\":true,\"min\":1,\"max\":10}," +
        "{\"id\":\"photos\",\"type\":\"photos\",\"required\":true,\"max\":5}]}";

    private const string UnknownTypeSchema =
        "{\"questions\":[" +
        "{\"id\":\"rooms\",\"type\":\"stepper\",\"required\":true,\"min\":1,\"max\":10}," +
        "{\"id\":\"future\",\"type\":\"hologram\",\"required\":true}]}";

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
        public List<Promotion> Promotions { get; } = [];

        public Promotion AddPromotion(
            string discountType = "percentage",
            decimal value = 10,
            string status = "active",
            DateTime? startsAt = null,
            DateTime? endsAt = null,
            DateTime? archivedAt = null)
        {
            var promotion = new Promotion
            {
                Id = Guid.NewGuid(),
                ServiceId = ServiceEntity.Id,
                Name = "Khuyen mai thang 7",
                DiscountType = discountType,
                DiscountValue = value,
                Status = status,
                StartsAt = startsAt ?? DateTime.UtcNow.AddDays(-1),
                EndsAt = endsAt ?? DateTime.UtcNow.AddDays(1),
                ArchivedAt = archivedAt
            };
            Promotions.Add(promotion);
            return promotion;
        }

        public static FeatureScenario Create()
        {
            var scenario = new FeatureScenario();
            var serviceId = Guid.NewGuid();
            var addressId = Guid.NewGuid();
            var workerId = Guid.NewGuid();

            scenario.ServiceEntity = new Service
            {
                Id = serviceId,
                Name = "Dá»n nhÃ ",
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
                Label = "NhÃ ",
                AddressText = "Quáº­n 1",
                Latitude = 10.7769m,
                Longitude = 106.7009m
            };
            var worker = new WorkerProfile
            {
                UserId = workerId,
                VerificationStatus = "approved",
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
                .With(scenario.Promotions)
                .With(new List<BookingStatusLog>());

            var availabilityService = new BookingAvailabilityService(unitOfWork);
            var mapper = TestMapperFactory.Create();
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
            From = DateTime.UtcNow.Date.AddDays(7).AddHours(9),
            To = DateTime.UtcNow.Date.AddDays(7).AddHours(9)
        };

        public CreateBookingDto CreateRequest(
            BookingType bookingType,
            DateTime? start = null,
            decimal discount = 0,
            decimal durationHours = 0,
            Dictionary<string, JsonElement>? answers = null) => new()
            {
                ServiceId = ServiceEntity.Id,
                AddressId = Address.Id,
                BookingType = bookingType,
                ScheduledStartTime = start,
                DurationHours = durationHours,
                DiscountAmount = discount,
                OptionAnswers = answers
            };
    }
}

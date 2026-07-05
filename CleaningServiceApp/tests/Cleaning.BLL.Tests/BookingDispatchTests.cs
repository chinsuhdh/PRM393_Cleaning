using AutoMapper;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Mapping;
using Cleaning.BLL.Services;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cleaning.BLL.Tests;

public sealed class BookingDispatchTests
{
    [Fact(DisplayName = "[UT-BOOK-DSP-01] Dispatch surfaces unassigned AwaitingWorker jobs for a verified service")]
    public async Task GetAvailable_UnassignedAwaitingWorker_ForVerifiedService_IsSurfaced()
    {
        var scenario = DispatchScenario.Create();
        scenario.AddBooking(BookingStatus.AwaitingWorker);

        var available = await scenario.BookingService.GetAvailableBookingsAsync(scenario.WorkerId);

        var dto = Assert.Single(available);
        Assert.Equal(scenario.ServiceEntity.Id, dto.ServiceId);
        Assert.Null(dto.WorkerId);
    }

    [Fact(DisplayName = "[UT-BOOK-DSP-02] Dispatch does not surface post-job PendingPayment bookings")]
    public async Task GetAvailable_PendingPayment_IsHidden()
    {
        var scenario = DispatchScenario.Create();
        scenario.AddBooking(BookingStatus.PendingPayment);

        var available = await scenario.BookingService.GetAvailableBookingsAsync(scenario.WorkerId);

        Assert.Empty(available);
    }

    [Fact(DisplayName = "[UT-BOOK-DSP-03] Dispatch hides jobs the worker is not verified to perform")]
    public async Task GetAvailable_ServiceNotVerified_IsHidden()
    {
        var scenario = DispatchScenario.Create();
        scenario.AddBooking(BookingStatus.AwaitingWorker, serviceId: Guid.NewGuid());

        var available = await scenario.BookingService.GetAvailableBookingsAsync(scenario.WorkerId);

        Assert.Empty(available);
    }

    [Fact(DisplayName = "[UT-BOOK-DSP-04] Dispatch hides jobs already claimed by a worker")]
    public async Task GetAvailable_AlreadyAssigned_IsHidden()
    {
        var scenario = DispatchScenario.Create();
        scenario.AddBooking(BookingStatus.Accepted, workerId: Guid.NewGuid());

        var available = await scenario.BookingService.GetAvailableBookingsAsync(scenario.WorkerId);

        Assert.Empty(available);
    }

    [Fact(DisplayName = "[UT-BOOK-DSP-05] Dispatch hides jobs whose skill is not yet verified for this worker")]
    public async Task GetAvailable_SkillPendingVerification_IsHidden()
    {
        var scenario = DispatchScenario.Create(workerSkillVerified: false);
        scenario.AddBooking(BookingStatus.AwaitingWorker);

        var available = await scenario.BookingService.GetAvailableBookingsAsync(scenario.WorkerId);

        Assert.Empty(available);
    }

    [Fact(DisplayName = "[UT-BOOK-ACC-01] Accepting an unassigned job assigns the worker and moves it to Accepted")]
    public async Task Accept_UnassignedAwaitingWorker_AssignsWorker()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.AwaitingWorker);

        var accepted = await scenario.BookingService.AcceptBookingAsync(booking.Id, scenario.WorkerId);

        Assert.True(accepted);
        Assert.Equal(scenario.WorkerId, booking.WorkerId);
        Assert.Equal(BookingStatus.Accepted, booking.Status);
        Assert.Single(scenario.StatusLogs, log => log.BookingId == booking.Id && log.NewStatus == BookingStatus.Accepted);
    }

    [Fact(DisplayName = "[UT-BOOK-ACC-02] A job already claimed by another worker cannot be accepted")]
    public async Task Accept_AlreadyAssigned_ReturnsFalse()
    {
        var scenario = DispatchScenario.Create();
        var otherWorker = Guid.NewGuid();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: otherWorker);

        var accepted = await scenario.BookingService.AcceptBookingAsync(booking.Id, scenario.WorkerId);

        Assert.False(accepted);
        Assert.Equal(otherWorker, booking.WorkerId);
    }

    [Fact(DisplayName = "[UT-BOOK-ACC-03] A job in a non-awaiting status cannot be accepted")]
    public async Task Accept_NonAwaitingStatus_ReturnsFalse()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Completed);

        var accepted = await scenario.BookingService.AcceptBookingAsync(booking.Id, scenario.WorkerId);

        Assert.False(accepted);
        Assert.Null(booking.WorkerId);
    }

    [Fact(DisplayName = "[UT-BOOK-ACC-04] Accepting a non-existent job returns false")]
    public async Task Accept_MissingBooking_ReturnsFalse()
    {
        var scenario = DispatchScenario.Create();

        var accepted = await scenario.BookingService.AcceptBookingAsync(Guid.NewGuid(), scenario.WorkerId);

        Assert.False(accepted);
    }

    [Fact(DisplayName = "[UT-BOOK-ACC-05] Two workers racing for the same job: only the first succeeds")]
    public async Task Accept_SecondWorkerAfterFirst_ReturnsFalse()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.AwaitingWorker);
        var firstWorker = scenario.WorkerId;
        var secondWorker = Guid.NewGuid();

        var first = await scenario.BookingService.AcceptBookingAsync(booking.Id, firstWorker);
        var second = await scenario.BookingService.AcceptBookingAsync(booking.Id, secondWorker);

        Assert.True(first);
        Assert.False(second);
        Assert.Equal(firstWorker, booking.WorkerId);
    }

    [Fact(DisplayName = "[UT-BOOK-STS-01] A user who is neither the client nor the assigned worker cannot change a booking status")]
    public async Task UpdateStatus_NonParticipant_ReturnsFalseAndLeavesStatusUnchanged()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);

        var updated = await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, Guid.NewGuid(), new UpdateBookingStatusDto { NewStatus = BookingStatus.Cancelled });

        Assert.False(updated);
        Assert.Equal(BookingStatus.Accepted, booking.Status);
    }

    [Fact(DisplayName = "[UT-BOOK-STS-02] The owning client can change the status of their own booking")]
    public async Task UpdateStatus_OwningClient_Succeeds()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.AwaitingWorker);

        var updated = await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, scenario.ClientId, new UpdateBookingStatusDto { NewStatus = BookingStatus.Cancelled });

        Assert.True(updated);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }

    [Fact(DisplayName = "[UT-BOOK-STS-03] The assigned worker can change the status of a booking they hold")]
    public async Task UpdateStatus_AssignedWorker_Succeeds()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);

        var updated = await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, scenario.WorkerId, new UpdateBookingStatusDto { NewStatus = BookingStatus.InProgress });

        Assert.True(updated);
        Assert.Equal(BookingStatus.InProgress, booking.Status);
    }

    [Fact(DisplayName = "[UT-BOOK-IDEM-01] Re-sending the same idempotency key returns the original booking, not a duplicate")]
    public async Task Create_DuplicateIdempotencyKey_ReturnsExistingBooking()
    {
        var scenario = DispatchScenario.Create();

        var first = await scenario.BookingService.CreateBookingAsync(
            scenario.ClientId, "dup-key", scenario.CreateRequest());
        var second = await scenario.BookingService.CreateBookingAsync(
            scenario.ClientId, "dup-key", scenario.CreateRequest());

        Assert.Equal(first.Id, second.Id);
        Assert.Single(scenario.Bookings);
    }

    private sealed class DispatchScenario
    {
        public Guid ClientId { get; } = Guid.NewGuid();
        public Guid WorkerId { get; } = Guid.NewGuid();
        public BookingService BookingService { get; private set; } = null!;
        public Service ServiceEntity { get; private set; } = null!;
        public UserAddress Address { get; private set; } = null!;
        public List<Booking> Bookings { get; } = [];
        public List<BookingStatusLog> StatusLogs { get; } = [];

        public static DispatchScenario Create(bool workerSkillVerified = true)
        {
            var scenario = new DispatchScenario();
            var serviceId = Guid.NewGuid();

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
                Id = Guid.NewGuid(),
                UserId = scenario.ClientId,
                Label = "NhÃ ",
                AddressText = "Quáº­n 1",
                Latitude = 10.7769m,
                Longitude = 106.7009m
            };
            var worker = new WorkerProfile
            {
                UserId = scenario.WorkerId,
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
                .With([new Cleaning.DAL.Entities.WorkerService
                {
                    WorkerId = scenario.WorkerId,
                    ServiceId = serviceId,
                    IsVerified = workerSkillVerified
                }])
                .With([worker])
                .With([new WorkerAvailability
                {
                    Id = Guid.NewGuid(),
                    WorkerId = scenario.WorkerId,
                    StartTime = DateTime.UtcNow.AddYears(-1),
                    EndTime = DateTime.UtcNow.AddYears(1),
                    Status = AvailabilityStatus.Available
                }])
                .With(scenario.Bookings)
                .With(scenario.StatusLogs)
                .With(new List<BookingCancellation>());

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

        public Booking AddBooking(BookingStatus status, Guid? workerId = null, Guid? serviceId = null)
        {
            var start = DateTime.UtcNow.AddHours(3);
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                ClientId = ClientId,
                WorkerId = workerId,
                ServiceId = serviceId ?? ServiceEntity.Id,
                AddressId = Address.Id,
                BookingType = BookingType.Scheduled,
                ScheduledStartTime = start,
                ScheduledEndTime = start.AddHours(2),
                DurationHours = 2,
                Status = status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            Bookings.Add(booking);
            return booking;
        }

        public CreateBookingDto CreateRequest() => new()
        {
            ServiceId = ServiceEntity.Id,
            AddressId = Address.Id,
            BookingType = BookingType.Immediate,
            DurationHours = 2
        };
    }
}

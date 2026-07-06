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

    [Fact(DisplayName = "[UT-BOOK-STS-03] The assigned worker follows Accepted to OnTheWay")]
    public async Task UpdateStatus_AssignedWorker_FollowsStateMachine()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);

        var updated = await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, scenario.WorkerId, new UpdateBookingStatusDto { NewStatus = BookingStatus.OnTheWay });

        Assert.True(updated);
        Assert.Equal(BookingStatus.OnTheWay, booking.Status);
    }

    [Theory(DisplayName = "[UT-BOOK-STS-04] § 4.1: every allowed transition succeeds for the right actor and writes a log")]
    [InlineData(BookingStatus.AwaitingWorker, BookingStatus.Cancelled, "client")]
    [InlineData(BookingStatus.Accepted, BookingStatus.OnTheWay, "worker")]
    [InlineData(BookingStatus.OnTheWay, BookingStatus.InProgress, "worker")]
    [InlineData(BookingStatus.InProgress, BookingStatus.PendingPayment, "worker")]
    [InlineData(BookingStatus.PendingPayment, BookingStatus.Completed, "worker")]
    [InlineData(BookingStatus.Accepted, BookingStatus.RescheduleRequested, "client")]
    [InlineData(BookingStatus.Accepted, BookingStatus.RescheduleRequested, "worker")]
    [InlineData(BookingStatus.RescheduleRequested, BookingStatus.Accepted, "client")]
    [InlineData(BookingStatus.Accepted, BookingStatus.Cancelled, "client")]
    [InlineData(BookingStatus.RescheduleRequested, BookingStatus.Cancelled, "worker")]
    [InlineData(BookingStatus.OnTheWay, BookingStatus.Cancelled, "client")]
    [InlineData(BookingStatus.InProgress, BookingStatus.Cancelled, "worker")]
    [InlineData(BookingStatus.PendingPayment, BookingStatus.Cancelled, "client")]
    public async Task UpdateStatus_AllowedArrow_Succeeds(BookingStatus from, BookingStatus to, string actor)
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(
            from, workerId: from == BookingStatus.AwaitingWorker ? null : scenario.WorkerId);
        var actorId = actor == "client" ? scenario.ClientId : scenario.WorkerId;

        var updated = await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, actorId, new UpdateBookingStatusDto { NewStatus = to });

        Assert.True(updated);
        Assert.Equal(to, booking.Status);
        Assert.Single(scenario.StatusLogs,
            log => log.BookingId == booking.Id && log.OldStatus == from && log.NewStatus == to && log.ChangedBy == actorId);
    }

    [Theory(DisplayName = "[UT-BOOK-STS-05] § 4.1: forbidden transitions are rejected without touching the booking")]
    [InlineData(BookingStatus.Accepted, BookingStatus.InProgress, "worker")]     // must pass through OnTheWay
    [InlineData(BookingStatus.Accepted, BookingStatus.OnTheWay, "client")]       // wrong actor
    [InlineData(BookingStatus.OnTheWay, BookingStatus.InProgress, "client")]     // wrong actor
    [InlineData(BookingStatus.InProgress, BookingStatus.Completed, "worker")]    // must pass through PendingPayment
    [InlineData(BookingStatus.AwaitingWorker, BookingStatus.Accepted, "client")] // accept is worker-only via /accept
    [InlineData(BookingStatus.Completed, BookingStatus.Cancelled, "client")]     // terminal
    [InlineData(BookingStatus.Cancelled, BookingStatus.Accepted, "worker")]      // terminal
    [InlineData(BookingStatus.Accepted, BookingStatus.AwaitingWorker, "client")] // release is worker-only
    public async Task UpdateStatus_ForbiddenArrow_ReturnsFalse(BookingStatus from, BookingStatus to, string actor)
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(
            from, workerId: from == BookingStatus.AwaitingWorker ? null : scenario.WorkerId);
        var actorId = actor == "client" ? scenario.ClientId : scenario.WorkerId;

        var updated = await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, actorId, new UpdateBookingStatusDto { NewStatus = to });

        Assert.False(updated);
        Assert.Equal(from, booking.Status);
        Assert.DoesNotContain(scenario.StatusLogs, log => log.BookingId == booking.Id);
    }

    [Fact(DisplayName = "[UT-BOOK-STS-06] Worker plain-cancel releases the job: WorkerId cleared and re-broadcast (§ 4.10)")]
    public async Task UpdateStatus_WorkerReleasesJob_ClearsWorkerAndRebroadcasts()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);

        var updated = await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, scenario.WorkerId, new UpdateBookingStatusDto { NewStatus = BookingStatus.AwaitingWorker });

        Assert.True(updated);
        Assert.Equal(BookingStatus.AwaitingWorker, booking.Status);
        Assert.Null(booking.WorkerId);
        // The released job must show up again in the broadcast feed for eligible workers.
        var available = await scenario.BookingService.GetAvailableBookingsAsync(scenario.WorkerId);
        Assert.Single(available, dto => dto.Id == booking.Id);
    }

    [Fact(DisplayName = "[UT-BOOK-STS-07] Start and finish stamp ActualStartTime and ActualEndTime")]
    public async Task UpdateStatus_StartAndFinish_StampActualTimes()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.OnTheWay, workerId: scenario.WorkerId);

        await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, scenario.WorkerId, new UpdateBookingStatusDto { NewStatus = BookingStatus.InProgress });
        Assert.NotNull(booking.ActualStartTime);
        Assert.Null(booking.ActualEndTime);

        await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, scenario.WorkerId, new UpdateBookingStatusDto { NewStatus = BookingStatus.PendingPayment });
        Assert.NotNull(booking.ActualEndTime);
    }

    [Fact(DisplayName = "[UT-BOOK-STS-08] A report-cancel from InProgress records who cancelled and why")]
    public async Task UpdateStatus_ReportCancel_WritesCancellationRecord()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.InProgress, workerId: scenario.WorkerId);

        var updated = await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, scenario.WorkerId,
            new UpdateBookingStatusDto { NewStatus = BookingStatus.Cancelled, Reason = "Khach vang mat" });

        Assert.True(updated);
        var record = Assert.Single(scenario.Cancellations);
        Assert.Equal(booking.Id, record.BookingId);
        Assert.Equal(scenario.WorkerId, record.CancelledBy);
        Assert.Equal(UserRole.Worker, record.ActorRole);
        Assert.Equal("Khach vang mat", record.Reason);
    }

    [Fact(DisplayName = "[UT-BOOK-PHT-01] The owning client can attach up to five Before photos")]
    public async Task AddPhotos_Owner_PersistsBeforePhotos()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.AwaitingWorker);

        var result = await scenario.BookingService.AddPhotosAsync(
            booking.Id, scenario.ClientId, ["https://cdn/x1.jpg", "https://cdn/x2.jpg"]);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
        Assert.Equal(2, scenario.Photos.Count);
        Assert.All(scenario.Photos, photo =>
        {
            Assert.Equal(PhotoType.Before, photo.PhotoType);
            Assert.Equal(scenario.ClientId, photo.UploadedBy);
        });
    }

    [Fact(DisplayName = "[UT-BOOK-PHT-02] More than five photos in total are rejected")]
    public async Task AddPhotos_OverCap_ReturnsNull()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.AwaitingWorker);
        for (var i = 0; i < 4; i++)
        {
            scenario.Photos.Add(new BookingPhoto
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                UploadedBy = scenario.ClientId,
                PhotoUrl = $"https://cdn/old{i}.jpg",
                PhotoType = PhotoType.Before
            });
        }

        var result = await scenario.BookingService.AddPhotosAsync(
            booking.Id, scenario.ClientId, ["https://cdn/x1.jpg", "https://cdn/x2.jpg"]);

        Assert.Null(result);
        Assert.Equal(4, scenario.Photos.Count);
    }

    [Fact(DisplayName = "[UT-BOOK-PHT-03] Someone other than the owning client cannot attach photos")]
    public async Task AddPhotos_NonOwner_ReturnsNull()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);

        var result = await scenario.BookingService.AddPhotosAsync(
            booking.Id, scenario.WorkerId, ["https://cdn/x1.jpg"]);

        Assert.Null(result);
        Assert.Empty(scenario.Photos);
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
        public List<BookingCancellation> Cancellations { get; } = [];
        public List<BookingPhoto> Photos { get; } = [];

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
                .With(scenario.Cancellations)
                .With(scenario.Photos);

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
            BookingType = BookingType.Immediate
        };
    }
}

using AutoMapper;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.BLL.Mapping;
using Cleaning.BLL.Services;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cleaning.BLL.Tests;

public sealed partial class BookingDispatchTests
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

    [Fact(DisplayName = "[UT-BOOK-DSP-06] A Busy worker still sees available immediate jobs (only Offline hides them)")]
    public async Task GetAvailable_WorkerBusy_ImmediateJobStillSurfaced()
    {
        var scenario = DispatchScenario.Create(workerOnlineStatus: WorkerOnlineStatus.Busy);
        scenario.AddBooking(BookingStatus.AwaitingWorker, bookingType: BookingType.Immediate);

        var available = await scenario.BookingService.GetAvailableBookingsAsync(scenario.WorkerId);

        Assert.Single(available);
    }

    [Fact(DisplayName = "[UT-BOOK-DSP-07] An Offline worker does not see available immediate jobs")]
    public async Task GetAvailable_WorkerOffline_ImmediateJobHidden()
    {
        var scenario = DispatchScenario.Create(workerOnlineStatus: WorkerOnlineStatus.Offline);
        scenario.AddBooking(BookingStatus.AwaitingWorker, bookingType: BookingType.Immediate);

        var available = await scenario.BookingService.GetAvailableBookingsAsync(scenario.WorkerId);

        Assert.Empty(available);
    }

    [Fact(DisplayName = "[UT-BOOK-DSP-08] A job overlapping the worker's own accepted booking is still visible to browse")]
    public async Task GetAvailable_OverlapsOwnAcceptedBooking_StillSurfaced()
    {
        var scenario = DispatchScenario.Create();
        var start = DateTime.UtcNow.AddHours(3);
        scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId, start: start, durationHours: 2);
        var overlapping = scenario.AddBooking(BookingStatus.AwaitingWorker, start: start.AddHours(1), durationHours: 2);

        var available = await scenario.BookingService.GetAvailableBookingsAsync(scenario.WorkerId);

        Assert.Single(available, dto => dto.Id == overlapping.Id);
    }

    [Fact(DisplayName = "[UT-BOOK-ACC-06] Accepting a job that time-overlaps the worker's own accepted booking is rejected")]
    public async Task Accept_OverlapsOwnAcceptedBooking_ReturnsFalse()
    {
        var scenario = DispatchScenario.Create();
        var start = DateTime.UtcNow.AddHours(3);
        scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId, start: start, durationHours: 2);
        var overlapping = scenario.AddBooking(BookingStatus.AwaitingWorker, start: start.AddHours(1), durationHours: 2);

        var accepted = await scenario.BookingService.AcceptBookingAsync(overlapping.Id, scenario.WorkerId);

        Assert.False(accepted);
        Assert.Null(overlapping.WorkerId);
    }

    [Fact(DisplayName = "[UT-BOOK-ACC-07] Accepting a job that does not overlap the worker's own accepted booking succeeds")]
    public async Task Accept_NoOverlapWithOwnBooking_Succeeds()
    {
        var scenario = DispatchScenario.Create();
        var start = DateTime.UtcNow.AddHours(3);
        scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId, start: start, durationHours: 2);
        var later = scenario.AddBooking(BookingStatus.AwaitingWorker, start: start.AddHours(3), durationHours: 2);

        var accepted = await scenario.BookingService.AcceptBookingAsync(later.Id, scenario.WorkerId);

        Assert.True(accepted);
        Assert.Equal(scenario.WorkerId, later.WorkerId);
    }

    [Fact(DisplayName = "[UT-BOOK-ACC-08] A Busy worker can still accept a job that doesn't overlap their current one")]
    public async Task Accept_BusyWorkerNoOverlap_Succeeds()
    {
        var scenario = DispatchScenario.Create(workerOnlineStatus: WorkerOnlineStatus.Busy);
        var start = DateTime.UtcNow.AddHours(3);
        scenario.AddBooking(BookingStatus.InProgress, workerId: scenario.WorkerId, start: start, durationHours: 2);
        var later = scenario.AddBooking(
            BookingStatus.AwaitingWorker, bookingType: BookingType.Immediate, start: start.AddHours(3), durationHours: 2);

        var accepted = await scenario.BookingService.AcceptBookingAsync(later.Id, scenario.WorkerId);

        Assert.True(accepted);
    }

    [Fact(DisplayName = "[UT-BOOK-STS-09] Client cancelling a pre-accept AwaitingWorker booking notifies "
        + "the workers who had it in their feed, so it disappears there too")]
    public async Task UpdateStatus_ClientCancelsAwaitingWorker_NotifiesEligibleWorkers()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.AwaitingWorker, bookingType: BookingType.Immediate);

        var updated = await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, scenario.ClientId, new UpdateBookingStatusDto { NewStatus = BookingStatus.Cancelled });

        Assert.True(updated);
        var recipients = Assert.Single(scenario.DispatchPublisher.CancelledRecipients);
        Assert.Contains(scenario.WorkerId, recipients);
    }

    [Fact(DisplayName = "[UT-BOOK-STS-10] Reporting/cancelling an already-accepted booking notifies "
        + "specifically the assigned worker, so their own My Jobs / active-job view updates live")]
    public async Task UpdateStatus_CancelAlreadyAcceptedBooking_NotifiesAssignedWorker()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.InProgress, workerId: scenario.WorkerId);

        var updated = await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, scenario.WorkerId,
            new UpdateBookingStatusDto { NewStatus = BookingStatus.Cancelled, Reason = "Khach vang mat" });

        Assert.True(updated);
        var recipients = Assert.Single(scenario.DispatchPublisher.CancelledRecipients);
        Assert.Equal([scenario.WorkerId], recipients);
    }

    [Fact(DisplayName = "[UT-BOOK-DTL-01] Once a worker is assigned, booking detail exposes their "
        + "name, rating, and current position (needed for the worker card + OnTheWay live map)")]
    public async Task GetBookingById_WorkerAssigned_ExposesWorkerSummary()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);

        var dto = await scenario.BookingService.GetBookingByIdAsync(booking.Id, scenario.ClientId);

        Assert.NotNull(dto!.Worker);
        Assert.Equal(scenario.WorkerId, dto.Worker!.Id);
        Assert.Equal("Anh Ba", dto.Worker.Name);
        Assert.Equal(10.7769m, dto.Worker.Latitude);
        Assert.Equal(106.7009m, dto.Worker.Longitude);
    }

    [Fact(DisplayName = "[UT-BOOK-DTL-02] An unassigned AwaitingWorker booking exposes no worker "
        + "info — candidate workers are never shown to the client during broadcast")]
    public async Task GetBookingById_Unassigned_ExposesNoWorker()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.AwaitingWorker);

        var dto = await scenario.BookingService.GetBookingByIdAsync(booking.Id, scenario.ClientId);

        Assert.Null(dto!.Worker);
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

    [Fact(DisplayName = "[UT-BOOK-STS-11] Accepting a booking pushes a booking-scoped status-changed event, " +
        "so the client's Booking Detail live-updates without waiting on a poll")]
    public async Task AcceptBooking_PublishesBookingStatusChanged()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.AwaitingWorker, bookingType: BookingType.Immediate);

        await scenario.BookingService.AcceptBookingAsync(booking.Id, scenario.WorkerId);

        Assert.Contains(scenario.DispatchPublisher.StatusChanges, change =>
            change.BookingId == booking.Id && change.NewStatus == nameof(BookingStatus.Accepted));
    }

    [Fact(DisplayName = "[UT-BOOK-STS-12] Every allowed status transition pushes a booking-scoped " +
        "status-changed event, not just Accept/Cancel")]
    public async Task UpdateStatus_PublishesBookingStatusChanged()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.OnTheWay, workerId: scenario.WorkerId);

        await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, scenario.WorkerId, new UpdateBookingStatusDto { NewStatus = BookingStatus.InProgress });

        Assert.Contains(scenario.DispatchPublisher.StatusChanges, change =>
            change.BookingId == booking.Id && change.NewStatus == nameof(BookingStatus.InProgress));
    }

    [Fact(DisplayName = "[UT-BOOK-DSP-09] Broadcasting (first post or a retry) bumps UpdatedAt, so the " +
        "client's search countdown restarts instead of staying anchored to the original CreatedAt")]
    public async Task BroadcastBooking_BumpsUpdatedAt()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.AwaitingWorker, bookingType: BookingType.Immediate);
        booking.UpdatedAt = DateTime.UtcNow.AddMinutes(-10);
        var staleUpdatedAt = booking.UpdatedAt;

        await scenario.BookingService.BroadcastBookingAsync(booking.Id);

        Assert.True(booking.UpdatedAt > staleUpdatedAt);
        Assert.True(booking.UpdatedAt > DateTime.UtcNow.AddSeconds(-5));
    }

    public sealed class FakeDispatchPublisher : IDispatchPublisher
    {
        public List<IReadOnlyCollection<Guid>> CancelledRecipients { get; } = [];
        public List<(Guid BookingId, string NewStatus)> StatusChanges { get; } = [];

        public Task JobPostedAsync(BookingDto booking, IReadOnlyCollection<Guid> workerIds) => Task.CompletedTask;
        public Task JobTakenAsync(Guid bookingId, IReadOnlyCollection<Guid> workerIds) => Task.CompletedTask;

        public Task JobCancelledAsync(Guid bookingId, IReadOnlyCollection<Guid> workerIds)
        {
            CancelledRecipients.Add(workerIds);
            return Task.CompletedTask;
        }

        public Task BookingStatusChangedAsync(Guid bookingId, string newStatus)
        {
            StatusChanges.Add((bookingId, newStatus));
            return Task.CompletedTask;
        }

        public Task WorkerPositionAsync(Guid bookingId, decimal latitude, decimal longitude) => Task.CompletedTask;

        public Task NearbyWorkerLocationsAsync(Guid bookingId, IReadOnlyList<NearbyWorkerLocationDto> locations) =>
            Task.CompletedTask;
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
        public List<Payment> Payments { get; } = [];
        public List<Account> Accounts { get; } = [];
        public WorkerProfile Worker { get; private set; } = null!;
        public FakeDispatchPublisher DispatchPublisher { get; } = new();

        public static DispatchScenario Create(
            bool workerSkillVerified = true,
            WorkerOnlineStatus workerOnlineStatus = WorkerOnlineStatus.Online)
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
            scenario.Worker = new WorkerProfile
            {
                UserId = scenario.WorkerId,
                VerificationStatus = "approved",
                OnlineStatus = workerOnlineStatus,
                CurrentLat = 10.7769m,
                CurrentLng = 106.7009m,
                BaseLatitude = 10.7769m,
                BaseLongitude = 106.7009m,
                ServiceRadiusKm = 10,
                LocationUpdatedAt = DateTime.UtcNow
            };

            var workerAccountProfile = new Cleaning.DAL.Entities.Profile
            {
                Id = scenario.WorkerId,
                FullName = "Anh Ba",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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
                .With([scenario.Worker])
                .With([workerAccountProfile])
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
                .With(scenario.Photos)
                .With(scenario.Payments)
                .With(scenario.Accounts);

            var availabilityService = new BookingAvailabilityService(unitOfWork);
            var mapper = new MapperConfiguration(
                configuration => configuration.AddProfile<BookingMappingProfile>(),
                NullLoggerFactory.Instance).CreateMapper();
            var creationService = new BookingCreationService(
                unitOfWork, availabilityService, NullLogger<BookingCreationService>.Instance, mapper);
            scenario.BookingService = new BookingService(
                unitOfWork, NullLogger<BookingService>.Instance, availabilityService, creationService, mapper,
                scenario.DispatchPublisher);
            return scenario;
        }

        public Booking AddBooking(
            BookingStatus status,
            Guid? workerId = null,
            Guid? serviceId = null,
            BookingType bookingType = BookingType.Scheduled,
            DateTime? start = null,
            double durationHours = 2,
            PaymentMethod paymentMethod = PaymentMethod.Cash)
        {
            var scheduledStart = start ?? DateTime.UtcNow.AddHours(3);
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                ClientId = ClientId,
                WorkerId = workerId,
                ServiceId = serviceId ?? ServiceEntity.Id,
                AddressId = Address.Id,
                BookingType = bookingType,
                ScheduledStartTime = scheduledStart,
                ScheduledEndTime = scheduledStart.AddHours(durationHours),
                DurationHours = (decimal)durationHours,
                Status = status,
                PaymentMethod = paymentMethod,
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

        /// Seeds the client's Account row — needed only by tests touching VNPay linking, since the
        /// dispatch scenarios themselves never load Account.
        public Account AddClientAccount(string? vnpayAccount = null)
        {
            var account = new Account
            {
                Id = ClientId,
                Email = "client@test.local",
                VnpayAccount = vnpayAccount,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            Accounts.Add(account);
            return account;
        }
    }
}

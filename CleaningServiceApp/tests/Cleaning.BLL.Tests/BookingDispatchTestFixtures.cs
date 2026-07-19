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
    public sealed class FakeDispatchPublisher : IDispatchPublisher
    {
        public List<IReadOnlyCollection<Guid>> CancelledRecipients { get; } = [];
        public List<(Guid BookingId, Guid ClientId, string NewStatus)> StatusChanges { get; } = [];
        public List<BookingDto> PostedJobs { get; } = [];
        public List<Guid> SuspendedWorkerIds { get; } = [];

        public Task JobPostedAsync(BookingDto booking, IReadOnlyCollection<Guid> workerIds)
        {
            PostedJobs.Add(booking);
            return Task.CompletedTask;
        }
        public Task JobTakenAsync(Guid bookingId, IReadOnlyCollection<Guid> workerIds) => Task.CompletedTask;

        public Task JobCancelledAsync(Guid bookingId, IReadOnlyCollection<Guid> workerIds)
        {
            CancelledRecipients.Add(workerIds);
            return Task.CompletedTask;
        }

        public Task BookingStatusChangedAsync(Guid bookingId, Guid clientId, string newStatus)
        {
            StatusChanges.Add((bookingId, clientId, newStatus));
            return Task.CompletedTask;
        }

        public Task WorkerPositionAsync(Guid bookingId, decimal latitude, decimal longitude) => Task.CompletedTask;

        public Task NearbyWorkerLocationsAsync(Guid bookingId, IReadOnlyList<NearbyWorkerLocationDto> locations) =>
            Task.CompletedTask;

        public Task WorkerSuspendedAsync(Guid workerId)
        {
            SuspendedWorkerIds.Add(workerId);
            return Task.CompletedTask;
        }
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
        public List<WorkerEarning> WorkerEarnings { get; } = [];
        public List<Account> Accounts { get; } = [];
        public WorkerProfile Worker { get; private set; } = null!;
        public FakeDispatchPublisher DispatchPublisher { get; } = new();
        public InMemoryUnitOfWork UnitOfWork { get; private set; } = null!;
        public List<BookingRescheduleRequest> RescheduleRequests { get; } = [];

        public static DispatchScenario Create(
            bool workerSkillVerified = true,
            WorkerOnlineStatus workerOnlineStatus = WorkerOnlineStatus.Online)
        {
            var scenario = new DispatchScenario();
            var serviceId = Guid.NewGuid();

            scenario.ServiceEntity = new Service
            {
                Id = serviceId,
                Name = "Dá»n nhÃ ",
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
                Label = "NhÃ ",
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
                .With(scenario.WorkerEarnings)
                .With(scenario.Accounts)
                .With(scenario.RescheduleRequests);
            scenario.UnitOfWork = unitOfWork;

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

        public Account AddClientAccount()
        {
            var account = new Account
            {
                Id = ClientId,
                Email = "client@test.local",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            Accounts.Add(account);
            return account;
        }
    }
}

using AutoMapper;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Common;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Cleaning.DAL.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cleaning.BLL.Services
{
    public partial class BookingService : IBookingService
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BookingService> _logger;
        private readonly IBookingAvailabilityService _availabilityService;
        private readonly IBookingCreationService _creationService;
        private readonly IMapper _mapper;
        private readonly IDispatchPublisher? _dispatchPublisher;

        public BookingService(IUnitOfWork unitOfWork, ILogger<BookingService> logger, IBookingAvailabilityService availabilityService, IBookingCreationService creationService, IMapper mapper, IDispatchPublisher? dispatchPublisher = null)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _availabilityService = availabilityService;
            _creationService = creationService;
            _mapper = mapper;
            _dispatchPublisher = dispatchPublisher;
        }

        public Task<BookingAvailabilityDto> GetAvailabilityAsync(Guid clientId, BookingAvailabilityRequestDto request) =>
            _availabilityService.GetAsync(clientId, request);

        public Task<PricingBreakdownDto> GetQuoteAsync(Guid clientId, BookingQuoteRequestDto request) =>
            _creationService.GetQuoteAsync(clientId, request);

        public Task<BookingDto> CreateBookingAsync(Guid clientId, string idempotencyKey, CreateBookingDto request) =>
            _creationService.CreateAsync(clientId, idempotencyKey, request);

        public async Task<IEnumerable<BookingDto>> GetClientBookingsAsync(Guid clientId)
        {
            var bookings = (await _unitOfWork.Repository<Booking>().FindAsync(b => b.ClientId == clientId)).ToList();
            await HydrateAsync(bookings);
            return bookings.Select(_mapper.Map<BookingDto>).OrderByDescending(b => b.CreatedAt);
        }

        public async Task<IEnumerable<BookingDto>> GetWorkerBookingsAsync(Guid workerId)
        {
            var bookings = (await _unitOfWork.Repository<Booking>().FindAsync(b => b.WorkerId == workerId)).ToList();
            await HydrateAsync(bookings);
            return bookings.Select(_mapper.Map<BookingDto>).OrderByDescending(b => b.ScheduledStartTime);
        }

        public async Task<bool> UpdateBookingStatusAsync(Guid bookingId, Guid accountId, UpdateBookingStatusDto request)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId);
                if (booking == null) return false;

                if (booking.ClientId != accountId && booking.WorkerId != accountId)
                    return false;

                var oldStatus = booking.Status;
                var isClient = booking.ClientId == accountId;
                var isWorker = booking.WorkerId == accountId;
                if (!IsAllowedTransition(oldStatus, request.NewStatus, isClient, isWorker))
                    return false;
                booking.Status = request.NewStatus;
                booking.UpdatedAt = DateTime.UtcNow;

                if (oldStatus == BookingStatus.Accepted && request.NewStatus == BookingStatus.AwaitingWorker)
                    booking.WorkerId = null;
                if (request.NewStatus == BookingStatus.InProgress)
                    booking.ActualStartTime = DateTime.UtcNow;
                if (request.NewStatus == BookingStatus.PendingPayment)
                    booking.ActualEndTime = DateTime.UtcNow;

                if (request.NewStatus == BookingStatus.Cancelled)
                {
                    await _unitOfWork.Repository<BookingCancellation>().AddAsync(new BookingCancellation
                    {
                        BookingId = booking.Id,
                        CancelledBy = accountId,
                        ActorRole = booking.ClientId == accountId ? UserRole.Client : UserRole.Worker,
                        Reason = request.Reason ?? string.Empty, // Fix: Tránh null
                        CreatedAt = DateTime.UtcNow
                    });
                }

                _unitOfWork.Repository<Booking>().Update(booking);

                var statusLog = new BookingStatusLog
                {
                    BookingId = booking.Id,
                    OldStatus = oldStatus,
                    NewStatus = request.NewStatus,
                    ChangedBy = accountId,
                    Reason = request.Reason ?? string.Empty, // Fix: Tránh null
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<BookingStatusLog>().AddAsync(statusLog);
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
                if (_dispatchPublisher != null)
                {
                    // Cancelling from AwaitingWorker means the job was live in eligible workers' feeds;
                    // includeTaken recomputes eligibility as if it were still AwaitingWorker/unassigned
                    // (booking.Status is already Cancelled at this point) so those workers actually get
                    // told to remove it, instead of an empty recipient list because nothing is eligible
                    // for a booking that's no longer AwaitingWorker.
                    if (request.NewStatus == BookingStatus.Cancelled && oldStatus == BookingStatus.AwaitingWorker)
                    {
                        var recipients = await EligibleWorkerIdsAsync(booking, includeTaken: true);
                        await _dispatchPublisher.JobCancelledAsync(booking.Id, recipients);
                    }
                    else if (request.NewStatus == BookingStatus.Cancelled && booking.WorkerId.HasValue)
                    {
                        // Post-accept cancel/report: this booking was never in anyone else's eligible
                        // feed, so only the worker it was assigned to needs telling — that's what keeps
                        // their own My Jobs / active-job bar from still showing a job that's gone.
                        await _dispatchPublisher.JobCancelledAsync(booking.Id, [booking.WorkerId.Value]);
                    }
                    else if (request.NewStatus == BookingStatus.AwaitingWorker)
                    {
                        await BroadcastBookingAsync(booking.Id);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái BookingId: {BookingId}", bookingId);
                return false;
            }
        }

        private async Task HydrateAsync(IReadOnlyCollection<Booking> bookings)
        {
            if (bookings.Count == 0) return;

            var serviceIds = bookings.Select(b => b.ServiceId).Distinct().ToHashSet();
            var services = (await _unitOfWork.Repository<Service>().FindAsync(s => serviceIds.Contains(s.Id)))
                .ToDictionary(s => s.Id);

            var addressIds = bookings.Where(b => b.AddressId.HasValue)
                .Select(b => b.AddressId!.Value).Distinct().ToHashSet();
            var addresses = addressIds.Count == 0
                ? new Dictionary<Guid, UserAddress>()
                : (await _unitOfWork.Repository<UserAddress>().FindAsync(a => addressIds.Contains(a.Id)))
                    .ToDictionary(a => a.Id);

            // Only for bookings that actually have an assigned worker — candidate workers during
            // broadcast (WorkerId == null) are never hydrated or exposed to the client.
            var workerIds = bookings.Where(b => b.WorkerId.HasValue)
                .Select(b => b.WorkerId!.Value).Distinct().ToHashSet();
            var workers = new Dictionary<Guid, WorkerProfile>();
            if (workerIds.Count > 0)
            {
                var workerProfiles = await _unitOfWork.Repository<WorkerProfile>()
                    .FindAsync(w => workerIds.Contains(w.UserId));
                var workerAccountProfiles = (await _unitOfWork.Repository<Cleaning.DAL.Entities.Profile>()
                    .FindAsync(p => workerIds.Contains(p.Id))).ToDictionary(p => p.Id);
                foreach (var workerProfile in workerProfiles)
                {
                    if (workerAccountProfiles.TryGetValue(workerProfile.UserId, out var account))
                        workerProfile.User = account;
                    workers[workerProfile.UserId] = workerProfile;
                }
            }

            foreach (var b in bookings)
            {
                if (services.TryGetValue(b.ServiceId, out var service))
                    b.Service = service;
                if (b.AddressId.HasValue && addresses.TryGetValue(b.AddressId.Value, out var address))
                    b.Address = address;
                if (b.WorkerId.HasValue && workers.TryGetValue(b.WorkerId.Value, out var worker))
                    b.Worker = worker;
            }
        }

        private static bool IsAllowedTransition(
            BookingStatus from,
            BookingStatus to,
            bool isClient,
            bool isWorker) =>
            (from, to) switch
            {
                (BookingStatus.AwaitingWorker, BookingStatus.Cancelled) => isClient,
                (BookingStatus.Accepted, BookingStatus.OnTheWay) => isWorker,
                (BookingStatus.OnTheWay, BookingStatus.InProgress) => isWorker,
                (BookingStatus.InProgress, BookingStatus.PendingPayment) => isWorker,
                (BookingStatus.PendingPayment, BookingStatus.Completed) => isWorker,
                (BookingStatus.Accepted, BookingStatus.RescheduleRequested) => isClient || isWorker,
                (BookingStatus.RescheduleRequested, BookingStatus.Accepted) => isClient || isWorker,
                (BookingStatus.Accepted, BookingStatus.AwaitingWorker) => isWorker,
                (_, BookingStatus.Cancelled) when from is BookingStatus.Accepted
                    or BookingStatus.RescheduleRequested
                    or BookingStatus.OnTheWay
                    or BookingStatus.InProgress
                    or BookingStatus.PendingPayment => isClient || isWorker,
                _ => false
            };

    }
}

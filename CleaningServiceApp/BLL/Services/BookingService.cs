using AutoMapper;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Cleaning.DAL.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cleaning.BLL.Services
{
    public class BookingService : IBookingService
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BookingService> _logger;
        private readonly IBookingAvailabilityService _availabilityService;
        private readonly IBookingCreationService _creationService;
        private readonly IMapper _mapper;

        public BookingService(IUnitOfWork unitOfWork, ILogger<BookingService> logger, IBookingAvailabilityService availabilityService, IBookingCreationService creationService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _availabilityService = availabilityService;
            _creationService = creationService;
            _mapper = mapper;
        }

        public Task<BookingAvailabilityDto> GetAvailabilityAsync(Guid clientId, BookingAvailabilityRequestDto request) =>
            _availabilityService.GetAsync(clientId, request);

        public Task<PricingBreakdownDto> GetQuoteAsync(Guid clientId, BookingQuoteRequestDto request) =>
            _creationService.GetQuoteAsync(clientId, request);

        public Task<BookingDto> CreateBookingAsync(Guid clientId, string idempotencyKey, CreateBookingDto request) =>
            _creationService.CreateAsync(clientId, idempotencyKey, request);

        public async Task<IEnumerable<BookingDto>> GetClientBookingsAsync(Guid clientId)
        {
            var bookings = await _unitOfWork.Repository<Booking>().FindAsync(b => b.ClientId == clientId);

            foreach (var b in bookings)
            {
                b.Service = await _unitOfWork.Repository<Service>().GetByIdAsync(b.ServiceId);
                if (b.AddressId.HasValue)
                    b.Address = await _unitOfWork.Repository<UserAddress>().GetByIdAsync(b.AddressId.Value);
            }
            return bookings.Select(_mapper.Map<BookingDto>).OrderByDescending(b => b.CreatedAt);
        }

        public async Task<IEnumerable<BookingDto>> GetWorkerBookingsAsync(Guid workerId)
        {
            var bookings = await _unitOfWork.Repository<Booking>().FindAsync(b => b.WorkerId == workerId);
            foreach (var b in bookings)
            {
                b.Service = await _unitOfWork.Repository<Service>().GetByIdAsync(b.ServiceId);
                if (b.AddressId.HasValue)
                    b.Address = await _unitOfWork.Repository<UserAddress>().GetByIdAsync(b.AddressId.Value);
            }
            return bookings.Select(_mapper.Map<BookingDto>).OrderByDescending(b => b.ScheduledStartTime);
        }

        public async Task<bool> UpdateBookingStatusAsync(Guid bookingId, Guid accountId, UpdateBookingStatusDto request)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId);
                if (booking == null) return false;

                var oldStatus = booking.Status;
                booking.Status = request.NewStatus;
                booking.UpdatedAt = DateTime.UtcNow;

                if (request.NewStatus == BookingStatus.Cancelled)
                {
                    await _unitOfWork.Repository<BookingCancellation>().AddAsync(new BookingCancellation
                    {
                        BookingId = booking.Id,
                        CancelledBy = accountId,
                        Reason = request.Reason ?? string.Empty, // Fix: Tránh null
                        CancellationFee = 0,
                        RefundAmount = 0,
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
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái BookingId: {BookingId}", bookingId);
                return false;
            }
        }

        public async Task<bool> AcceptBookingAsync(Guid bookingId, Guid workerId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId);

                if (booking == null || booking.WorkerId != null || !IsAwaitingWorker(booking.Status))
                    return false;

                var oldStatus = booking.Status;
                booking.WorkerId = workerId;
                booking.Status = BookingStatus.Accepted;
                booking.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Repository<Booking>().Update(booking);

                var statusLog = new BookingStatusLog
                {
                    BookingId = booking.Id,
                    OldStatus = oldStatus,
                    NewStatus = BookingStatus.Accepted,
                    ChangedBy = workerId,
                    Reason = "Thợ đã nhận đơn",
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<BookingStatusLog>().AddAsync(statusLog);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi thợ {WorkerId} nhận đơn {BookingId}", workerId, bookingId);
                return false;
            }
        }

        public async Task<IEnumerable<BookingDto>> GetAvailableBookingsAsync(Guid workerId)
        {
            // Dispatch: only surface jobs the worker is actually eligible for — services they are
            // verified to perform — so the "correct" workers see the incoming task on their screen.
            var verifiedServiceIds = (await _unitOfWork.Repository<Cleaning.DAL.Entities.WorkerService>()
                    .FindAsync(ws => ws.WorkerId == workerId && ws.IsVerified))
                .Select(ws => ws.ServiceId)
                .ToHashSet();

            var availableBookings = (await _unitOfWork.Repository<Booking>()
                    .FindAsync(b => (b.Status == BookingStatus.PaidPendingWorker || b.Status == BookingStatus.AwaitingWorker)
                                    && b.WorkerId == null))
                .Where(b => verifiedServiceIds.Contains(b.ServiceId))
                .ToList();

            foreach (var b in availableBookings)
            {
                b.Service = await _unitOfWork.Repository<Service>().GetByIdAsync(b.ServiceId);
                if (b.AddressId.HasValue)
                    b.Address = await _unitOfWork.Repository<UserAddress>().GetByIdAsync(b.AddressId.Value);
            }

            return availableBookings.Select(_mapper.Map<BookingDto>).OrderBy(b => b.ScheduledStartTime);
        }

        public async Task<BookingDto?> GetBookingByIdAsync(Guid bookingId)
        {
            var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId);
            if (booking == null) return null;

            booking.Service = await _unitOfWork.Repository<Service>().GetByIdAsync(booking.ServiceId);
            if (booking.AddressId.HasValue)
            {
                booking.Address = await _unitOfWork.Repository<UserAddress>().GetByIdAsync(booking.AddressId.Value);
            }

            return _mapper.Map<BookingDto>(booking);
        }

        private static bool IsAwaitingWorker(BookingStatus status) =>
            status is BookingStatus.PaidPendingWorker or BookingStatus.AwaitingWorker;

    }
}

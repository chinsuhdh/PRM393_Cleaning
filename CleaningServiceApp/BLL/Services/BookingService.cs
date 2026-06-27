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

        public BookingService(IUnitOfWork unitOfWork, ILogger<BookingService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<BookingDto> CreateBookingAsync(Guid clientId, CreateBookingDto request)
        {
            var service = await _unitOfWork.Repository<Service>().GetByIdAsync(request.ServiceId)
                          ?? throw new Exception("Không tìm thấy dịch vụ này.");

            decimal unitPrice = service.BasePrice;
            decimal extraFee = 0;
            decimal discountAmount = request.DiscountAmount;
            decimal totalPrice = (unitPrice * request.DurationHours) + extraFee - discountAmount;

            if (totalPrice < 0) totalPrice = 0;

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var booking = new Booking
                {
                    ClientId = clientId,
                    ServiceId = request.ServiceId,
                    AddressId = request.AddressId,
                    BookingType = request.BookingType,
                    ScheduledStartTime = request.ScheduledStartTime,
                    ScheduledEndTime = request.ScheduledStartTime.AddHours((double)request.DurationHours),
                    DurationHours = request.DurationHours,
                    UnitPrice = unitPrice,
                    ExtraFee = extraFee,
                    DiscountAmount = discountAmount,
                    TotalPrice = totalPrice,
                    Status = BookingStatus.PendingPayment,
                    Notes = request.Notes ?? string.Empty, // Fix: Tránh gán null
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<Booking>().AddAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                var statusLog = new BookingStatusLog
                {
                    BookingId = booking.Id,
                    OldStatus = null,
                    NewStatus = BookingStatus.PendingPayment,
                    ChangedBy = clientId,
                    Reason = "Khách hàng tạo đơn đặt lịch",
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<BookingStatusLog>().AddAsync(statusLog);
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();

                booking.Service = service;
                if (request.AddressId.HasValue)
                {
                    booking.Address = await _unitOfWork.Repository<UserAddress>().GetByIdAsync(request.AddressId.Value);
                }

                return MapToDto(booking);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi xảy ra khi tạo Booking cho ClientId: {ClientId}", clientId);
                throw new Exception("Lỗi hệ thống khi tạo đơn. Vui lòng thử lại sau.");
            }
        }

        public async Task<IEnumerable<BookingDto>> GetClientBookingsAsync(Guid clientId)
        {
            var bookings = await _unitOfWork.Repository<Booking>().FindAsync(b => b.ClientId == clientId);

            foreach (var b in bookings)
            {
                b.Service = await _unitOfWork.Repository<Service>().GetByIdAsync(b.ServiceId);
                if (b.AddressId.HasValue)
                    b.Address = await _unitOfWork.Repository<UserAddress>().GetByIdAsync(b.AddressId.Value);
            }
            return bookings.Select(MapToDto).OrderByDescending(b => b.CreatedAt);
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
            return bookings.Select(MapToDto).OrderByDescending(b => b.ScheduledStartTime);
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

                if (booking == null || booking.WorkerId != null || booking.Status != BookingStatus.PaidPendingWorker)
                    return false;

                booking.WorkerId = workerId;
                booking.Status = BookingStatus.Accepted;
                booking.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Repository<Booking>().Update(booking);

                var statusLog = new BookingStatusLog
                {
                    BookingId = booking.Id,
                    OldStatus = BookingStatus.PaidPendingWorker,
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

        public async Task<IEnumerable<BookingDto>> GetAvailableBookingsAsync()
        {
            var availableBookings = await _unitOfWork.Repository<Booking>()
                .FindAsync(b => b.Status == BookingStatus.PaidPendingWorker && b.WorkerId == null);

            foreach (var b in availableBookings)
            {
                b.Service = await _unitOfWork.Repository<Service>().GetByIdAsync(b.ServiceId);
                if (b.AddressId.HasValue)
                    b.Address = await _unitOfWork.Repository<UserAddress>().GetByIdAsync(b.AddressId.Value);
            }

            return availableBookings.Select(MapToDto).OrderBy(b => b.ScheduledStartTime);
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

            return MapToDto(booking);
        }

        private static BookingDto MapToDto(Booking b)
        {
            return new BookingDto
            {
                Id = b.Id,
                ClientId = b.ClientId,
                WorkerId = b.WorkerId,
                ServiceId = b.ServiceId,
                AddressId = b.AddressId,
                BookingType = b.BookingType.ToString(),
                ScheduledStartTime = b.ScheduledStartTime,
                ScheduledEndTime = b.ScheduledEndTime,
                DurationHours = b.DurationHours,
                UnitPrice = b.UnitPrice,
                ExtraFee = b.ExtraFee,
                DiscountAmount = b.DiscountAmount,
                TotalPrice = b.TotalPrice,
                Status = b.Status.ToString(),
                Notes = b.Notes ?? string.Empty,
                CreatedAt = b.CreatedAt,
                ServiceName = b.Service?.Name ?? string.Empty,
                AddressText = b.Address?.AddressText ?? string.Empty,
                Latitude = b.Address?.Latitude,
                Longitude = b.Address?.Longitude
            };
        }
    }
}
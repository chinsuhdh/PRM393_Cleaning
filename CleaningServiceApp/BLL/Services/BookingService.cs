using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Cleaning.DAL.Interfaces;

namespace Cleaning.BLL.Services
{
    public class BookingService : IBookingService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BookingService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BookingDto> CreateBookingAsync(Guid clientId, CreateBookingDto request)
        {
            var service = await _unitOfWork.Repository<Service>().GetByIdAsync(request.ServiceId)
                          ?? throw new Exception("Service not found.");

            if (request.ScheduledEndTime <= request.ScheduledStartTime)
            {
                throw new Exception("Scheduled end time must be after start time.");
            }

            var durationHours = Math.Max(request.DurationHours, service.MinimumHours);
            var duplicateBookings = await _unitOfWork.Repository<Booking>().FindAsync(b =>
                b.ClientId == clientId &&
                b.ServiceId == request.ServiceId &&
                b.ScheduledStartTime == request.ScheduledStartTime &&
                b.ScheduledEndTime == request.ScheduledEndTime &&
                b.Status != BookingStatus.Cancelled &&
                b.Status != BookingStatus.Refunded);

            if (duplicateBookings.Any())
            {
                throw new Exception("Bạn đã đặt dịch vụ này vào khung giờ này rồi. Vui lòng kiểm tra lại giỏ hàng của bạn!");
            }

            decimal unitPrice = service.BasePrice;
            decimal extraFee = 0;
            decimal discountAmount = 0;
            decimal totalPrice = (unitPrice * durationHours) + extraFee - discountAmount;

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
                    ScheduledEndTime = request.ScheduledEndTime,
                    DurationHours = durationHours,
                    UnitPrice = unitPrice,
                    ExtraFee = extraFee,
                    DiscountAmount = discountAmount,
                    TotalPrice = totalPrice,
                    Status = BookingStatus.PendingPayment,
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<Booking>().AddAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.Repository<BookingStatusLog>().AddAsync(new BookingStatusLog
                {
                    BookingId = booking.Id,
                    OldStatus = null,
                    NewStatus = BookingStatus.PendingPayment,
                    ChangedBy = clientId,
                    Reason = "Khách hàng tạo đơn đặt lịch, chờ thanh toán",
                    CreatedAt = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                booking.Service = service;
                if (request.AddressId.HasValue)
                {
                    booking.Address = await _unitOfWork.Repository<UserAddress>().GetByIdAsync(request.AddressId.Value);
                }

                return MapToDto(booking);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<BookingDto>> GetClientBookingsAsync(Guid clientId)
        {
            var bookings = await _unitOfWork.Repository<Booking>().FindAsync(b => b.ClientId == clientId);
            await LoadBookingDetailsAsync(bookings);
            return bookings.Select(MapToDto).OrderByDescending(b => b.CreatedAt);
        }

        public async Task<IEnumerable<BookingDto>> GetWorkerBookingsAsync(Guid workerId)
        {
            var bookings = await _unitOfWork.Repository<Booking>().FindAsync(b => b.WorkerId == workerId);
            await LoadBookingDetailsAsync(bookings);
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
                _unitOfWork.Repository<Booking>().Update(booking);

                await _unitOfWork.Repository<BookingStatusLog>().AddAsync(new BookingStatusLog
                {
                    BookingId = booking.Id,
                    OldStatus = oldStatus,
                    NewStatus = request.NewStatus,
                    ChangedBy = accountId,
                    Reason = request.Reason,
                    CreatedAt = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
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

                await _unitOfWork.Repository<BookingStatusLog>().AddAsync(new BookingStatusLog
                {
                    BookingId = booking.Id,
                    OldStatus = BookingStatus.PaidPendingWorker,
                    NewStatus = BookingStatus.Accepted,
                    ChangedBy = workerId,
                    Reason = "Thợ đã nhận đơn",
                    CreatedAt = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<IEnumerable<BookingDto>> GetAvailableBookingsAsync()
        {
            var availableBookings = await _unitOfWork.Repository<Booking>()
                .FindAsync(b => b.Status == BookingStatus.PaidPendingWorker && b.WorkerId == null);

            await LoadBookingDetailsAsync(availableBookings);
            return availableBookings.Select(MapToDto).OrderBy(b => b.ScheduledStartTime);
        }

        public async Task<BookingDto?> GetBookingByIdAsync(Guid bookingId)
        {
            var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId);
            if (booking == null) return null;

            await LoadBookingDetailsAsync(new[] { booking });
            return MapToDto(booking);
        }

        private async Task LoadBookingDetailsAsync(IEnumerable<Booking> bookings)
        {
            foreach (var booking in bookings)
            {
                booking.Service = await _unitOfWork.Repository<Service>().GetByIdAsync(booking.ServiceId)
                                  ?? booking.Service;
                if (booking.AddressId.HasValue)
                {
                    booking.Address = await _unitOfWork.Repository<UserAddress>().GetByIdAsync(booking.AddressId.Value);
                }
            }
        }

        private static BookingDto MapToDto(Booking booking)
        {
            return new BookingDto
            {
                Id = booking.Id,
                ClientId = booking.ClientId,
                WorkerId = booking.WorkerId,
                ServiceId = booking.ServiceId,
                AddressId = booking.AddressId,
                BookingType = booking.BookingType.ToString(),
                ScheduledStartTime = booking.ScheduledStartTime,
                ScheduledEndTime = booking.ScheduledEndTime,
                ActualStartTime = booking.ActualStartTime,
                ActualEndTime = booking.ActualEndTime,
                DurationHours = booking.DurationHours,
                UnitPrice = booking.UnitPrice,
                ExtraFee = booking.ExtraFee,
                DiscountAmount = booking.DiscountAmount,
                TotalPrice = booking.TotalPrice,
                Status = booking.Status.ToString(),
                Notes = booking.Notes,
                CreatedAt = booking.CreatedAt,
                ServiceName = booking.Service?.Name,
                AddressText = booking.Address?.AddressText,
                Latitude = booking.Address?.Latitude,
                Longitude = booking.Address?.Longitude
            };
        }
    }
}

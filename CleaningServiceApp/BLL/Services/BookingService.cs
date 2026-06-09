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
            // 1. Kiểm tra Dịch vụ có tồn tại không
            var service = await _unitOfWork.Repository<Service>().GetByIdAsync(request.ServiceId)
                          ?? throw new Exception("Service not found.");

            // ==========================================
            // 2. LOGIC CHỐNG DOUBLE BOOKING 
            // ==========================================
            var duplicateBookings = await _unitOfWork.Repository<Booking>().FindAsync(b =>
                b.ClientId == clientId &&
                b.ServiceId == request.ServiceId &&
                b.ScheduledTime == request.ScheduledTime &&
                b.Status != BookingStatus.Cancelled);

            if (duplicateBookings.Any())
            {
                throw new Exception("Bạn đã đặt dịch vụ này vào khung giờ này rồi. Vui lòng kiểm tra lại giỏ hàng của bạn!");
            }
            // ==========================================

            // 3. Tính toán giá tiền
            decimal unitPrice = service.BasePrice;
            decimal extraFee = 0; // Logic tính phụ phí (Lễ/Tết) có thể bổ sung sau
            decimal totalPrice = (unitPrice * request.Quantity) + extraFee;

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var booking = new Booking
                {
                    ClientId = clientId,
                    ServiceId = request.ServiceId,
                    AddressId = request.AddressId,
                    ScheduledTime = request.ScheduledTime,
                    DurationHours = request.DurationHours,
                    Quantity = request.Quantity,
                    UnitPrice = unitPrice,
                    ExtraFee = extraFee,
                    TotalPrice = totalPrice,
                    Status = BookingStatus.Pending,
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<Booking>().AddAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                // Ghi log trạng thái khởi tạo
                var statusLog = new BookingStatusLog
                {
                    BookingId = booking.Id,
                    OldStatus = null,
                    NewStatus = BookingStatus.Pending,
                    ChangedBy = clientId,
                    Reason = "Khách hàng tạo đơn đặt lịch",
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<BookingStatusLog>().AddAsync(statusLog);
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();

                // Nạp thêm thông tin Service để trả về đúng tên dịch vụ ngay lập tức
                booking.Service = service;

                // Nạp thêm thông tin Address nếu có
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

            // Nạp thêm dữ liệu Dịch vụ và Địa chỉ
            foreach (var b in bookings)
            {
                b.Service = await _unitOfWork.Repository<Service>().GetByIdAsync(b.ServiceId);
                if (b.AddressId.HasValue)
                {
                    b.Address = await _unitOfWork.Repository<UserAddress>().GetByIdAsync(b.AddressId.Value);
                }
            }

            return bookings.Select(MapToDto).OrderByDescending(b => b.CreatedAt);
        }

        public async Task<IEnumerable<BookingDto>> GetWorkerBookingsAsync(Guid workerId)
        {
            var bookings = await _unitOfWork.Repository<Booking>().FindAsync(b => b.WorkerId == workerId);

            // Nạp thêm dữ liệu Dịch vụ và Địa chỉ
            foreach (var b in bookings)
            {
                b.Service = await _unitOfWork.Repository<Service>().GetByIdAsync(b.ServiceId);
                if (b.AddressId.HasValue)
                {
                    b.Address = await _unitOfWork.Repository<UserAddress>().GetByIdAsync(b.AddressId.Value);
                }
            }

            return bookings.Select(MapToDto).OrderByDescending(b => b.ScheduledTime);
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
                    booking.CancelReason = request.Reason;
                }

                _unitOfWork.Repository<Booking>().Update(booking);

                // Ghi log sự thay đổi
                var statusLog = new BookingStatusLog
                {
                    BookingId = booking.Id,
                    OldStatus = oldStatus,
                    NewStatus = request.NewStatus,
                    ChangedBy = accountId,
                    Reason = request.Reason,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<BookingStatusLog>().AddAsync(statusLog);
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

                // Kiểm tra xem đơn còn trống không 
                if (booking == null || booking.WorkerId != null || booking.Status != BookingStatus.Pending)
                    return false;

                // Cập nhật thông tin người nhận và trạng thái
                booking.WorkerId = workerId;
                booking.Status = BookingStatus.Accepted;
                booking.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Repository<Booking>().Update(booking);

                // Ghi log chuyển trạng thái
                var statusLog = new BookingStatusLog
                {
                    BookingId = booking.Id,
                    OldStatus = BookingStatus.Pending,
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
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<IEnumerable<BookingDto>> GetAvailableBookingsAsync()
        {
            // Lấy các đơn hàng Pending và chưa có thợ nhận
            var availableBookings = await _unitOfWork.Repository<Booking>()
                .FindAsync(b => b.Status == BookingStatus.Pending && b.WorkerId == null);

            // Nạp thêm dữ liệu Dịch vụ và Địa chỉ
            foreach (var b in availableBookings)
            {
                b.Service = await _unitOfWork.Repository<Service>().GetByIdAsync(b.ServiceId);
                if (b.AddressId.HasValue)
                {
                    b.Address = await _unitOfWork.Repository<UserAddress>().GetByIdAsync(b.AddressId.Value);
                }
            }

            return availableBookings.Select(MapToDto).OrderBy(b => b.ScheduledTime);
        }

        // Hàm GetBookingByIdAsync
        public async Task<BookingDto?> GetBookingByIdAsync(Guid bookingId)
        {
            var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId);

            if (booking == null) return null;

            // Nạp thêm dữ liệu Dịch vụ và Địa chỉ
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
                ScheduledTime = b.ScheduledTime,
                DurationHours = b.DurationHours,
                Quantity = b.Quantity,
                UnitPrice = b.UnitPrice,
                ExtraFee = b.ExtraFee,
                TotalPrice = b.TotalPrice,
                Status = b.Status.ToString(),
                Notes = b.Notes,
                CreatedAt = b.CreatedAt,

                // Map thông tin để đẩy xuống App
                ServiceName = b.Service?.Name,
                AddressText = b.Address?.AddressText,
                Latitude = b.Address?.Latitude,
                Longitude = b.Address?.Longitude
            };
        }
    }
}
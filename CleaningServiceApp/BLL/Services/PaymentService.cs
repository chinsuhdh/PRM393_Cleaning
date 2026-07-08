using Cleaning.BLL.Common;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Cleaning.DAL.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cleaning.BLL.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(IUnitOfWork unitOfWork, ILogger<PaymentService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto request)
        {
            // 1. Validate Booking có tồn tại và đúng trạng thái không
            var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(request.BookingId)
                          ?? throw new Exception("Không tìm thấy đơn đặt lịch.");

            if (booking.Status != BookingStatus.PendingPayment)
                throw new Exception("Đơn đặt lịch không ở trạng thái chờ thanh toán.");

            // 2. Bảo mật: Lấy giá trị TotalPrice từ DB, không tin tưởng Amount từ Client
            decimal finalAmount = booking.TotalPrice;

            // 3. Xử lý ràng buộc UNIQUE: Kiểm tra xem đã có record Payment cho Booking này chưa
            var existingPayments = await _unitOfWork.Repository<Payment>().FindAsync(p => p.BookingId == request.BookingId);
            var paymentRecord = existingPayments.FirstOrDefault();

            if (paymentRecord != null)
            {
                if (paymentRecord.Status == PaymentStatus.Success)
                    throw new Exception("Đơn hàng này đã được thanh toán.");

                // Nếu có record cũ (có thể do lần trước thanh toán lỗi/hủy), ta Update lại Method và Amount
                paymentRecord.Method = request.Method;
                paymentRecord.Amount = finalAmount;
                paymentRecord.Status = PaymentStatus.Pending;
                paymentRecord.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Repository<Payment>().Update(paymentRecord);
                await _unitOfWork.SaveChangesAsync();

                return MapToDto(paymentRecord);
            }

            // 4. Nếu chưa có, tạo mới
            var payment = new Payment
            {
                BookingId = request.BookingId,
                Amount = finalAmount, // Dùng tiền từ DB
                Method = request.Method,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Payment>().AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(payment);
        }

        public async Task<PaymentDto?> GetPaymentByBookingAsync(Guid bookingId)
        {
            var payments = await _unitOfWork.Repository<Payment>().FindAsync(p => p.BookingId == bookingId);
            var payment = payments.FirstOrDefault();

            return payment != null ? MapToDto(payment) : null;
        }

        public async Task<bool> ProcessPaymentCallbackAsync(Guid paymentId, PaymentCallbackDto request)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var payment = await _unitOfWork.Repository<Payment>().GetByIdAsync(paymentId);
                if (payment == null) return false;

                // Idempotency: Nếu webhook gọi 2 lần cho 1 giao dịch thành công, ta bỏ qua
                if (payment.Status == PaymentStatus.Success) return true;

                payment.Status = request.Status;
                payment.TransactionId = request.TransactionId;
                payment.UpdatedAt = DateTime.UtcNow;

                if (request.Status == PaymentStatus.Success)
                {
                    payment.PaidAt = DateTime.UtcNow;

                    // Pay-after-job: thanh toán thành công đóng đơn — PendingPayment -> Completed
                    var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(payment.BookingId);
                    if (booking != null && booking.Status == BookingStatus.PendingPayment)
                    {
                        var oldStatus = booking.Status;
                        booking.Status = BookingStatus.Completed;
                        booking.UpdatedAt = DateTime.UtcNow;
                        _unitOfWork.Repository<Booking>().Update(booking);

                        // Ghi log trạng thái (ChangedBy = null vì đây là hệ thống tự đổi)
                        var statusLog = new BookingStatusLog
                        {
                            BookingId = booking.Id,
                            OldStatus = oldStatus,
                            NewStatus = BookingStatus.Completed,
                            ChangedBy = null,
                            Reason = "Hệ thống xác nhận thanh toán thành công",
                            CreatedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.Repository<BookingStatusLog>().AddAsync(statusLog);
                    }
                }

                _unitOfWork.Repository<Payment>().Update(payment);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi xử lý Payment Callback cho PaymentId: {PaymentId}", paymentId);
                return false;
            }
        }

        public async Task<VnpayAccountDto> GetVnpayAccountAsync(Guid accountId)
        {
            var account = await _unitOfWork.Repository<Account>().GetByIdAsync(accountId)
                          ?? throw new AppException(AppErrors.NotFound);
            return new VnpayAccountDto { VnpayAccount = account.VnpayAccount };
        }

        // Cổng VNPay mô phỏng: mọi chuỗi không rỗng đều "liên kết" thành công, không gọi hệ thống ngoài.
        public async Task<VnpayAccountDto> LinkVnpayAccountAsync(Guid accountId, VnpayAccountDto request)
        {
            var value = request.VnpayAccount?.Trim();
            if (string.IsNullOrEmpty(value))
                throw new AppException(AppErrors.VnpayAccountInvalid);

            var account = await _unitOfWork.Repository<Account>().GetByIdAsync(accountId)
                          ?? throw new AppException(AppErrors.NotFound);

            account.VnpayAccount = value;
            account.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<Account>().Update(account);
            await _unitOfWork.SaveChangesAsync();

            return new VnpayAccountDto { VnpayAccount = account.VnpayAccount };
        }

        private static PaymentDto MapToDto(Payment p)
        {
            return new PaymentDto
            {
                Id = p.Id,
                BookingId = p.BookingId,
                Amount = p.Amount,
                Method = p.Method.ToString(),
                Status = p.Status.ToString(),
                TransactionId = p.TransactionId,
                PaidAt = p.PaidAt,
                CreatedAt = p.CreatedAt
            };
        }
    }
}
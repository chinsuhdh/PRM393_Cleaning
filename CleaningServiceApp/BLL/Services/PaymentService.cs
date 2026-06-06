using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Cleaning.DAL.Interfaces;

namespace Cleaning.BLL.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PaymentService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto request)
        {
            var payment = new Payment
            {
                BookingId = request.BookingId,
                Amount = request.Amount,
                Method = request.Method,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Payment>().AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(payment);
        }

        public async Task<IEnumerable<PaymentDto>> GetPaymentsByBookingAsync(Guid bookingId)
        {
            var payments = await _unitOfWork.Repository<Payment>().FindAsync(p => p.BookingId == bookingId);
            return payments.Select(MapToDto).OrderByDescending(p => p.CreatedAt);
        }

        public async Task<bool> ProcessPaymentCallbackAsync(Guid paymentId, PaymentCallbackDto request)
        {
            var payment = await _unitOfWork.Repository<Payment>().GetByIdAsync(paymentId);
            if (payment == null) return false;

            payment.Status = request.Status;
            payment.TransactionId = request.TransactionId;

            if (request.Status == PaymentStatus.Success)
            {
                payment.PaidAt = DateTime.UtcNow;
            }

            _unitOfWork.Repository<Payment>().Update(payment);
            await _unitOfWork.SaveChangesAsync();

            return true;
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
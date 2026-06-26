using Cleaning.BLL.DTOs;

namespace Cleaning.BLL.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto request);
        Task<PaymentDto?> GetPaymentByBookingAsync(Guid bookingId); // Trả về 1 record duy nhất
        Task<bool> ProcessPaymentCallbackAsync(Guid paymentId, PaymentCallbackDto request);
    }
}
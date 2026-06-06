// File: IPaymentService.cs
using Cleaning.BLL.DTOs;

namespace Cleaning.BLL.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto request);
        Task<IEnumerable<PaymentDto>> GetPaymentsByBookingAsync(Guid bookingId);
        Task<bool> ProcessPaymentCallbackAsync(Guid paymentId, PaymentCallbackDto request);
    }
}
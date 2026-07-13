using Cleaning.BLL.DTOs;

namespace Cleaning.BLL.Interfaces
{
    public interface IPaymentService
    {
        Task<PayNowResponseDto> PayNowAsync(Guid clientId, PayNowRequestDto request, string ipAddress);
        Task<PaymentDto?> GetPaymentByBookingAsync(Guid bookingId);
        Task<bool> ProcessPayOsWebhookAsync(string rawJson);
    }
}
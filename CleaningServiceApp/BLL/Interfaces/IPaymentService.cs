using Cleaning.BLL.DTOs;

namespace Cleaning.BLL.Interfaces
{
    public enum VnpayConfirmOutcome
    {
        Success,
        OrderAlreadyConfirmed,
        OrderNotFound,
        InvalidAmount,
        InvalidSignature,
        UnknownError
    }

    public interface IPaymentService
    {
        Task<PayNowResponseDto> PayNowAsync(Guid clientId, PayNowRequestDto request, string ipAddress);
        Task<PaymentDto?> GetPaymentByBookingAsync(Guid bookingId);
        Task<VnpayConfirmOutcome> ConfirmVnpayPaymentAsync(IReadOnlyDictionary<string, string> queryParams);
    }
}

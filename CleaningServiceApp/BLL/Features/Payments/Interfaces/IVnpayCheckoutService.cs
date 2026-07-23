namespace Cleaning.BLL.Features.Payments;

public sealed record VnpayCheckoutLink(string TxnRef, string PaymentUrl);

public sealed record VnpayCallbackResult(
    bool SignatureValid, bool Success, string TxnRef, decimal Amount, string? TransactionNo, string ResponseCode);

public interface IVnpayCheckoutService
{
    VnpayCheckoutLink CreatePaymentUrl(decimal amount, string orderInfo, string ipAddress);

    VnpayCallbackResult VerifyCallback(IReadOnlyDictionary<string, string> queryParams);
}

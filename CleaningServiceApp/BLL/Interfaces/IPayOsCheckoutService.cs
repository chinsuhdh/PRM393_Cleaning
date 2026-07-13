namespace Cleaning.BLL.Interfaces;

public sealed record PayOsCheckoutLink(long OrderCode, string CheckoutUrl);

public sealed record PayOsWebhookResult(bool Success, long OrderCode, decimal Amount, string? Reference);

public interface IPayOsCheckoutService
{
    Task<PayOsCheckoutLink> CreatePaymentLinkAsync(decimal amount, string description, CancellationToken ct = default);

    Task<PayOsWebhookResult?> VerifyWebhookAsync(string rawJson, CancellationToken ct = default);
}

using System.Text.Json;
using Cleaning.BLL.Common;
using Cleaning.BLL.Interfaces;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;

namespace Cleaning.BLL.Services;

public class PayOsCheckoutService : IPayOsCheckoutService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PayOSClient _client;
    private readonly PayOsSettings _settings;

    public PayOsCheckoutService(HttpClient httpClient, IOptions<PayOsSettings> options)
    {
        _settings = options.Value;
        _client = new PayOSClient(new PayOSOptions
        {
            ClientId = _settings.ClientId,
            ApiKey = _settings.ApiKey,
            ChecksumKey = _settings.ChecksumKey,
            HttpClient = httpClient
        });
    }

    public async Task<PayOsCheckoutLink> CreatePaymentLinkAsync(
        decimal amount, string description, CancellationToken ct = default)
    {
        var orderCode = GenerateOrderCode();
        var truncatedDescription = description.Length > 25 ? description[..25] : description;

        var response = await _client.PaymentRequests.CreateAsync(
            new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = (long)Math.Round(amount, MidpointRounding.AwayFromZero),
                Description = truncatedDescription,
                CancelUrl = _settings.CancelUrl,
                ReturnUrl = _settings.ReturnUrl
            },
            new RequestOptions<CreatePaymentLinkRequest> { CancellationToken = ct });

        return new PayOsCheckoutLink(orderCode, response.CheckoutUrl);
    }

    public async Task<PayOsWebhookResult?> VerifyWebhookAsync(string rawJson, CancellationToken ct = default)
    {
        var webhook = JsonSerializer.Deserialize<Webhook>(rawJson, JsonOptions);
        if (webhook == null) return null;

        try
        {
            var data = await _client.Webhooks.VerifyAsync(webhook);
            return new PayOsWebhookResult(webhook.Success, data.OrderCode, data.Amount, data.Reference);
        }
        catch
        {
            return null;
        }
    }

    private static long GenerateOrderCode() =>
        BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0) & 0x1FFFFFFFFFFFFF;
}

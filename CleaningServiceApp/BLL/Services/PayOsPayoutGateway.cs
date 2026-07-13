using Cleaning.BLL.Common;
using Cleaning.BLL.Interfaces;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models;
using PayOS.Models.V1.Payouts;

namespace Cleaning.BLL.Services;

public class PayOsPayoutGateway : IPayoutGateway
{
    private readonly PayOSClient _client;

    public PayOsPayoutGateway(HttpClient httpClient, IOptions<PayOsSettings> options)
    {
        var settings = options.Value;
        _client = new PayOSClient(new PayOSOptions
        {
            ClientId = settings.ClientId,
            ApiKey = settings.ApiKey,
            ChecksumKey = settings.ChecksumKey,
            HttpClient = httpClient
        });
    }

    public async Task<PayoutResult> PayAsync(
        Guid earningId, decimal amount, string toBin, string toAccountNumber, string description,
        CancellationToken ct = default)
    {
        var payout = await _client.Payouts.CreateAsync(
            new PayoutRequest
            {
                ReferenceId = earningId.ToString(),
                Amount = (long)Math.Round(amount, MidpointRounding.AwayFromZero),
                Description = description,
                ToBin = toBin,
                ToAccountNumber = toAccountNumber,
                Category = ["salary"]
            },
            idempotencyKey: earningId.ToString(),
            requestOptions: new RequestOptions<Payout> { CancellationToken = ct });

        return MapResult(payout);
    }

    public async Task<PayoutResult> GetStatusAsync(string payoutId, CancellationToken ct = default)
    {
        var payout = await _client.Payouts.GetAsync(payoutId, new RequestOptions { CancellationToken = ct });
        return MapResult(payout);
    }

    private static PayoutResult MapResult(Payout payout)
    {
        var transaction = payout.Transactions?.FirstOrDefault();
        if (transaction != null)
        {
            return transaction.State switch
            {
                PayoutTransactionState.Succeeded => new PayoutResult(PayoutState.Succeeded, payout.Id, null),
                PayoutTransactionState.Failed or PayoutTransactionState.Cancelled or PayoutTransactionState.Reversed =>
                    new PayoutResult(PayoutState.Failed, payout.Id, transaction.ErrorMessage ?? transaction.ErrorCode),
                _ => new PayoutResult(PayoutState.Processing, payout.Id, null)
            };
        }

        return payout.ApprovalState switch
        {
            PayoutApprovalState.Completed => new PayoutResult(PayoutState.Succeeded, payout.Id, null),
            PayoutApprovalState.Rejected or PayoutApprovalState.Cancelled or PayoutApprovalState.Failed =>
                new PayoutResult(PayoutState.Failed, payout.Id, $"payOS approval state: {payout.ApprovalState}"),
            _ => new PayoutResult(PayoutState.Processing, payout.Id, null)
        };
    }
}

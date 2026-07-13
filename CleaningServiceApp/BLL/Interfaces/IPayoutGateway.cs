namespace Cleaning.BLL.Interfaces;

public enum PayoutState
{
    Processing,
    Succeeded,
    Failed
}

public sealed record PayoutResult(PayoutState State, string? PayoutId, string? FailureReason);

public interface IPayoutGateway
{
    Task<PayoutResult> PayAsync(
        Guid earningId, decimal amount, string toBin, string toAccountNumber, string description,
        CancellationToken ct = default);

    Task<PayoutResult> GetStatusAsync(string payoutId, CancellationToken ct = default);
}

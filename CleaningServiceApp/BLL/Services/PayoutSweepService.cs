using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cleaning.BLL.Services;

public sealed class PayoutSweepService(
    IUnitOfWork unitOfWork,
    IPayoutGateway payoutGateway,
    ILogger<PayoutSweepService> logger) : IPayoutSweepService
{
    public async Task RunTickAsync(CancellationToken cancellationToken = default)
    {
        await ReconcileProcessingPayoutsAsync(cancellationToken);
        await RetryPendingPayoutsWithBankInfoAsync(cancellationToken);
    }

    private async Task ReconcileProcessingPayoutsAsync(CancellationToken cancellationToken)
    {
        var processing = await unitOfWork.Repository<WorkerEarning>()
            .FindAsync(e => e.Status == "processing" && e.PayoutId != null);

        foreach (var earning in processing)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var result = await payoutGateway.GetStatusAsync(earning.PayoutId!, cancellationToken);
                if (result.State == PayoutState.Succeeded)
                {
                    earning.Status = "paid";
                    earning.PaidAt = DateTime.UtcNow;
                    unitOfWork.Repository<WorkerEarning>().Update(earning);
                    await unitOfWork.SaveChangesAsync();
                }
                else if (result.State == PayoutState.Failed)
                {
                    earning.Status = "pending";
                    earning.PayoutId = null;
                    earning.PayoutFailureReason = result.FailureReason;
                    unitOfWork.Repository<WorkerEarning>().Update(earning);
                    await unitOfWork.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Payout status poll failed for EarningId: {EarningId}", earning.Id);
            }
        }
    }

    private async Task RetryPendingPayoutsWithBankInfoAsync(CancellationToken cancellationToken)
    {
        var pending = await unitOfWork.Repository<WorkerEarning>()
            .FindAsync(e => e.Status == "pending" && e.PayoutId == null);

        foreach (var earning in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var worker = await unitOfWork.Repository<WorkerProfile>().GetByIdAsync(earning.WorkerId);
            if (string.IsNullOrWhiteSpace(worker?.PayoutBankBin) || string.IsNullOrWhiteSpace(worker.PayoutBankAccountNumber))
                continue;

            try
            {
                var result = await payoutGateway.PayAsync(
                    earning.Id, earning.Amount, worker.PayoutBankBin, worker.PayoutBankAccountNumber,
                    $"Thanh toan don {earning.BookingId}", cancellationToken);

                earning.PayoutId = result.PayoutId;
                earning.PayoutFailureReason = result.FailureReason;
                if (result.State == PayoutState.Succeeded)
                {
                    earning.Status = "paid";
                    earning.PaidAt = DateTime.UtcNow;
                }
                else if (result.State == PayoutState.Processing)
                {
                    earning.Status = "processing";
                }

                unitOfWork.Repository<WorkerEarning>().Update(earning);
                await unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Retry payout failed for EarningId: {EarningId}", earning.Id);
            }
        }
    }
}

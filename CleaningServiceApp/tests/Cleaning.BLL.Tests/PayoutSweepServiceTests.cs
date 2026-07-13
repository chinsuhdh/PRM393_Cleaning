using Cleaning.BLL.Interfaces;
using Cleaning.BLL.Services;
using Cleaning.DAL.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Cleaning.BLL.Tests;

public class PayoutSweepServiceTests
{
    private static WorkerEarning CreateEarning(Guid workerId, string status, string? payoutId = null) => new()
    {
        Id = Guid.NewGuid(),
        BookingId = Guid.NewGuid(),
        WorkerId = workerId,
        Amount = 250_000m,
        Status = status,
        PayoutId = payoutId,
        EarnedAt = DateTime.UtcNow,
        CreatedAt = DateTime.UtcNow
    };

    [Fact(DisplayName = "[UT-BE-PAYOUT-SWEEP-01] A processing payout confirmed Succeeded is finalized to paid")]
    public async Task RunTickAsync_ProcessingPayoutSucceeded_MarksPaid()
    {
        var workerId = Guid.NewGuid();
        var earning = CreateEarning(workerId, "processing", "payout-1");
        var unitOfWork = new InMemoryUnitOfWork().With([earning]).With(new List<WorkerProfile>());
        var payoutGateway = new Mock<IPayoutGateway>();
        payoutGateway.Setup(g => g.GetStatusAsync("payout-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayoutResult(PayoutState.Succeeded, "payout-1", null));
        var service = new PayoutSweepService(unitOfWork, payoutGateway.Object, NullLogger<PayoutSweepService>.Instance);

        await service.RunTickAsync();

        Assert.Equal("paid", earning.Status);
        Assert.NotNull(earning.PaidAt);
    }

    [Fact(DisplayName = "[UT-BE-PAYOUT-SWEEP-02] A processing payout confirmed Failed resets to pending for retry")]
    public async Task RunTickAsync_ProcessingPayoutFailed_ResetsToPending()
    {
        var workerId = Guid.NewGuid();
        var earning = CreateEarning(workerId, "processing", "payout-1");
        var unitOfWork = new InMemoryUnitOfWork().With([earning]).With(new List<WorkerProfile>());
        var payoutGateway = new Mock<IPayoutGateway>();
        payoutGateway.Setup(g => g.GetStatusAsync("payout-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayoutResult(PayoutState.Failed, "payout-1", "insufficient balance"));
        var service = new PayoutSweepService(unitOfWork, payoutGateway.Object, NullLogger<PayoutSweepService>.Instance);

        await service.RunTickAsync();

        Assert.Equal("pending", earning.Status);
        Assert.Null(earning.PayoutId);
        Assert.Equal("insufficient balance", earning.PayoutFailureReason);
    }

    [Fact(DisplayName = "[UT-BE-PAYOUT-SWEEP-03] A pending earning whose worker now has bank info is retried")]
    public async Task RunTickAsync_PendingWithNewBankInfo_AttemptsPayout()
    {
        var workerId = Guid.NewGuid();
        var earning = CreateEarning(workerId, "pending");
        var worker = new WorkerProfile
        {
            UserId = workerId, PayoutBankBin = "970422", PayoutBankAccountNumber = "0123456789",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        var unitOfWork = new InMemoryUnitOfWork().With([earning]).With([worker]);
        var payoutGateway = new Mock<IPayoutGateway>();
        payoutGateway.Setup(g => g.PayAsync(
                earning.Id, 250_000m, "970422", "0123456789", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayoutResult(PayoutState.Succeeded, "payout-2", null));
        var service = new PayoutSweepService(unitOfWork, payoutGateway.Object, NullLogger<PayoutSweepService>.Instance);

        await service.RunTickAsync();

        Assert.Equal("paid", earning.Status);
        Assert.Equal("payout-2", earning.PayoutId);
    }

    [Fact(DisplayName = "[UT-BE-PAYOUT-SWEEP-04] A pending earning whose worker still has no bank info is left untouched")]
    public async Task RunTickAsync_PendingWithoutBankInfo_DoesNotCallGateway()
    {
        var workerId = Guid.NewGuid();
        var earning = CreateEarning(workerId, "pending");
        var worker = new WorkerProfile { UserId = workerId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var unitOfWork = new InMemoryUnitOfWork().With([earning]).With([worker]);
        var payoutGateway = new Mock<IPayoutGateway>(MockBehavior.Strict);
        var service = new PayoutSweepService(unitOfWork, payoutGateway.Object, NullLogger<PayoutSweepService>.Instance);

        await service.RunTickAsync();

        Assert.Equal("pending", earning.Status);
        payoutGateway.VerifyNoOtherCalls();
    }
}

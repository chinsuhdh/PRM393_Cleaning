using Cleaning.BLL.Common;
using Cleaning.BLL.DTOs;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Cleaning.DAL.Interfaces;
using Moq;

using WorkerService = Cleaning.BLL.Services.WorkerService;

namespace Cleaning.BLL.Tests;

public class WorkerServiceTests
{
    private static (Mock<IGenericRepository<WorkerProfile>> repo, Mock<IUnitOfWork> uow) MockUnitOfWork(WorkerProfile? worker)
    {
        var repository = new Mock<IGenericRepository<WorkerProfile>>();
        repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(worker);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(w => w.Repository<WorkerProfile>()).Returns(repository.Object);
        unitOfWork.Setup(w => w.SaveChangesAsync()).ReturnsAsync(1);
        return (repository, unitOfWork);
    }

    [Fact(DisplayName = "[UT-WORKER-ONLINE-01] Toggling Offline to Online succeeds")]
    public async Task UpdateOnlineStatusAsync_OfflineToOnline_Succeeds()
    {
        var workerId = Guid.NewGuid();
        var worker = new WorkerProfile { UserId = workerId, OnlineStatus = WorkerOnlineStatus.Offline };
        var (repository, unitOfWork) = MockUnitOfWork(worker);
        var service = new WorkerService(unitOfWork.Object);

        var result = await service.UpdateOnlineStatusAsync(workerId, new UpdateOnlineStatusDto { OnlineStatus = WorkerOnlineStatus.Online });

        Assert.True(result);
        Assert.Equal(WorkerOnlineStatus.Online, worker.OnlineStatus);
        repository.Verify(r => r.Update(worker), Times.Once);
    }

    [Fact(DisplayName = "[UT-WORKER-ONLINE-02] Toggling to Online while Busy is rejected")]
    public async Task UpdateOnlineStatusAsync_BusyToOnline_Rejected()
    {
        var workerId = Guid.NewGuid();
        var worker = new WorkerProfile { UserId = workerId, OnlineStatus = WorkerOnlineStatus.Busy };
        var (_, unitOfWork) = MockUnitOfWork(worker);
        var service = new WorkerService(unitOfWork.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateOnlineStatusAsync(workerId, new UpdateOnlineStatusDto { OnlineStatus = WorkerOnlineStatus.Online }));
        Assert.Equal(WorkerOnlineStatus.Busy, worker.OnlineStatus);
    }

    [Fact(DisplayName = "[UT-WORKER-ONLINE-03] Toggling to Offline while Busy is allowed")]
    public async Task UpdateOnlineStatusAsync_BusyToOffline_Allowed()
    {
        var workerId = Guid.NewGuid();
        var worker = new WorkerProfile { UserId = workerId, OnlineStatus = WorkerOnlineStatus.Busy };
        var (_, unitOfWork) = MockUnitOfWork(worker);
        var service = new WorkerService(unitOfWork.Object);

        var result = await service.UpdateOnlineStatusAsync(workerId, new UpdateOnlineStatusDto { OnlineStatus = WorkerOnlineStatus.Offline });

        Assert.True(result);
        Assert.Equal(WorkerOnlineStatus.Offline, worker.OnlineStatus);
    }

    [Fact(DisplayName = "[UT-WORKER-ONLINE-04] An unknown worker returns false instead of throwing")]
    public async Task UpdateOnlineStatusAsync_UnknownWorker_ReturnsFalse()
    {
        var (_, unitOfWork) = MockUnitOfWork(null);
        var service = new WorkerService(unitOfWork.Object);

        var result = await service.UpdateOnlineStatusAsync(Guid.NewGuid(), new UpdateOnlineStatusDto { OnlineStatus = WorkerOnlineStatus.Online });

        Assert.False(result);
    }

    [Fact(DisplayName = "[UT-WORKER-ONLINE-05] Requesting Busy directly is rejected regardless of current status")]
    public async Task UpdateOnlineStatusAsync_RequestingBusy_Rejected()
    {
        var workerId = Guid.NewGuid();
        var worker = new WorkerProfile { UserId = workerId, OnlineStatus = WorkerOnlineStatus.Offline };
        var (_, unitOfWork) = MockUnitOfWork(worker);
        var service = new WorkerService(unitOfWork.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateOnlineStatusAsync(workerId, new UpdateOnlineStatusDto { OnlineStatus = WorkerOnlineStatus.Busy }));
    }

    [Fact(DisplayName = "[UT-WRK-SUS-06] A suspended worker cannot go back Online")]
    public async Task UpdateOnlineStatusAsync_SuspendedToOnline_ThrowsWorkerSuspended()
    {
        var workerId = Guid.NewGuid();
        var worker = new WorkerProfile
        {
            UserId = workerId,
            OnlineStatus = WorkerOnlineStatus.Offline,
            SuspendedAt = DateTime.UtcNow
        };
        var (_, unitOfWork) = MockUnitOfWork(worker);
        var service = new WorkerService(unitOfWork.Object);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.UpdateOnlineStatusAsync(workerId, new UpdateOnlineStatusDto { OnlineStatus = WorkerOnlineStatus.Online }));
        Assert.Equal(AppErrors.WorkerSuspended.Code, ex.Code);
        Assert.Equal(WorkerOnlineStatus.Offline, worker.OnlineStatus);
    }

    [Fact(DisplayName = "[UT-WRK-SUS-07] A suspended worker can still go Offline")]
    public async Task UpdateOnlineStatusAsync_SuspendedToOffline_Allowed()
    {
        var workerId = Guid.NewGuid();
        var worker = new WorkerProfile
        {
            UserId = workerId,
            OnlineStatus = WorkerOnlineStatus.Offline,
            SuspendedAt = DateTime.UtcNow
        };
        var (_, unitOfWork) = MockUnitOfWork(worker);
        var service = new WorkerService(unitOfWork.Object);

        var result = await service.UpdateOnlineStatusAsync(workerId, new UpdateOnlineStatusDto { OnlineStatus = WorkerOnlineStatus.Offline });

        Assert.True(result);
    }

    [Fact(DisplayName = "[UT-WORKER-PAYOUT-01] Updating the payout account stores trimmed bank details")]
    public async Task UpdatePayoutAccountAsync_ValidInput_StoresTrimmedValues()
    {
        var workerId = Guid.NewGuid();
        var worker = new WorkerProfile { UserId = workerId };
        var (repository, unitOfWork) = MockUnitOfWork(worker);
        var service = new WorkerService(unitOfWork.Object);

        var result = await service.UpdatePayoutAccountAsync(workerId, new UpdatePayoutAccountDto
        {
            BankBin = " 970422 ",
            AccountNumber = " 0123456789 ",
            AccountName = " Nguyen Van A "
        });

        Assert.True(result);
        Assert.Equal("970422", worker.PayoutBankBin);
        Assert.Equal("0123456789", worker.PayoutBankAccountNumber);
        Assert.Equal("Nguyen Van A", worker.PayoutBankAccountName);
        repository.Verify(r => r.Update(worker), Times.Once);
    }

    [Fact(DisplayName = "[UT-WORKER-PAYOUT-02] Blank bank details are rejected with PAYOUT_ACCOUNT_INVALID")]
    public async Task UpdatePayoutAccountAsync_BlankInput_Throws()
    {
        var workerId = Guid.NewGuid();
        var worker = new WorkerProfile { UserId = workerId };
        var (_, unitOfWork) = MockUnitOfWork(worker);
        var service = new WorkerService(unitOfWork.Object);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.UpdatePayoutAccountAsync(workerId, new UpdatePayoutAccountDto
            {
                BankBin = "970422",
                AccountNumber = "   ",
                AccountName = "Nguyen Van A"
            }));

        Assert.Equal(AppErrors.PayoutAccountInvalid.Code, ex.Code);
    }

    [Fact(DisplayName = "[UT-WORKER-PAYOUT-03] Updating the payout account for an unknown worker returns false")]
    public async Task UpdatePayoutAccountAsync_UnknownWorker_ReturnsFalse()
    {
        var (_, unitOfWork) = MockUnitOfWork(null);
        var service = new WorkerService(unitOfWork.Object);

        var result = await service.UpdatePayoutAccountAsync(Guid.NewGuid(), new UpdatePayoutAccountDto
        {
            BankBin = "970422",
            AccountNumber = "0123456789",
            AccountName = "Nguyen Van A"
        });

        Assert.False(result);
    }

    [Fact(DisplayName = "[UT-WORKER-PAYOUT-04] Earnings are returned newest-first")]
    public async Task GetWorkerEarningsAsync_ReturnsOrderedByEarnedAtDescending()
    {
        var workerId = Guid.NewGuid();
        var older = new WorkerEarning
        {
            Id = Guid.NewGuid(), WorkerId = workerId, BookingId = Guid.NewGuid(), Amount = 100_000m,
            Status = "paid", EarnedAt = DateTime.UtcNow.AddDays(-1)
        };
        var newer = new WorkerEarning
        {
            Id = Guid.NewGuid(), WorkerId = workerId, BookingId = Guid.NewGuid(), Amount = 200_000m,
            Status = "pending", EarnedAt = DateTime.UtcNow
        };
        var repository = new Mock<IGenericRepository<WorkerEarning>>();
        repository.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<WorkerEarning, bool>>>()))
            .ReturnsAsync([older, newer]);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(w => w.Repository<WorkerEarning>()).Returns(repository.Object);
        var service = new WorkerService(unitOfWork.Object);

        var result = (await service.GetWorkerEarningsAsync(workerId)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(newer.Id, result[0].Id);
        Assert.Equal(older.Id, result[1].Id);
    }
}

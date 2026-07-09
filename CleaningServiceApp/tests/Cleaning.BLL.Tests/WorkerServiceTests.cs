using Cleaning.BLL.DTOs;
using Cleaning.BLL.Services;
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
}

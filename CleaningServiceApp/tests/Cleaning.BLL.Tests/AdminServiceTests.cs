using Cleaning.BLL.Features.Admin;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cleaning.BLL.Tests;

public sealed class AdminServiceTests
{
    [Fact(DisplayName = "[UT-ADM-01] GetDashboardStatsAsync returns correct totals")]
    public async Task GetDashboardStatsAsync_ReturnsCorrectTotals()
    {
        // Arrange
        var clients = new[] {
            new Account { Id = Guid.NewGuid(), Role = UserRole.Client, Email = "client1@test.com" },
            new Account { Id = Guid.NewGuid(), Role = UserRole.Client, Email = "client2@test.com" }
        };
        var workers = new[] {
            new Account { Id = Guid.NewGuid(), Role = UserRole.Worker, Email = "worker1@test.com" }
        };
        var bookings = new[] {
            new Booking { Id = Guid.NewGuid(), Status = BookingStatus.Completed, TotalPrice = 150000m },
            new Booking { Id = Guid.NewGuid(), Status = BookingStatus.Completed, TotalPrice = 200000m },
            new Booking { Id = Guid.NewGuid(), Status = BookingStatus.PendingPayment, TotalPrice = 100000m }
        };

        var unitOfWork = new InMemoryUnitOfWork()
            .With(clients.Concat(workers).ToList())
            .With(bookings.ToList());
        var service = new AdminService(unitOfWork, TestMapperFactory.Create());

        // Act
        var stats = await service.GetDashboardStatsAsync();

        // Assert
        Assert.Equal(2, stats.TotalClients);
        Assert.Equal(1, stats.TotalWorkers);
        Assert.Equal(3, stats.TotalBookings);
        Assert.Equal(350000m, stats.TotalRevenue);
    }

    [Fact(DisplayName = "[UT-ADM-02] ApproveWorkerApplicationAsync updates status and creates profile")]
    public async Task ApproveWorkerApplicationAsync_UpdatesStatusAndCreatesProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var account = new Account { Id = userId, Role = UserRole.Client, Email = "newworker@test.com" };
        var application = new WorkerApplication { Id = appId, UserId = userId, Status = "pending" };

        var unitOfWork = new InMemoryUnitOfWork().With(new List<Account> { account }).With(new List<WorkerApplication> { application });
        var service = new AdminService(unitOfWork, TestMapperFactory.Create());

        // Act
        var result = await service.ApproveWorkerApplicationAsync(appId, new ApproveWorkerApplicationDto { AdminId = adminId });

        // Assert
        Assert.True(result);
        var updatedApp = await unitOfWork.Repository<WorkerApplication>().GetByIdAsync(appId);
        Assert.Equal("approved", updatedApp!.Status);
        Assert.Equal(adminId, updatedApp.ReviewedBy);
        Assert.NotNull(updatedApp.ReviewedAt);

        var updatedAccount = await unitOfWork.Repository<Account>().GetByIdAsync(userId);
        Assert.Equal(UserRole.Worker, updatedAccount!.Role);

        // Note: The profile creation logic should be verified as well, assuming it's in UnitOfWork
        var profiles = unitOfWork.Repository<WorkerProfile>().GetQueryable().ToList();
        Assert.Single(profiles);
        Assert.Equal(userId, profiles[0].UserId);
    }

    [Fact(DisplayName = "[UT-ADM-03] ChangeAccountStatusAsync updates user status")]
    public async Task ChangeAccountStatusAsync_UpdatesUserStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var account = new Account { Id = userId, Status = AccountStatus.Active, Email = "test@test.com" };

        var unitOfWork = new InMemoryUnitOfWork().With(new List<Account> { account });
        var service = new AdminService(unitOfWork, TestMapperFactory.Create());

        // Act
        var result = await service.ChangeAccountStatusAsync(userId, new UpdateAccountStatusDto { Status = "Banned" });

        // Assert
        Assert.True(result);
        var updatedAccount = await unitOfWork.Repository<Account>().GetByIdAsync(userId);
        Assert.NotNull(updatedAccount);
        Assert.Equal(AccountStatus.Banned, updatedAccount.Status);
    }
    [Fact(DisplayName = "[UT-ADM-04] GetAllServicesAsync returns all services")]
    public async Task GetAllServicesAsync_ReturnsAllServices()
    {
        // Arrange
        var services = new[] {
            new Service { Id = Guid.NewGuid(), Name = "Basic Cleaning", PropertyType = PropertyType.House, UnitType = ServiceUnitType.Hour, IsActive = true, BasePrice = 100000 },
            new Service { Id = Guid.NewGuid(), Name = "Deep Cleaning", PropertyType = PropertyType.Apartment, UnitType = ServiceUnitType.Hour, IsActive = false, BasePrice = 200000 }
        };

        var unitOfWork = new InMemoryUnitOfWork().With(services.ToList());
        var service = new AdminService(unitOfWork, TestMapperFactory.Create());

        // Act
        var result = await service.GetAllServicesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, s => s.Name == "Basic Cleaning");
        Assert.Contains(result, s => s.Name == "Deep Cleaning");
    }
}

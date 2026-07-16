using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Cleaning.BLL.DTOs;
using Cleaning.DAL.Data;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CleaningService.API.Tests;

[Collection(ApiTestCollection.Name)]
public sealed class AdminApiTests(PostgreSqlApiFixture fixture) : IAsyncLifetime
{
    private static readonly Guid AdminId = Guid.Parse("10000000-0000-0000-0000-000000000000");
    private static readonly Guid ClientId = Guid.Parse("20000000-0000-0000-0000-000000000000");
    private static readonly Guid WorkerId = Guid.Parse("30000000-0000-0000-0000-000000000000");
    private static readonly Guid ServiceId = Guid.Parse("40000000-0000-0000-0000-000000000000");
    private static readonly Guid ApplicationId = Guid.Parse("50000000-0000-0000-0000-000000000000");

    public async Task InitializeAsync()
    {
        await fixture.ResetDatabaseAsync();
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;

        // Seed Admin Account
        db.Accounts.Add(BookingApiTestData.Account(AdminId, "admin@test.local", UserRole.Admin, now));
        db.Profiles.Add(BookingApiTestData.Profile(AdminId, "Admin", now));

        // Seed Client Account
        db.Accounts.Add(BookingApiTestData.Account(ClientId, "client@test.local", UserRole.Client, now));
        db.Profiles.Add(BookingApiTestData.Profile(ClientId, "Client", now));

        // Seed Pending Worker Account and Application
        db.Accounts.Add(BookingApiTestData.Account(WorkerId, "pendingworker@test.local", UserRole.Client, now));
        db.Profiles.Add(BookingApiTestData.Profile(WorkerId, "Pending Worker", now));
        db.WorkerApplications.Add(new WorkerApplication
        {
            Id = ApplicationId,
            UserId = WorkerId,
            Status = "pending",
            CreatedAt = now,
            UpdatedAt = now,
            SubmittedAt = now
        });

        // Seed a Service
        db.Services.Add(new Service
        {
            Id = ServiceId,
            Name = "House Cleaning",
            PropertyType = PropertyType.House,
            UnitType = ServiceUnitType.Hour,
            BasePrice = 100000,
            MinimumHours = 2,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });

        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "[IT-ADM-API-01] GetDashboardStats returns 200 OK for Admin")]
    public async Task GetDashboardStats_AsAdmin_Succeeds()
    {
        using var client = AuthenticatedClient(AdminId, UserRole.Admin);
        var response = await client.GetAsync("/api/Admin/dashboard-stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stats = await response.Content.ReadDataAsync<AdminDashboardStatsDto>();
        Assert.NotNull(stats);
        Assert.Equal(2, stats!.TotalClients); // ClientId and WorkerId are currently Clients
    }

    [Fact(DisplayName = "[IT-ADM-API-02] ApproveWorkerApplication upgrades role and creates profile")]
    public async Task ApproveWorkerApplication_AsAdmin_Succeeds()
    {
        using var client = AuthenticatedClient(AdminId, UserRole.Admin);
        var dto = new ApproveWorkerApplicationDto { AdminId = AdminId };
        var response = await client.PutAsJsonAsync($"/api/Admin/worker-applications/{ApplicationId}/approve", dto);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var account = await db.Accounts.FindAsync(WorkerId);
        var app = await db.WorkerApplications.FindAsync(ApplicationId);
        var profile = await db.WorkerProfiles.FirstOrDefaultAsync(p => p.UserId == WorkerId);

        Assert.Equal(UserRole.Worker, account!.Role);
        Assert.Equal("approved", app!.Status);
        Assert.NotNull(profile);
    }

    [Fact(DisplayName = "[IT-ADM-API-03] RejectWorkerApplication updates status and reason")]
    public async Task RejectWorkerApplication_AsAdmin_Succeeds()
    {
        using var client = AuthenticatedClient(AdminId, UserRole.Admin);
        var dto = new RejectWorkerApplicationDto { AdminId = AdminId, Reason = "Insufficient experience" };
        var response = await client.PutAsJsonAsync($"/api/Admin/worker-applications/{ApplicationId}/reject", dto);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var app = await db.WorkerApplications.FindAsync(ApplicationId);

        Assert.Equal("rejected", app!.Status);
        Assert.Equal("Insufficient experience", app.RejectionReason);
    }

    [Fact(DisplayName = "[IT-ADM-API-04] CreateService adds a new service")]
    public async Task CreateService_AsAdmin_Succeeds()
    {
        using var client = AuthenticatedClient(AdminId, UserRole.Admin);
        var dto = new CreateServiceDto
        {
            Name = "Deep Cleaning",
            PropertyType = "Apartment",
            UnitType = "Hour",
            BasePrice = 150000,
            MinimumHours = 3
        };
        var response = await client.PostAsJsonAsync("/api/Admin/services", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdService = await response.Content.ReadDataAsync<ServiceDto>();
        Assert.NotNull(createdService);
        Assert.Equal("Deep Cleaning", createdService!.Name);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var serviceInDb = await db.Services.FindAsync(createdService.Id);
        Assert.NotNull(serviceInDb);
    }

    [Fact(DisplayName = "[IT-ADM-API-05] UpdateService modifies an existing service")]
    public async Task UpdateService_AsAdmin_Succeeds()
    {
        using var client = AuthenticatedClient(AdminId, UserRole.Admin);
        var dto = new UpdateServiceDto
        {
            Name = "House Cleaning Updated",
            BasePrice = 120000,
            MinimumHours = 2,
            IsActive = false
        };
        var response = await client.PutAsJsonAsync($"/api/Admin/services/{ServiceId}", dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var serviceInDb = await db.Services.FindAsync(ServiceId);

        Assert.Equal("House Cleaning Updated", serviceInDb!.Name);
        Assert.False(serviceInDb.IsActive);
    }

    [Fact(DisplayName = "[IT-ADM-API-06] ChangeAccountStatus modifies account status")]
    public async Task ChangeAccountStatus_AsAdmin_Succeeds()
    {
        using var client = AuthenticatedClient(AdminId, UserRole.Admin);
        var dto = new UpdateAccountStatusDto { Status = "Banned" };
        var response = await client.PutAsJsonAsync($"/api/Admin/accounts/{ClientId}/status", dto);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var accountInDb = await db.Accounts.FindAsync(ClientId);

        Assert.Equal(AccountStatus.Banned, accountInDb!.Status);
    }

    [Fact(DisplayName = "[IT-ADM-API-07] Unauthenticated calls are rejected")]
    public async Task AdminEndpoints_Unauthenticated_Rejected()
    {
        var response = await fixture.Client.GetAsync("/api/Admin/dashboard-stats");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "[IT-ADM-API-08] Non-admin calls are forbidden")]
    public async Task AdminEndpoints_NonAdmin_Forbidden()
    {
        using var client = AuthenticatedClient(ClientId, UserRole.Client);
        var response = await client.GetAsync("/api/Admin/dashboard-stats");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
    [Fact(DisplayName = "[IT-ADM-API-09] GetAllServices returns 200 OK and list of services")]
    public async Task GetAllServices_AsAdmin_Succeeds()
    {
        // Arrange
        using var client = AuthenticatedClient(AdminId, UserRole.Admin);

        // Act
        var response = await client.GetAsync("/api/Admin/services");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var services = await response.Content.ReadDataAsync<IEnumerable<ServiceDto>>();
        Assert.NotNull(services);
        Assert.NotEmpty(services);
        Assert.Contains(services, s => s.Id == ServiceId && s.Name == "House Cleaning");
    }
    private HttpClient AuthenticatedClient(Guid accountId, UserRole role)
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(accountId, role));
        return client;
    }

    private static string CreateToken(Guid accountId, UserRole role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            "3YlcESfqMCfUbxi5yDM0lAb7oh6XiOAniuq4Nm50Gjw="));
        var token = new JwtSecurityToken(
            issuer: "CleaningService.Api.Tests",
            audience: "CleaningService.Api.Tests",
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, accountId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, accountId.ToString()),
                new Claim(ClaimTypes.Role, role.ToString())
            ],
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

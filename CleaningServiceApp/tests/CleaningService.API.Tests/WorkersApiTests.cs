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
public sealed class WorkersApiTests(PostgreSqlApiFixture fixture) : IAsyncLifetime
{
    private static readonly Guid WorkerId = Guid.Parse("91000000-0000-0000-0000-000000000001");

    public async Task InitializeAsync()
    {
        await fixture.ResetDatabaseAsync();
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        db.Accounts.Add(BookingApiTestData.Account(WorkerId, "workers-api-worker@test.local", UserRole.Worker, now));
        db.Profiles.Add(BookingApiTestData.Profile(WorkerId, "Workers Api Worker", now));
        db.WorkerProfiles.Add(new WorkerProfile
        {
            UserId = WorkerId,
            OnlineStatus = WorkerOnlineStatus.Offline,
            VerificationStatus = "approved",
            CreatedAt = now,
            UpdatedAt = now
        });
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "[UT-WORKER-ONLINE-API-01] A worker can toggle Offline to Online")]
    public async Task UpdateOnlineStatus_OfflineToOnline_Succeeds()
    {
        using var client = AuthenticatedClient(WorkerId, UserRole.Worker);
        var response = await client.PatchAsJsonAsync(
            "/api/Workers/online-status", new UpdateOnlineStatusDto { OnlineStatus = WorkerOnlineStatus.Online });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var worker = await db.WorkerProfiles.SingleAsync(w => w.UserId == WorkerId);
        Assert.Equal(WorkerOnlineStatus.Online, worker.OnlineStatus);
    }

    [Fact(DisplayName = "[UT-WORKER-ONLINE-API-02] A worker cannot toggle to Online while Busy")]
    public async Task UpdateOnlineStatus_BusyToOnline_Rejected()
    {
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var worker = await db.WorkerProfiles.SingleAsync(w => w.UserId == WorkerId);
            worker.OnlineStatus = WorkerOnlineStatus.Busy;
            await db.SaveChangesAsync();
        }

        using var client = AuthenticatedClient(WorkerId, UserRole.Worker);
        var response = await client.PatchAsJsonAsync(
            "/api/Workers/online-status", new UpdateOnlineStatusDto { OnlineStatus = WorkerOnlineStatus.Online });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "[UT-WORKER-ONLINE-API-03] Unauthenticated calls are rejected")]
    public async Task UpdateOnlineStatus_Unauthenticated_Rejected()
    {
        var response = await fixture.Client.PatchAsJsonAsync(
            "/api/Workers/online-status", new UpdateOnlineStatusDto { OnlineStatus = WorkerOnlineStatus.Online });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "[UT-WORKER-PAYOUT-API-01] A worker can set their payout bank account")]
    public async Task UpdatePayoutAccount_ValidInput_Succeeds()
    {
        using var client = AuthenticatedClient(WorkerId, UserRole.Worker);
        var response = await client.PutAsJsonAsync("/api/Workers/me/payout-account", new UpdatePayoutAccountDto
        {
            BankBin = "970422",
            AccountNumber = "0123456789",
            AccountName = "Nguyen Van A"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var worker = await db.WorkerProfiles.SingleAsync(w => w.UserId == WorkerId);
        Assert.Equal("970422", worker.PayoutBankBin);
        Assert.Equal("0123456789", worker.PayoutBankAccountNumber);
        Assert.Equal("Nguyen Van A", worker.PayoutBankAccountName);
    }

    [Fact(DisplayName = "[UT-WORKER-PAYOUT-API-02] Blank payout bank details are rejected")]
    public async Task UpdatePayoutAccount_BlankInput_Rejected()
    {
        using var client = AuthenticatedClient(WorkerId, UserRole.Worker);
        var response = await client.PutAsJsonAsync("/api/Workers/me/payout-account", new UpdatePayoutAccountDto
        {
            BankBin = "970422",
            AccountNumber = "   ",
            AccountName = "Nguyen Van A"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

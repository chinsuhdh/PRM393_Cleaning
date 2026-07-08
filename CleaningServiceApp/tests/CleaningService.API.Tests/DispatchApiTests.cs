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
public sealed class DispatchApiTests(PostgreSqlApiFixture fixture) : IAsyncLifetime
{
    private static readonly Guid ClientId = Guid.Parse("81000000-0000-0000-0000-000000000001");
    private static readonly Guid OtherClientId = Guid.Parse("81000000-0000-0000-0000-000000000003");
    private static readonly Guid WorkerId = Guid.Parse("81000000-0000-0000-0000-000000000002");
    private static readonly Guid ServiceId = Guid.Parse("82000000-0000-0000-0000-000000000001");
    private static readonly Guid AddressId = Guid.Parse("83000000-0000-0000-0000-000000000001");
    private static readonly Guid EligibleBookingId = Guid.Parse("84000000-0000-0000-0000-000000000001");
    private static readonly Guid OwnBookingId = Guid.Parse("84000000-0000-0000-0000-000000000002");

    public async Task InitializeAsync()
    {
        await fixture.ResetDatabaseAsync();
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        db.Accounts.AddRange(
            BookingApiTestData.Account(ClientId, "dispatch-client@test.local", UserRole.Client, now),
            BookingApiTestData.Account(OtherClientId, "dispatch-other-client@test.local", UserRole.Client, now),
            BookingApiTestData.Account(WorkerId, "dispatch-worker@test.local", UserRole.Worker, now));
        db.Profiles.AddRange(
            BookingApiTestData.Profile(ClientId, "Dispatch Client", now),
            BookingApiTestData.Profile(OtherClientId, "Dispatch Other Client", now),
            BookingApiTestData.Profile(WorkerId, "Dispatch Worker", now));
        db.Services.Add(new Service
        {
            Id = ServiceId, Name = "Dispatch Service", PropertyType = PropertyType.Apartment,
            UnitType = ServiceUnitType.Hour, BasePrice = 100_000, MinimumHours = 2,
            IsActive = true, CreatedAt = now, UpdatedAt = now
        });
        db.UserAddresses.Add(new UserAddress
        {
            Id = AddressId, UserId = ClientId, Label = "Home", AddressText = "District 1",
            Latitude = 10.7769m, Longitude = 106.7009m, PropertyType = PropertyType.Apartment,
            CreatedAt = now, UpdatedAt = now
        });
        db.WorkerProfiles.Add(new WorkerProfile
        {
            UserId = WorkerId, VerificationStatus = "approved", OnlineStatus = WorkerOnlineStatus.Online,
            BaseLatitude = 10.7769m, BaseLongitude = 106.7009m, CurrentLat = 10.7769m,
            CurrentLng = 106.7009m, ServiceRadiusKm = 10, LocationUpdatedAt = now,
            CreatedAt = now, UpdatedAt = now
        });
        db.WorkerServices.Add(new Cleaning.DAL.Entities.WorkerService
        {
            WorkerId = WorkerId, ServiceId = ServiceId, IsVerified = true,
            CreatedAt = now, UpdatedAt = now
        });
        db.Bookings.AddRange(
            Booking(EligibleBookingId, ClientId, now),
            Booking(OwnBookingId, WorkerId, now));
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "[IT-DISPATCH-001-01] Available feed excludes own jobs and permanently honors hide")]
    public async Task AvailableFeed_ExcludesOwnAndHiddenJobs()
    {
        using var worker = AuthenticatedWorker();

        var initialResponse = await worker.GetAsync("/api/Bookings/available");
        var initial = await initialResponse.Content.ReadDataAsync<List<BookingDto>>();
        var visible = Assert.Single(initial!);
        Assert.Equal(EligibleBookingId, visible.Id);

        var hidden = await worker.PostAsync($"/api/Bookings/{EligibleBookingId}/hide", null);
        Assert.Equal(HttpStatusCode.OK, hidden.StatusCode);

        var afterHideResponse = await worker.GetAsync("/api/Bookings/available");
        var afterHide = await afterHideResponse.Content.ReadDataAsync<List<BookingDto>>();
        Assert.Empty(afterHide!);
    }

    [Fact(DisplayName = "[IT-DISPATCH-001-02] Eligible worker can inspect an available booking before accepting")]
    public async Task EligibleWorker_CanInspectAvailableBooking()
    {
        using var worker = AuthenticatedWorker();

        var response = await worker.GetAsync($"/api/Bookings/{EligibleBookingId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var booking = await response.Content.ReadDataAsync<BookingDto>();
        Assert.Equal(EligibleBookingId, booking!.Id);
    }

    [Fact(DisplayName = "[IT-DISPATCH-003-01] Dispatch hub negotiation requires worker authentication")]
    public async Task DispatchHub_NegotiationRequiresAuthentication()
    {
        using var anonymous = fixture.CreateClient();
        var rejected = await anonymous.PostAsync("/hubs/dispatch/negotiate?negotiateVersion=1", null);
        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);

        using var worker = AuthenticatedWorker();
        var accepted = await worker.PostAsync("/hubs/dispatch/negotiate?negotiateVersion=1", null);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
    }

    [Fact(DisplayName = "[UT-BOOK-NEARBY-01] The booking's own client sees an online, in-radius worker's coordinates")]
    public async Task NearbyWorkers_OnlineInRadiusWorker_IsReturned()
    {
        using var client = AuthenticatedClient(ClientId);

        var response = await client.GetAsync($"/api/Bookings/{EligibleBookingId}/nearby-workers");
        var locations = await response.Content.ReadDataAsync<List<NearbyWorkerLocationDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var location = Assert.Single(locations!);
        Assert.Equal(10.7769m, location.Latitude);
        Assert.Equal(106.7009m, location.Longitude);
    }

    [Fact(DisplayName = "[UT-BOOK-NEARBY-02] An offline worker is excluded from the nearby list")]
    public async Task NearbyWorkers_OfflineWorker_Excluded()
    {
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var worker = await db.WorkerProfiles.SingleAsync(w => w.UserId == WorkerId);
            worker.OnlineStatus = WorkerOnlineStatus.Offline;
            await db.SaveChangesAsync();
        }
        using var client = AuthenticatedClient(ClientId);

        var response = await client.GetAsync($"/api/Bookings/{EligibleBookingId}/nearby-workers");
        var locations = await response.Content.ReadDataAsync<List<NearbyWorkerLocationDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(locations!);
    }

    [Fact(DisplayName = "[UT-BOOK-NEARBY-03] The response carries only coordinates, no worker id/name/rating")]
    public async Task NearbyWorkers_ResponseShape_IsAnonymous()
    {
        using var client = AuthenticatedClient(ClientId);

        var response = await client.GetAsync($"/api/Bookings/{EligibleBookingId}/nearby-workers");
        var raw = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("workerId", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fullName", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("averageRating", raw, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("latitude", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "[UT-BOOK-NEARBY-04] A client who does not own this booking gets an empty list, not the other client's data")]
    public async Task NearbyWorkers_NonOwningClient_GetsEmptyList()
    {
        using var client = AuthenticatedClient(OtherClientId);

        var response = await client.GetAsync($"/api/Bookings/{EligibleBookingId}/nearby-workers");
        var locations = await response.Content.ReadDataAsync<List<NearbyWorkerLocationDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(locations!);
    }

    [Fact(DisplayName = "[UT-BOOK-NEARBY-05] A booking that already has a worker assigned (no longer searching) returns an empty list")]
    public async Task NearbyWorkers_AlreadyAssignedBooking_ReturnsEmptyList()
    {
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var booking = await db.Bookings.SingleAsync(b => b.Id == EligibleBookingId);
            booking.WorkerId = WorkerId;
            booking.Status = BookingStatus.Accepted;
            await db.SaveChangesAsync();
        }
        using var client = AuthenticatedClient(ClientId);

        var response = await client.GetAsync($"/api/Bookings/{EligibleBookingId}/nearby-workers");
        var locations = await response.Content.ReadDataAsync<List<NearbyWorkerLocationDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(locations!);
    }

    private HttpClient AuthenticatedWorker()
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(WorkerId, UserRole.Worker));
        return client;
    }

    private HttpClient AuthenticatedClient(Guid accountId)
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(accountId, UserRole.Client));
        return client;
    }

    private static string CreateToken(Guid accountId, UserRole role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            "3YlcESfqMCfUbxi5yDM0lAb7oh6XiOAniuq4Nm50Gjw="));
        var token = new JwtSecurityToken(
            issuer: "CleaningService.Api.Tests", audience: "CleaningService.Api.Tests",
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

    private static Booking Booking(Guid id, Guid clientId, DateTime now) => new()
    {
        Id = id, ClientId = clientId, ServiceId = ServiceId, AddressId = AddressId,
        BookingType = BookingType.Immediate, ScheduledStartTime = now.AddMinutes(15),
        ScheduledEndTime = now.AddHours(2), DurationHours = 2, UnitPrice = 100_000,
        TotalPrice = 200_000, Status = BookingStatus.AwaitingWorker,
        AddressSnapshot = "{}", OptionAnswers = "{}", PricingBreakdown = "{}",
        CreatedAt = now, UpdatedAt = now
    };
}

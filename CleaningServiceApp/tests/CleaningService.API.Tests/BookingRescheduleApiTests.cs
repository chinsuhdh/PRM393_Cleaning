using Cleaning.BLL.Features.Bookings;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Cleaning.DAL.Data;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CleaningService.API.Tests;

[Collection(ApiTestCollection.Name)]
public sealed class BookingRescheduleApiTests(PostgreSqlApiFixture fixture) : IAsyncLifetime
{
    private static readonly Guid ClientId = Guid.Parse("99000000-0000-0000-0000-000000000001");
    private static readonly Guid WorkerId = Guid.Parse("99000000-0000-0000-0000-000000000002");
    private static readonly Guid ServiceId = Guid.Parse("99100000-0000-0000-0000-000000000001");
    private static readonly Guid AddressId = Guid.Parse("99200000-0000-0000-0000-000000000001");
    private static readonly Guid BookingId = Guid.Parse("99300000-0000-0000-0000-000000000001");
    private static readonly DateTime OriginalStart = RoundToSlot(DateTime.UtcNow.AddHours(10));

    public async Task InitializeAsync()
    {
        await fixture.ResetDatabaseAsync();
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        db.Accounts.AddRange(
            BookingApiTestData.Account(ClientId, "rsc-client@test.local", UserRole.Client, now),
            BookingApiTestData.Account(WorkerId, "rsc-worker@test.local", UserRole.Worker, now));
        db.Profiles.AddRange(
            BookingApiTestData.Profile(ClientId, "Rsc Client", now),
            BookingApiTestData.Profile(WorkerId, "Rsc Worker", now));
        db.Services.Add(new Service
        {
            Id = ServiceId,
            Name = "Rsc Service",
            PropertyType = PropertyType.Apartment,
            UnitType = ServiceUnitType.Hour,
            BasePrice = 100_000,
            MinimumHours = 2,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });
        db.UserAddresses.Add(new UserAddress
        {
            Id = AddressId,
            UserId = ClientId,
            Label = "Home",
            AddressText = "District 1",
            Latitude = 10.7769m,
            Longitude = 106.7009m,
            PropertyType = PropertyType.Apartment,
            CreatedAt = now,
            UpdatedAt = now
        });
        db.WorkerProfiles.Add(new WorkerProfile
        {
            UserId = WorkerId,
            VerificationStatus = "approved",
            OnlineStatus = WorkerOnlineStatus.Online,
            BaseLatitude = 10.7769m,
            BaseLongitude = 106.7009m,
            CurrentLat = 10.7769m,
            CurrentLng = 106.7009m,
            ServiceRadiusKm = 10,
            LocationUpdatedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });
        db.WorkerServices.Add(new Cleaning.DAL.Entities.WorkerService
        {
            WorkerId = WorkerId,
            ServiceId = ServiceId,
            IsVerified = true,
            CreatedAt = now,
            UpdatedAt = now
        });
        db.Bookings.Add(new Booking
        {
            Id = BookingId,
            ClientId = ClientId,
            WorkerId = WorkerId,
            ServiceId = ServiceId,
            AddressId = AddressId,
            BookingType = BookingType.Scheduled,
            ScheduledStartTime = OriginalStart,
            ScheduledEndTime = OriginalStart.AddHours(2),
            DurationHours = 2,
            UnitPrice = 100_000,
            TotalPrice = 200_000,
            Status = BookingStatus.Accepted,
            AddressSnapshot = "{}",
            OptionAnswers = "{}",
            PricingBreakdown = "{}",
            CreatedAt = now,
            UpdatedAt = now
        });
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "[IT-BOOK-RSC-01] Full propose-then-accept round trip updates the booking's scheduled time")]
    public async Task ProposeThenAccept_RoundTrip_UpdatesScheduledTime()
    {
        using var client = AuthenticatedClient();
        using var worker = AuthenticatedWorker();
        var newStart = RoundToSlot(DateTime.UtcNow.AddHours(20));

        var proposeResponse = await client.PostAsJsonAsync($"/api/Bookings/{BookingId}/reschedule",
            new { newStartTime = newStart });
        Assert.Equal(HttpStatusCode.OK, proposeResponse.StatusCode);
        var proposed = await proposeResponse.Content.ReadDataAsync<BookingDto>();
        Assert.Equal(nameof(BookingStatus.RescheduleRequested), proposed!.Status);
        var requestId = proposed.PendingReschedule!.Id;

        var acceptResponse = await worker.PatchAsJsonAsync($"/api/Bookings/{BookingId}/reschedule/{requestId}",
            new { action = "Accept" });
        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await db.Bookings.SingleAsync(b => b.Id == BookingId);
        Assert.Equal(BookingStatus.Accepted, booking.Status);
        // Postgres timestamp has microsecond precision vs. .NET's 100ns ticks — compare with a
        // small tolerance rather than exact equality across the round trip.
        Assert.True(Math.Abs((booking.ScheduledStartTime - newStart).TotalMilliseconds) < 1);
    }

    [Fact(DisplayName = "[IT-BOOK-RSC-02] A too-soon reschedule time is rejected server-side even bypassing the Flutter picker")]
    public async Task ProposeReschedule_TooSoon_RejectedServerSide()
    {
        using var client = AuthenticatedClient();

        var response = await client.PostAsJsonAsync($"/api/Bookings/{BookingId}/reschedule",
            new { newStartTime = DateTime.UtcNow.AddMinutes(30) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await db.Bookings.SingleAsync(b => b.Id == BookingId);
        Assert.Equal(BookingStatus.Accepted, booking.Status);
    }

    private static DateTime RoundToSlot(DateTime value) =>
        value.AddMinutes(-(value.Minute % 30)).AddSeconds(-value.Second).AddMilliseconds(-value.Millisecond);

    private HttpClient AuthenticatedWorker()
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(WorkerId, UserRole.Worker));
        return client;
    }

    private HttpClient AuthenticatedClient()
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(ClientId, UserRole.Client));
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
}

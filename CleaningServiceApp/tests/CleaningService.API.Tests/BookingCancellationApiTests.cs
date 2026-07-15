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
public sealed class BookingCancellationApiTests(PostgreSqlApiFixture fixture) : IAsyncLifetime
{
    private static readonly Guid ClientId = Guid.Parse("91000000-0000-0000-0000-000000000001");
    private static readonly Guid WorkerId = Guid.Parse("91000000-0000-0000-0000-000000000002");
    private static readonly Guid ServiceId = Guid.Parse("92000000-0000-0000-0000-000000000001");
    private static readonly Guid AddressId = Guid.Parse("93000000-0000-0000-0000-000000000001");

    public async Task InitializeAsync()
    {
        await fixture.ResetDatabaseAsync();
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        db.Accounts.AddRange(
            BookingApiTestData.Account(ClientId, "cxl-client@test.local", UserRole.Client, now),
            BookingApiTestData.Account(WorkerId, "cxl-worker@test.local", UserRole.Worker, now));
        db.Profiles.AddRange(
            BookingApiTestData.Profile(ClientId, "Cxl Client", now),
            BookingApiTestData.Profile(WorkerId, "Cxl Worker", now));
        db.Services.Add(new Service
        {
            Id = ServiceId,
            Name = "Cxl Service",
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
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "[IT-BOOK-CXL-01] Client cancel succeeds pre-accept")]
    public async Task ClientCancel_AwaitingWorker_Succeeds()
    {
        var bookingId = await SeedBookingAsync(BookingStatus.AwaitingWorker);
        using var client = AuthenticatedClient(ClientId);

        var response = await client.PostAsync($"/api/Bookings/{bookingId}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await db.Bookings.SingleAsync(b => b.Id == bookingId);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }

    [Fact(DisplayName = "[IT-BOOK-CXL-02] Client cancel on an already-accepted booking is rejected")]
    public async Task ClientCancel_AlreadyAccepted_Rejected()
    {
        var bookingId = await SeedBookingAsync(BookingStatus.Accepted, workerId: WorkerId);
        using var client = AuthenticatedClient(ClientId);

        var response = await client.PostAsync($"/api/Bookings/{bookingId}/cancel", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var envelope = await response.Content.ReadEnvelopeAsync();
        Assert.Equal("BOOKING_CANCEL_NOT_ALLOWED", envelope.GetProperty("errorCode").GetString());
    }

    [Fact(DisplayName = "[IT-BOOK-CXL-03] An accept-vs-cancel race against real Postgres has exactly one winner")]
    public async Task AcceptAndCancel_ConcurrentRace_ExactlyOneWinner()
    {
        var bookingId = await SeedBookingAsync(BookingStatus.AwaitingWorker);
        using var client = AuthenticatedClient(ClientId);
        using var worker = AuthenticatedWorker();

        var acceptTask = worker.PatchAsync($"/api/Bookings/{bookingId}/accept", null);
        var cancelTask = client.PostAsync($"/api/Bookings/{bookingId}/cancel", null);
        await Task.WhenAll(acceptTask, cancelTask);

        var outcomes = new[] { (await acceptTask).StatusCode, (await cancelTask).StatusCode };
        Assert.Single(outcomes, code => code == HttpStatusCode.OK);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await db.Bookings.SingleAsync(b => b.Id == bookingId);
        Assert.True(booking.Status is BookingStatus.Accepted or BookingStatus.Cancelled);
    }

    [Fact(DisplayName = "[IT-BOOK-CXL-04] Worker plain-cancel releases the job and requires a reason")]
    public async Task WorkerCancel_ReleasesJob()
    {
        var bookingId = await SeedBookingAsync(BookingStatus.Accepted, workerId: WorkerId);
        using var worker = AuthenticatedWorker();

        var response = await worker.PostAsJsonAsync($"/api/Bookings/{bookingId}/worker-cancel",
            new { reasonCode = "worker_cancel.too_far" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await db.Bookings.SingleAsync(b => b.Id == bookingId);
        Assert.Equal(BookingStatus.AwaitingWorker, booking.Status);
        Assert.Null(booking.WorkerId);
    }

    [Fact(DisplayName = "[IT-BOOK-CXL-05] The same booking can be worker-cancelled twice across a re-accept cycle " +
        "(Slice 0 regression: booking_cancellations no longer has a unique index on booking_id)")]
    public async Task WorkerCancel_TwiceOnSameBooking_BothRecordsPersist()
    {
        var bookingId = await SeedBookingAsync(BookingStatus.Accepted, workerId: WorkerId);
        using var worker = AuthenticatedWorker();

        var first = await worker.PostAsJsonAsync($"/api/Bookings/{bookingId}/worker-cancel",
            new { reasonCode = "worker_cancel.too_far" });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var accepted = await worker.PatchAsync($"/api/Bookings/{bookingId}/accept", null);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);

        var second = await worker.PostAsJsonAsync($"/api/Bookings/{bookingId}/worker-cancel",
            new { reasonCode = "worker_cancel.schedule_conflict" });
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var records = await db.BookingCancellations.Where(c => c.BookingId == bookingId).ToListAsync();
        Assert.Equal(2, records.Count);
    }

    [Fact(DisplayName = "[IT-BOOK-CXL-06] An unrecognized worker-cancel reason code is rejected")]
    public async Task WorkerCancel_InvalidReasonCode_Rejected()
    {
        var bookingId = await SeedBookingAsync(BookingStatus.Accepted, workerId: WorkerId);
        using var worker = AuthenticatedWorker();

        var response = await worker.PostAsJsonAsync($"/api/Bookings/{bookingId}/worker-cancel",
            new { reasonCode = "not_a_real_code" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "[IT-BOOK-CXL-07] A 3rd plain-cancel suspends the worker and blocks going back Online")]
    public async Task WorkerCancel_ThirdStrike_BlocksGoOnline()
    {
        using var worker = AuthenticatedWorker();
        for (var i = 0; i < 3; i++)
        {
            var bookingId = await SeedBookingAsync(BookingStatus.Accepted, workerId: WorkerId, idSuffix: i + 1);
            var response = await worker.PostAsJsonAsync($"/api/Bookings/{bookingId}/worker-cancel",
                new { reasonCode = "worker_cancel.too_far" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var goOnline = await worker.PatchAsJsonAsync("/api/Workers/online-status",
            new { onlineStatus = "Online" });

        Assert.Equal(HttpStatusCode.Forbidden, goOnline.StatusCode);
        var envelope = await goOnline.Content.ReadEnvelopeAsync();
        Assert.Equal("WORKER_SUSPENDED", envelope.GetProperty("errorCode").GetString());
    }

    private async Task<Guid> SeedBookingAsync(BookingStatus status, Guid? workerId = null, int idSuffix = 1)
    {
        var bookingId = Guid.Parse($"94000000-0000-0000-0000-{idSuffix:D12}");
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        db.Bookings.Add(new Booking
        {
            Id = bookingId,
            ClientId = ClientId,
            WorkerId = workerId,
            ServiceId = ServiceId,
            AddressId = AddressId,
            BookingType = BookingType.Scheduled,
            ScheduledStartTime = now.AddHours(5),
            ScheduledEndTime = now.AddHours(7),
            DurationHours = 2,
            UnitPrice = 100_000,
            TotalPrice = 200_000,
            Status = status,
            AddressSnapshot = "{}",
            OptionAnswers = "{}",
            PricingBreakdown = "{}",
            CreatedAt = now,
            UpdatedAt = now
        });
        await db.SaveChangesAsync();
        return bookingId;
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
}

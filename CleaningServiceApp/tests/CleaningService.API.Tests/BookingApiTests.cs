using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Cleaning.BLL.DTOs;
using Cleaning.DAL.Data;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CleaningService.API.Tests;

[Collection(ApiTestCollection.Name)]
public sealed class BookingApiTests(PostgreSqlApiFixture fixture) : IAsyncLifetime
{
    private static readonly Guid ClientId = Guid.Parse("71000000-0000-0000-0000-000000000001");
    private static readonly Guid OtherClientId = Guid.Parse("71000000-0000-0000-0000-000000000002");
    private static readonly Guid WorkerId = Guid.Parse("71000000-0000-0000-0000-000000000003");
    private static readonly Guid ServiceId = Guid.Parse("72000000-0000-0000-0000-000000000001");
    private static readonly Guid AddressId = Guid.Parse("73000000-0000-0000-0000-000000000001");

    public async Task InitializeAsync()
    {
        await fixture.ResetDatabaseAsync();
        await SeedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "[IT-BOOK-001-01] Scheduled happy path persists booking and status records")]
    public async Task ScheduledHappyPath_PersistsExpectedRecords()
    {
        using var client = AuthenticatedClient(ClientId, UserRole.Client);
        var start = DateTime.UtcNow.Date.AddDays(1).AddHours(2);
        var availability = await client.PostAsJsonAsync("/api/Bookings/availability",
            AvailabilityRequest(start));
        var available = await availability.Content.ReadFromJsonAsync<BookingAvailabilityDto>();
        Assert.Equal(HttpStatusCode.OK, availability.StatusCode);
        Assert.NotNull(available);
        Assert.Single(available.Slots);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Bookings");
        request.Headers.Add("Idempotency-Key", "it-book-001-happy");
        request.Content = JsonContent.Create(CreateRequest(start));
        var response = await client.SendAsync(request);
        var booking = await response.Content.ReadFromJsonAsync<BookingDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(booking);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await db.Bookings.SingleAsync(item => item.Id == booking.Id);
        Assert.Equal(BookingType.Scheduled, persisted.BookingType);
        Assert.Equal("it-book-001-happy", persisted.IdempotencyKey);
        Assert.Equal(BookingStatus.PendingPayment, persisted.Status);
        Assert.True(await db.BookingStatusLogs.AnyAsync(log => log.BookingId == booking.Id));
    }

    [Fact(DisplayName = "[IT-FE-BOOK-001-01] Immediate booking is persisted and queued for dispatch")]
    public async Task ImmediateHappyPath_PersistsAndQueuesDispatch()
    {
        using var client = AuthenticatedClient(ClientId, UserRole.Client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Bookings");
        request.Headers.Add("Idempotency-Key", "it-book-001-immediate");
        request.Content = JsonContent.Create(new CreateBookingDto
        {
            ServiceId = ServiceId,
            AddressId = AddressId,
            BookingType = BookingType.Immediate,
            ScheduledStartTime = null,
            DurationHours = 2
        });

        var response = await client.SendAsync(request);
        var booking = await response.Content.ReadFromJsonAsync<BookingDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(booking);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await db.Bookings.SingleAsync(item => item.Id == booking.Id);
        Assert.Equal(BookingType.Immediate, persisted.BookingType);
        Assert.Equal(BookingStatus.AwaitingWorker, persisted.Status);
        Assert.True(persisted.ScheduledStartTime >= DateTime.UtcNow.AddMinutes(14));
    }

    [Fact(DisplayName = "[IT-FE-BOOK-001-03] Immediate booking is created even when no worker is online, ready for dispatch")]
    public async Task ImmediateWithoutOnlineWorker_StillCreatesAwaitingWorker()
    {
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var worker = await db.WorkerProfiles.SingleAsync(item => item.UserId == WorkerId);
            worker.OnlineStatus = WorkerOnlineStatus.Offline;
            await db.SaveChangesAsync();
        }

        using var client = AuthenticatedClient(ClientId, UserRole.Client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Bookings");
        request.Headers.Add("Idempotency-Key", "it-book-001-no-worker");
        request.Content = JsonContent.Create(new CreateBookingDto
        {
            ServiceId = ServiceId,
            AddressId = AddressId,
            BookingType = BookingType.Immediate,
            DurationHours = 2
        });

        var response = await client.SendAsync(request);
        var booking = await response.Content.ReadFromJsonAsync<BookingDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(booking);
        await using var scope2 = fixture.Services.CreateAsyncScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await db2.Bookings.SingleAsync(item => item.Id == booking!.Id);
        Assert.Equal(BookingStatus.AwaitingWorker, persisted.Status);
    }

    [Fact(DisplayName = "[IT-BOOK-001-02] Unauthenticated, wrong-role, and non-owner calls are rejected")]
    public async Task DirectCalls_EnforceAuthenticationRoleAndOwnership()
    {
        var unauthenticated = await fixture.Client.PostAsJsonAsync(
            "/api/Bookings/availability", AvailabilityRequest(DateTime.UtcNow.AddDays(1)));
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);

        using var worker = AuthenticatedClient(WorkerId, UserRole.Worker);
        var wrongRole = await worker.PostAsJsonAsync(
            "/api/Bookings/availability", AvailabilityRequest(DateTime.UtcNow.AddDays(1)));
        Assert.Equal(HttpStatusCode.Forbidden, wrongRole.StatusCode);

        using var otherClient = AuthenticatedClient(OtherClientId, UserRole.Client);
        var wrongOwner = await otherClient.PostAsJsonAsync(
            "/api/Bookings/availability", AvailabilityRequest(DateTime.UtcNow.AddDays(1)));
        Assert.Equal(HttpStatusCode.Forbidden, wrongOwner.StatusCode);
    }

    [Fact(DisplayName = "[IT-BOOK-001-03] Retries, stale requests, and failed writes remain consistent")]
    public async Task RetryStaleAndFailedRequests_DoNotCreateDuplicates()
    {
        using var client = AuthenticatedClient(ClientId, UserRole.Client);
        var start = DateTime.UtcNow.Date.AddDays(1).AddHours(3);
        async Task<HttpResponseMessage> CreateAsync(string key, CreateBookingDto body)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/Bookings");
            request.Headers.Add("Idempotency-Key", key);
            request.Content = JsonContent.Create(body);
            return await client.SendAsync(request);
        }

        var first = await CreateAsync("it-book-001-retry", CreateRequest(start));
        var retry = await CreateAsync("it-book-001-retry", CreateRequest(start));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        var firstBooking = await first.Content.ReadFromJsonAsync<BookingDto>();
        var retryBooking = await retry.Content.ReadFromJsonAsync<BookingDto>();
        Assert.Equal(firstBooking!.Id, retryBooking!.Id);

        // New flow: no worker slot is reserved at create time, so two bookings for the same time both
        // succeed (they are matched to workers independently by dispatch afterwards).
        var concurrentStart = start.AddHours(3);
        var concurrent = await Task.WhenAll(
            CreateAsync("it-book-001-concurrent-a", CreateRequest(concurrentStart)),
            CreateAsync("it-book-001-concurrent-b", CreateRequest(concurrentStart)));
        Assert.All(concurrent, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));

        var invalid = CreateRequest(start.AddHours(1));
        invalid.AddressId = Guid.NewGuid();
        var failed = await CreateAsync("it-book-001-failed", invalid);
        Assert.Equal(HttpStatusCode.Forbidden, failed.StatusCode);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // One booking from the idempotent retry pair + two from the concurrent pair = 3.
        Assert.Equal(3, await db.Bookings.CountAsync());
        Assert.False(await db.Bookings.AnyAsync(item => item.IdempotencyKey == "it-book-001-failed"));

        await db.Database.ExecuteSqlRawAsync(
            "CREATE OR REPLACE FUNCTION fail_booking_log() RETURNS trigger AS 'BEGIN RAISE EXCEPTION ''forced rollback''; END;' LANGUAGE plpgsql;");
        await db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER fail_booking_log_trigger BEFORE INSERT ON booking_status_logs FOR EACH ROW EXECUTE FUNCTION fail_booking_log();");
        try
        {
            var transactionFailure = await CreateAsync(
                "it-book-001-rollback",
                CreateRequest(start.AddHours(7)));
            Assert.Equal(HttpStatusCode.InternalServerError, transactionFailure.StatusCode);
            var error = JsonDocument.Parse(await transactionFailure.Content.ReadAsStringAsync()).RootElement;
            Assert.Equal("BOOKING_CREATE_FAILED", error.GetProperty("code").GetString());
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync(
                "DROP TRIGGER IF EXISTS fail_booking_log_trigger ON booking_status_logs; DROP FUNCTION IF EXISTS fail_booking_log();");
        }
        db.ChangeTracker.Clear();
        Assert.False(await db.Bookings.AnyAsync(item => item.IdempotencyKey == "it-book-001-rollback"));

        db.WorkerAvailabilities.RemoveRange(db.WorkerAvailabilities);
        await db.SaveChangesAsync();
        var stale = await client.PostAsJsonAsync("/api/Bookings/availability",
            AvailabilityRequest(start.AddHours(2)));
        var staleBody = await stale.Content.ReadFromJsonAsync<BookingAvailabilityDto>();
        Assert.Equal(HttpStatusCode.OK, stale.StatusCode);
        Assert.Empty(staleBody!.Slots);
        Assert.Equal("BOOKING_NO_AVAILABLE_WORKER", staleBody.EmptyReasonCode);
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

    private static BookingAvailabilityRequestDto AvailabilityRequest(DateTime start) => new()
    {
        ServiceId = ServiceId,
        AddressId = AddressId,
        BookingType = BookingType.Scheduled,
        DurationHours = 2,
        From = start,
        To = start
    };

    private static CreateBookingDto CreateRequest(DateTime start) => new()
    {
        ServiceId = ServiceId,
        AddressId = AddressId,
        BookingType = BookingType.Scheduled,
        ScheduledStartTime = start,
        DurationHours = 2
    };

    private async Task SeedAsync()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        db.Accounts.AddRange(
            BookingApiTestData.Account(ClientId, "book-client@cleanai.test", UserRole.Client, now),
            BookingApiTestData.Account(OtherClientId, "book-other@cleanai.test", UserRole.Client, now),
            BookingApiTestData.Account(WorkerId, "book-worker@cleanai.test", UserRole.Worker, now));
        db.Profiles.AddRange(
            BookingApiTestData.Profile(ClientId, "Booking Client", now),
            BookingApiTestData.Profile(OtherClientId, "Other Client", now),
            BookingApiTestData.Profile(WorkerId, "Booking Worker", now));
        db.Services.Add(new Service
        {
            Id = ServiceId, Name = "Booking Test Service", PropertyType = PropertyType.Apartment,
            UnitType = ServiceUnitType.Hour, BasePrice = 100_000, MinimumHours = 2,
            IsActive = true, CreatedAt = now, UpdatedAt = now
        });
        db.UserAddresses.Add(new UserAddress
        {
            Id = AddressId, UserId = ClientId, Label = "Home", AddressText = "Test address",
            PropertyType = PropertyType.Apartment, CreatedAt = now, UpdatedAt = now
        });
        db.WorkerProfiles.Add(new WorkerProfile
        {
            UserId = WorkerId, OnlineStatus = WorkerOnlineStatus.Online,
            ImmediateBookingEnabled = true, VerificationStatus = "approved",
            LocationUpdatedAt = now,
            CreatedAt = now, UpdatedAt = now
        });
        db.WorkerServices.Add(new Cleaning.DAL.Entities.WorkerService
        {
            WorkerId = WorkerId, ServiceId = ServiceId, IsVerified = true,
            CreatedAt = now, UpdatedAt = now
        });
        db.WorkerAvailabilities.Add(new WorkerAvailability
        {
            Id = Guid.NewGuid(), WorkerId = WorkerId,
            StartTime = now.Date.AddDays(1), EndTime = now.Date.AddDays(2),
            Status = AvailabilityStatus.Available, CreatedAt = now, UpdatedAt = now
        });
        await db.SaveChangesAsync();
    }

}


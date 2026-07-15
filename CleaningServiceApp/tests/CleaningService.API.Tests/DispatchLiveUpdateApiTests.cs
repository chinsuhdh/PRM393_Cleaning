using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Cleaning.DAL.Data;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CleaningService.API.Tests;

/// H.1/H.2/H.3: end-to-end proof that the new cancel/report/reschedule endpoints actually push
/// "bookingStatusChanged" over the real SignalR hub to a subscriber on the booking:{id} group —
/// not just that the DB row changed, which the other API tests already cover.
[Collection(ApiTestCollection.Name)]
public sealed class DispatchLiveUpdateApiTests(PostgreSqlApiFixture fixture) : IAsyncLifetime
{
    private static readonly Guid ClientId = Guid.Parse("9b000000-0000-0000-0000-000000000001");
    private static readonly Guid WorkerId = Guid.Parse("9b000000-0000-0000-0000-000000000002");
    private static readonly Guid ServiceId = Guid.Parse("9b100000-0000-0000-0000-000000000001");
    private static readonly Guid AddressId = Guid.Parse("9b200000-0000-0000-0000-000000000001");
    private static readonly Guid BookingId = Guid.Parse("9b300000-0000-0000-0000-000000000001");

    public async Task InitializeAsync()
    {
        await fixture.ResetDatabaseAsync();
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        db.Accounts.AddRange(
            BookingApiTestData.Account(ClientId, "live-client@test.local", UserRole.Client, now),
            BookingApiTestData.Account(WorkerId, "live-worker@test.local", UserRole.Worker, now));
        db.Profiles.AddRange(
            BookingApiTestData.Profile(ClientId, "Live Client", now),
            BookingApiTestData.Profile(WorkerId, "Live Worker", now));
        db.Services.Add(new Service
        {
            Id = ServiceId,
            Name = "Live Service",
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
            ScheduledStartTime = now.AddHours(10),
            ScheduledEndTime = now.AddHours(12),
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

    [Fact(DisplayName = "[IT-LIVE-01] Worker plain-cancel pushes bookingStatusChanged=AwaitingWorker to a client subscribed on the booking group")]
    public async Task WorkerCancel_PushesBookingStatusChanged_ToSubscribedClient()
    {
        await using var connection = BuildHubConnection(ClientId, UserRole.Client);
        var received = new TaskCompletionSource<(Guid BookingId, string Status)>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<Guid, string>("bookingStatusChanged", (id, status) => received.TrySetResult((id, status)));

        await connection.StartAsync();
        await connection.InvokeAsync("SubscribeBooking", BookingId);

        using var worker = AuthenticatedClient(WorkerId, UserRole.Worker);
        var response = await worker.PostAsJsonAsync($"/api/Bookings/{BookingId}/worker-cancel",
            new { reasonCode = "worker_cancel.too_far" });
        response.EnsureSuccessStatusCode();

        var result = await WaitWithTimeout(received.Task);

        Assert.Equal(BookingId, result.BookingId);
        Assert.Equal(nameof(BookingStatus.AwaitingWorker), result.Status);
    }

    [Fact(DisplayName = "[IT-LIVE-02] Proposing a reschedule pushes bookingStatusChanged=RescheduleRequested to the worker")]
    public async Task ProposeReschedule_PushesBookingStatusChanged_ToWorker()
    {
        await using var connection = BuildHubConnection(WorkerId, UserRole.Worker);
        var received = new TaskCompletionSource<(Guid BookingId, string Status)>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<Guid, string>("bookingStatusChanged", (id, status) => received.TrySetResult((id, status)));

        await connection.StartAsync();
        await connection.InvokeAsync("SubscribeBooking", BookingId);

        using var client = AuthenticatedClient(ClientId, UserRole.Client);
        var newStart = DateTime.UtcNow.AddHours(20);
        newStart = newStart.AddMinutes(-(newStart.Minute % 30)).AddSeconds(-newStart.Second).AddMilliseconds(-newStart.Millisecond);
        var response = await client.PostAsJsonAsync($"/api/Bookings/{BookingId}/reschedule",
            new { newStartTime = newStart });
        response.EnsureSuccessStatusCode();

        var result = await WaitWithTimeout(received.Task);

        Assert.Equal(BookingId, result.BookingId);
        Assert.Equal(nameof(BookingStatus.RescheduleRequested), result.Status);
    }

    [Fact(DisplayName = "[IT-LIVE-03] Client cancel (pre-accept) pushes bookingStatusChanged=Cancelled to a subscribed worker")]
    public async Task ClientCancel_PushesBookingStatusChanged_ToWorker()
    {
        var awaitingBookingId = Guid.Parse("9b300000-0000-0000-0000-000000000002");
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTime.UtcNow;
            db.Bookings.Add(new Booking
            {
                Id = awaitingBookingId,
                ClientId = ClientId,
                ServiceId = ServiceId,
                AddressId = AddressId,
                BookingType = BookingType.Scheduled,
                // Deliberately non-overlapping with the Accepted booking seeded in InitializeAsync
                // (also assigned to WorkerId, 10h-12h out) — otherwise HasScheduleConflictAsync
                // makes this worker ineligible and SubscribeBooking's participant/eligibility check fails.
                ScheduledStartTime = now.AddHours(20),
                ScheduledEndTime = now.AddHours(22),
                DurationHours = 2,
                UnitPrice = 100_000,
                TotalPrice = 200_000,
                Status = BookingStatus.AwaitingWorker,
                AddressSnapshot = "{}",
                OptionAnswers = "{}",
                PricingBreakdown = "{}",
                CreatedAt = now,
                UpdatedAt = now
            });
            await db.SaveChangesAsync();
        }

        await using var connection = BuildHubConnection(WorkerId, UserRole.Worker);
        var received = new TaskCompletionSource<(Guid BookingId, string Status)>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<Guid, string>("bookingStatusChanged", (id, status) => received.TrySetResult((id, status)));

        await connection.StartAsync();
        await connection.InvokeAsync("SubscribeBooking", awaitingBookingId);

        using var client = AuthenticatedClient(ClientId, UserRole.Client);
        var response = await client.PostAsync($"/api/Bookings/{awaitingBookingId}/cancel", null);
        response.EnsureSuccessStatusCode();

        var result = await WaitWithTimeout(received.Task);

        Assert.Equal(awaitingBookingId, result.BookingId);
        Assert.Equal(nameof(BookingStatus.Cancelled), result.Status);
    }

    private static async Task<T> WaitWithTimeout<T>(Task<T> task)
    {
        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10)));
        Assert.True(ReferenceEquals(completed, task), "Timed out waiting for the SignalR push.");
        return await task;
    }

    private HubConnection BuildHubConnection(Guid accountId, UserRole role)
    {
        var token = CreateToken(accountId, role);
        return new HubConnectionBuilder()
            .WithUrl($"{fixture.Server.BaseAddress}hubs/dispatch", options =>
            {
                options.HttpMessageHandlerFactory = _ => fixture.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();
    }

    private HttpClient AuthenticatedClient(Guid accountId, UserRole role)
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CreateToken(accountId, role));
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

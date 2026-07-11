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
public sealed class BookingReportApiTests(PostgreSqlApiFixture fixture) : IAsyncLifetime
{
    private static readonly Guid ClientId = Guid.Parse("95000000-0000-0000-0000-000000000001");
    private static readonly Guid WorkerId = Guid.Parse("95000000-0000-0000-0000-000000000002");
    private static readonly Guid ServiceId = Guid.Parse("96000000-0000-0000-0000-000000000001");
    private static readonly Guid AddressId = Guid.Parse("97000000-0000-0000-0000-000000000001");
    private static readonly Guid BookingId = Guid.Parse("98000000-0000-0000-0000-000000000001");

    public async Task InitializeAsync()
    {
        await fixture.ResetDatabaseAsync();
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        db.Accounts.AddRange(
            BookingApiTestData.Account(ClientId, "rpt-client@test.local", UserRole.Client, now),
            BookingApiTestData.Account(WorkerId, "rpt-worker@test.local", UserRole.Worker, now));
        db.Profiles.AddRange(
            BookingApiTestData.Profile(ClientId, "Rpt Client", now),
            BookingApiTestData.Profile(WorkerId, "Rpt Worker", now));
        db.Services.Add(new Service
        {
            Id = ServiceId,
            Name = "Rpt Service",
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
            OnlineStatus = WorkerOnlineStatus.Busy,
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
            ScheduledStartTime = now.AddHours(5),
            ScheduledEndTime = now.AddHours(7),
            DurationHours = 2,
            UnitPrice = 100_000,
            TotalPrice = 200_000,
            Status = BookingStatus.InProgress,
            AddressSnapshot = "{}",
            OptionAnswers = "{}",
            PricingBreakdown = "{}",
            CreatedAt = now,
            UpdatedAt = now
        });
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "[IT-BOOK-RPT-01] A client report cancels the booking and releases the assigned worker's Busy state")]
    public async Task ClientReport_CancelsBookingAndReleasesWorker()
    {
        using var client = AuthenticatedClient();

        var response = await client.PostAsJsonAsync($"/api/Bookings/{BookingId}/report", new
        {
            reasonCode = "report.client.worker_no_show",
            freeText = "Nhan vien khong den dung gio va khong lien lac"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await db.Bookings.SingleAsync(b => b.Id == BookingId);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        var worker = await db.WorkerProfiles.SingleAsync(w => w.UserId == WorkerId);
        Assert.Equal(WorkerOnlineStatus.Online, worker.OnlineStatus);
    }

    [Fact(DisplayName = "[IT-BOOK-RPT-02] Free text under 20 characters is rejected with a 400")]
    public async Task Report_FreeTextTooShort_Rejected()
    {
        using var client = AuthenticatedClient();

        var response = await client.PostAsJsonAsync($"/api/Bookings/{BookingId}/report", new
        {
            reasonCode = "report.client.other",
            freeText = "qua ngan"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await db.Bookings.SingleAsync(b => b.Id == BookingId);
        Assert.Equal(BookingStatus.InProgress, booking.Status);
    }

    [Fact(DisplayName = "[IT-BOOK-RPT-03] A worker report against a client-only reason code is rejected")]
    public async Task WorkerReport_UsingClientReasonCode_Rejected()
    {
        using var worker = AuthenticatedWorker();

        var response = await worker.PostAsJsonAsync($"/api/Bookings/{BookingId}/report", new
        {
            reasonCode = "report.client.worker_no_show",
            freeText = "Khach hang khong co mat tai dia diem"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var envelope = await response.Content.ReadEnvelopeAsync();
        Assert.Equal("REPORT_REASON_INVALID", envelope.GetProperty("errorCode").GetString());
    }

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

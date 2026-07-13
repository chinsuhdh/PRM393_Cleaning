using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Data;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace CleaningService.API.Tests;

[Collection(ApiTestCollection.Name)]
public sealed class PaymentApiTests(PostgreSqlApiFixture fixture) : IAsyncLifetime
{
    private static readonly Guid ClientId = Guid.Parse("9c000000-0000-0000-0000-000000000001");
    private static readonly Guid WorkerId = Guid.Parse("9c000000-0000-0000-0000-000000000002");
    private static readonly Guid ServiceId = Guid.Parse("9c100000-0000-0000-0000-000000000001");
    private static readonly Guid AddressId = Guid.Parse("9c200000-0000-0000-0000-000000000001");
    private static readonly Guid BookingId = Guid.Parse("9c300000-0000-0000-0000-000000000001");

    private readonly FakePayOsCheckoutService _fakeCheckoutService = new();
    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await fixture.ResetDatabaseAsync();
        _factory = fixture.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPayOsCheckoutService>();
                services.AddSingleton<IPayOsCheckoutService>(_fakeCheckoutService);
            }));

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        db.Accounts.AddRange(
            BookingApiTestData.Account(ClientId, "pay-client@test.local", UserRole.Client, now),
            BookingApiTestData.Account(WorkerId, "pay-worker@test.local", UserRole.Worker, now));
        db.Profiles.AddRange(
            BookingApiTestData.Profile(ClientId, "Pay Client", now),
            BookingApiTestData.Profile(WorkerId, "Pay Worker", now));
        db.Services.Add(new Service
        {
            Id = ServiceId,
            Name = "Pay Service",
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
            Status = BookingStatus.PendingPayment,
            PaymentMethod = PaymentMethod.Payos,
            AddressSnapshot = "{}",
            OptionAnswers = "{}",
            PricingBreakdown = "{}",
            CreatedAt = now,
            UpdatedAt = now
        });
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "[IT-PAY-01] Pay-now trả về URL thanh toán payOS hợp lệ")]
    public async Task PayNow_HappyPath_ReturnsCheckoutUrl()
    {
        using var client = AuthenticatedClient(ClientId, UserRole.Client);

        var response = await client.PostAsJsonAsync("/api/Payments", new { bookingId = BookingId });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadDataAsync<PayNowResponseDto>();
        Assert.StartsWith("https://pay.payos.vn/web/", result!.PaymentUrl);
    }

    [Fact(DisplayName = "[IT-PAY-02] Webhook hợp lệ hoàn tất đơn và ghi nhận thu nhập")]
    public async Task PayOsWebhook_Valid_CompletesBookingAndWritesEarning()
    {
        using var client = AuthenticatedClient(ClientId, UserRole.Client);
        var payResponse = await client.PostAsJsonAsync("/api/Payments", new { bookingId = BookingId });
        await payResponse.Content.ReadDataAsync<PayNowResponseDto>();

        using var anonymous = _factory.CreateClient();
        var webhookBody = JsonSerializer.Serialize(new
        {
            orderCode = _fakeCheckoutService.LastOrderCode,
            amount = 200_000m,
            success = true
        });
        var webhookResponse = await anonymous.PostAsync(
            "/api/Payments/payos-webhook", new StringContent(webhookBody, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, webhookResponse.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await db.Bookings.SingleAsync(b => b.Id == BookingId);
        Assert.Equal(BookingStatus.Completed, booking.Status);
        var earning = await db.WorkerEarnings.SingleAsync(e => e.BookingId == BookingId);
        Assert.Equal("pending", earning.Status);
    }

    [Fact(DisplayName = "[IT-PAY-03] Webhook không xác thực được chữ ký bị từ chối và không thay đổi trạng thái đơn")]
    public async Task PayOsWebhook_InvalidSignature_RejectedAndBookingUntouched()
    {
        using var client = AuthenticatedClient(ClientId, UserRole.Client);
        var payResponse = await client.PostAsJsonAsync("/api/Payments", new { bookingId = BookingId });
        await payResponse.Content.ReadDataAsync<PayNowResponseDto>();

        using var anonymous = _factory.CreateClient();
        var webhookResponse = await anonymous.PostAsync(
            "/api/Payments/payos-webhook", new StringContent(FakePayOsCheckoutService.InvalidSignaturePayload, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, webhookResponse.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await db.Bookings.SingleAsync(b => b.Id == BookingId);
        Assert.Equal(BookingStatus.PendingPayment, booking.Status);
    }

    private HttpClient AuthenticatedClient(Guid accountId, UserRole role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(accountId, role));
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

internal sealed class FakePayOsCheckoutService : IPayOsCheckoutService
{
    public const string InvalidSignaturePayload = "invalid";

    public long? LastOrderCode { get; private set; }

    public Task<PayOsCheckoutLink> CreatePaymentLinkAsync(decimal amount, string description, CancellationToken ct = default)
    {
        var orderCode = Random.Shared.NextInt64(1, long.MaxValue);
        LastOrderCode = orderCode;
        return Task.FromResult(new PayOsCheckoutLink(orderCode, $"https://pay.payos.vn/web/{orderCode}"));
    }

    public Task<PayOsWebhookResult?> VerifyWebhookAsync(string rawJson, CancellationToken ct = default)
    {
        if (rawJson == InvalidSignaturePayload)
            return Task.FromResult<PayOsWebhookResult?>(null);

        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(rawJson)!;
        return Task.FromResult<PayOsWebhookResult?>(new PayOsWebhookResult(
            payload["success"].GetBoolean(),
            payload["orderCode"].GetInt64(),
            payload["amount"].GetDecimal(),
            "ref-test"));
    }
}

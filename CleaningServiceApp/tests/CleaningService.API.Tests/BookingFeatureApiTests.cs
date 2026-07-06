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

// End-to-end coverage for BOOK-002 (service-defined questions) and BOOK-004 (server-calculated pricing).
[Collection(ApiTestCollection.Name)]
public sealed class BookingFeatureApiTests(PostgreSqlApiFixture fixture) : IAsyncLifetime
{
    private static readonly Guid ClientId = Guid.Parse("74000000-0000-0000-0000-000000000001");
    private static readonly Guid WorkerId = Guid.Parse("74000000-0000-0000-0000-000000000003");
    private static readonly Guid ServiceId = Guid.Parse("75000000-0000-0000-0000-000000000001");
    private static readonly Guid AddressId = Guid.Parse("76000000-0000-0000-0000-000000000001");

    private const string Schema =
        "{\"questions\":[" +
        "{\"key\":\"rooms\",\"type\":\"number\",\"required\":true,\"min\":1,\"max\":10}," +
        "{\"key\":\"level\",\"type\":\"choice\",\"required\":true,\"options\":[\"light\",\"deep\"]}]}";

    public async Task InitializeAsync()
    {
        await fixture.ResetDatabaseAsync();
        await SeedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "[IT-BOOK-004-01] Quote returns a server breakdown and the created booking persists a matching breakdown")]
    public async Task Quote_And_Create_ProduceMatchingServerPricing()
    {
        using var client = AuthenticatedClient(ClientId, UserRole.Client);

        var quoteResponse = await client.PostAsJsonAsync("/api/Bookings/quote", new BookingQuoteRequestDto
        {
            ServiceId = ServiceId,
            DurationHours = 2,
            DiscountAmount = 50_000
        });
        Assert.Equal(HttpStatusCode.OK, quoteResponse.StatusCode);
        var quote = await quoteResponse.Content.ReadDataAsync<PricingBreakdownDto>();
        Assert.NotNull(quote);
        Assert.Equal(200_000m, quote!.LineTotal);
        Assert.Equal(200_000m, quote.TotalPrice);
        Assert.Equal("VND", quote.Currency);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Bookings");
        request.Headers.Add("Idempotency-Key", "it-book-004-create");
        request.Content = JsonContent.Create(new CreateBookingDto
        {
            ServiceId = ServiceId,
            AddressId = AddressId,
            BookingType = BookingType.Immediate,
            DurationHours = 2,
            DiscountAmount = 50_000,
            OptionAnswers = Answers("{\"rooms\":2,\"level\":\"deep\"}")
        });
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var booking = await response.Content.ReadDataAsync<BookingDto>();
        Assert.NotNull(booking);
        Assert.NotNull(booking!.PricingBreakdown);
        Assert.Equal(200_000m, booking.PricingBreakdown!.TotalPrice);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await db.Bookings.SingleAsync(item => item.Id == booking.Id);
        Assert.NotEqual("{}", persisted.PricingBreakdown);
        Assert.Equal(200_000m, persisted.TotalPrice);
    }

    [Fact(DisplayName = "[IT-BOOK-002-01] Valid service-defined answers are persisted on the booking")]
    public async Task Create_ValidAnswers_PersistsOptionAnswers()
    {
        using var client = AuthenticatedClient(ClientId, UserRole.Client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Bookings");
        request.Headers.Add("Idempotency-Key", "it-book-002-valid");
        request.Content = JsonContent.Create(new CreateBookingDto
        {
            ServiceId = ServiceId,
            AddressId = AddressId,
            BookingType = BookingType.Immediate,
            DurationHours = 2,
            OptionAnswers = Answers("{\"rooms\":3,\"level\":\"light\"}")
        });

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var booking = await response.Content.ReadDataAsync<BookingDto>();

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await db.Bookings.SingleAsync(item => item.Id == booking!.Id);
        using var stored = JsonDocument.Parse(persisted.OptionAnswers);
        Assert.Equal(3, stored.RootElement.GetProperty("rooms").GetInt32());
        Assert.Equal("light", stored.RootElement.GetProperty("level").GetString());
    }

    [Fact(DisplayName = "[IT-BOOK-002-02] Answers violating the schema are rejected without creating a booking")]
    public async Task Create_InvalidAnswers_Rejected()
    {
        using var client = AuthenticatedClient(ClientId, UserRole.Client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Bookings");
        request.Headers.Add("Idempotency-Key", "it-book-002-invalid");
        request.Content = JsonContent.Create(new CreateBookingDto
        {
            ServiceId = ServiceId,
            AddressId = AddressId,
            BookingType = BookingType.Immediate,
            DurationHours = 2,
            OptionAnswers = Answers("{\"level\":\"deep\"}") // missing required "rooms"
        });

        var response = await client.SendAsync(request);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("BOOKING_OPTION_ANSWERS_INVALID", body.GetProperty("errorCode").GetString());

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.Bookings.AnyAsync(item => item.IdempotencyKey == "it-book-002-invalid"));
    }

    private static Dictionary<string, JsonElement> Answers(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

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

    private async Task SeedAsync()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        db.Accounts.AddRange(
            BookingApiTestData.Account(ClientId, "feat-client@cleanai.test", UserRole.Client, now),
            BookingApiTestData.Account(WorkerId, "feat-worker@cleanai.test", UserRole.Worker, now));
        db.Profiles.AddRange(
            BookingApiTestData.Profile(ClientId, "Feature Client", now),
            BookingApiTestData.Profile(WorkerId, "Feature Worker", now));
        db.Services.Add(new Service
        {
            Id = ServiceId,
            Name = "Schema Service",
            PropertyType = PropertyType.Apartment,
            UnitType = ServiceUnitType.Hour,
            BasePrice = 100_000,
            MinimumHours = 2,
            IsActive = true,
            BookingFormSchema = Schema,
            CreatedAt = now,
            UpdatedAt = now
        });
        db.UserAddresses.Add(new UserAddress
        {
            Id = AddressId,
            UserId = ClientId,
            Label = "Home",
            AddressText = "Test address",
            PropertyType = PropertyType.Apartment,
            CreatedAt = now,
            UpdatedAt = now
        });
        db.WorkerProfiles.Add(new WorkerProfile
        {
            UserId = WorkerId,
            OnlineStatus = WorkerOnlineStatus.Online,
            VerificationStatus = "approved",
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
        db.WorkerAvailabilities.Add(new WorkerAvailability
        {
            Id = Guid.NewGuid(),
            WorkerId = WorkerId,
            StartTime = now.Date.AddDays(1),
            EndTime = now.Date.AddDays(2),
            Status = AvailabilityStatus.Available,
            CreatedAt = now,
            UpdatedAt = now
        });
        await db.SaveChangesAsync();
    }
}

using Cleaning.BLL.Features.UserAddresses;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Cleaning.DAL.Enums;
using Microsoft.IdentityModel.Tokens;

namespace CleaningService.API.Tests;

[Collection(ApiTestCollection.Name)]
public sealed class EnvelopeApiTests(PostgreSqlApiFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "[IT-BE-ENVELOPE-01] AppException is returned as a failure envelope with its error code")]
    public async Task AppException_ReturnsFailureEnvelope()
    {
        using var client = AuthenticatedClient(Guid.NewGuid(), UserRole.Client);

        var response = await client.GetAsync($"/api/Bookings/{Guid.NewGuid()}");
        var body = await response.Content.ReadEnvelopeAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(body.GetProperty("success").GetBoolean());
        Assert.Equal("BOOKING_NOT_FOUND", body.GetProperty("errorCode").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("data").ValueKind);
    }

    [Fact(DisplayName = "[IT-BE-ENVELOPE-02] Model validation failures return VALIDATION_ERROR with field errors")]
    public async Task ModelValidation_ReturnsValidationEnvelope()
    {
        var response = await fixture.Client.PostAsJsonAsync("/api/Auth/register", new { });
        var body = await response.Content.ReadEnvelopeAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(body.GetProperty("success").GetBoolean());
        Assert.Equal("VALIDATION_ERROR", body.GetProperty("errorCode").GetString());
        Assert.NotEqual(JsonValueKind.Null, body.GetProperty("errors").ValueKind);
        Assert.True(body.GetProperty("errors").EnumerateObject().Any());
    }

    [Fact(DisplayName = "[IT-BE-ENVELOPE-03] Successful responses are wrapped as a success envelope")]
    public async Task Success_ReturnsSuccessEnvelope()
    {
        var response = await fixture.Client.GetAsync("/api/ServiceCatalog/categories");
        var body = await response.Content.ReadEnvelopeAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.GetProperty("success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("errorCode").ValueKind);
        Assert.True(body.TryGetProperty("data", out _));
    }

    [Fact(DisplayName = "[IT-BE-ENVELOPE-04] Unhandled exceptions return a generic INTERNAL_ERROR without leaking details")]
    public async Task UnhandledException_ReturnsGenericInternalError()
    {
        using var client = AuthenticatedClient(Guid.NewGuid(), UserRole.Client);

        var response = await client.PostAsJsonAsync("/api/UserAddresses", new CreateUserAddressDto
        {
            AddressText = "Envelope test address",
            Label = "Home",
            PropertyType = PropertyType.Apartment,
            IsDefault = false
        });
        var body = await response.Content.ReadEnvelopeAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.False(body.GetProperty("success").GetBoolean());
        Assert.Equal("INTERNAL_ERROR", body.GetProperty("errorCode").GetString());
        Assert.Equal("Đã xảy ra lỗi, vui lòng thử lại.", body.GetProperty("message").GetString());
        Assert.False(body.TryGetProperty("details", out _));
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

using Cleaning.DAL.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;
namespace CleaningService.API.Tests;

public sealed class PostgreSqlApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("cleaning_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private Respawner? _respawner;
    private string? _previousConnectionString;

    public HttpClient Client { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["JwtConfig:Secret"] =
                        "3YlcESfqMCfUbxi5yDM0lAb7oh6XiOAniuq4Nm50Gjw=",
                    ["JwtConfig:Issuer"] = "CleaningService.Api.Tests",
                    ["JwtConfig:Audience"] = "CleaningService.Api.Tests",
                    ["VNPay:TmnCode"] = "TESTCODE",
                    ["VNPay:HashSecret"] = "TEST_HASH_SECRET",
                    ["VNPay:BaseUrl"] = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
                    ["VNPay:ReturnUrl"] = "https://api.test.local/api/Payments/vnpay-return",
                    // appsettings.json (with real Cloudinary credentials) is gitignored and doesn't exist in
                    // CI, so CloudinaryConfig binds to all-empty defaults there. UseMock=false in that case
                    // makes CloudinaryFileStorageService's constructor build a real Cloudinary Account with an
                    // empty cloud name, which throws — and since BookingsController/ProfilesController now
                    // take IFileStorageService in their constructor, that kills every request routed to either
                    // one. Force mock mode for tests explicitly, same as the other test-only config above.
                    ["CloudinaryConfig:UseMock"] = "true"
                });
        });
    }

    public async Task InitializeAsync()
    {
        await _database.StartAsync();
        _previousConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _database.GetConnectionString());
        Client = CreateClient();

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();

        await using var connection = new NpgsqlConnection(_database.GetConnectionString());
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = [new Table("__EFMigrationsHistory")]
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(_database.GetConnectionString());
        await connection.OpenAsync();
        await _respawner!.ResetAsync(connection);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Client.Dispose();
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _previousConnectionString);
        await _database.DisposeAsync();
        Dispose();
    }
}

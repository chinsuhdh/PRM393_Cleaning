using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;
using DotNetEnv;

namespace Cleaning.DAL.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? BuildLocalDockerConnectionString();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }

    private static string BuildLocalDockerConnectionString()
    {
        Env.TraversePath().Load();
        var username = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "postgres";
        var port = GetConfiguredPort();

        return new NpgsqlConnectionStringBuilder
        {
            Host = "localhost",
            Port = port,
            Database = "PRM393_Cleaning",
            Username = username,
            Password = password
        }.ConnectionString;
    }

    private static int GetConfiguredPort()
    {
        var configuredPort = Environment.GetEnvironmentVariable("DB_HOST_PORT");

        return int.TryParse(configuredPort, out var port) ? port : 5433;
    }
}

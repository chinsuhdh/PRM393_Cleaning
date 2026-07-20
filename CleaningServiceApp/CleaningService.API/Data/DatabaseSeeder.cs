using Cleaning.DAL.Data;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using static CleaningService.API.Data.DevelopmentSeedData;

namespace CleaningService.API.Data;

public static class DatabaseSeeder
{
    public static void Seed(AppDbContext context)
    {
        ReloadPostgresTypes(context);
        var now = DateTime.UtcNow;

        AddServices(context, now);
        AddAccountsAndProfiles(context, now);
        AddClientAddress(context, now);
        AddWorkerData(context, now);
        AddKnowledgeDocuments(context, now);

        context.SaveChanges();
    }

    public static async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        await ReloadPostgresTypesAsync(context, cancellationToken);
        var now = DateTime.UtcNow;

        await AddServicesAsync(context, now, cancellationToken);
        await AddAccountsAndProfilesAsync(context, now, cancellationToken);
        await AddClientAddressAsync(context, now, cancellationToken);
        await AddWorkerDataAsync(context, now, cancellationToken);
        await AddKnowledgeDocumentsAsync(context, now, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static void ReloadPostgresTypes(AppDbContext context)
    {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            connection.Open();
        }

        connection.ReloadTypes();
    }

    private static async Task ReloadPostgresTypesAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await connection.ReloadTypesAsync(cancellationToken);
    }

    private static void AddServices(AppDbContext context, DateTime now)
    {
        if (!context.Services.Any(service => service.Id == ApartmentServiceId))
        {
            context.Services.Add(CreateApartmentService(now));
        }

        if (!context.Services.Any(service => service.Id == HouseServiceId))
        {
            context.Services.Add(CreateHouseService(now));
        }
    }

    private static async Task AddServicesAsync(AppDbContext context, DateTime now, CancellationToken cancellationToken)
    {
        if (!await context.Services.AnyAsync(service => service.Id == ApartmentServiceId, cancellationToken))
        {
            context.Services.Add(CreateApartmentService(now));
        }

        if (!await context.Services.AnyAsync(service => service.Id == HouseServiceId, cancellationToken))
        {
            context.Services.Add(CreateHouseService(now));
        }
    }

    private static void AddAccountsAndProfiles(AppDbContext context, DateTime now)
    {
        AddAccountAndProfile(context, AdminId, "admin@cleanai.local", "Nguyễn Thị Ngọc Anh", UserRole.Admin, now);
        AddAccountAndProfile(context, ClientId, "client@cleanai.local", "Trần Văn Bình", UserRole.Client, now);
        AddAccountAndProfile(context, WorkerId, "worker@cleanai.local", "Lê Minh Khôi", UserRole.Worker, now);
    }

    private static async Task AddAccountsAndProfilesAsync(AppDbContext context, DateTime now, CancellationToken cancellationToken)
    {
        await AddAccountAndProfileAsync(context, AdminId, "admin@cleanai.local", "Nguyễn Thị Ngọc Anh", UserRole.Admin, now, cancellationToken);
        await AddAccountAndProfileAsync(context, ClientId, "client@cleanai.local", "Trần Văn Bình", UserRole.Client, now, cancellationToken);
        await AddAccountAndProfileAsync(context, WorkerId, "worker@cleanai.local", "Lê Minh Khôi", UserRole.Worker, now, cancellationToken);
    }

    private static void AddAccountAndProfile(
        AppDbContext context,
        Guid id,
        string email,
        string fullName,
        UserRole role,
        DateTime now)
    {
        if (!context.Accounts.Any(account => account.Id == id || account.Email == email))
        {
            context.Accounts.Add(CreateAccount(id, email, role, now));
        }

        if (!context.Profiles.Any(profile => profile.Id == id))
        {
            context.Profiles.Add(CreateProfile(id, fullName, now));
        }
    }

    private static async Task AddAccountAndProfileAsync(
        AppDbContext context,
        Guid id,
        string email,
        string fullName,
        UserRole role,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (!await context.Accounts.AnyAsync(account => account.Id == id || account.Email == email, cancellationToken))
        {
            context.Accounts.Add(CreateAccount(id, email, role, now));
        }

        if (!await context.Profiles.AnyAsync(profile => profile.Id == id, cancellationToken))
        {
            context.Profiles.Add(CreateProfile(id, fullName, now));
        }
    }

    private static void AddClientAddress(AppDbContext context, DateTime now)
    {
        if (!context.UserAddresses.Any(address => address.Id == ClientAddressId))
        {
            context.UserAddresses.Add(CreateClientAddress(now));
        }
    }

    private static async Task AddClientAddressAsync(AppDbContext context, DateTime now, CancellationToken cancellationToken)
    {
        if (!await context.UserAddresses.AnyAsync(address => address.Id == ClientAddressId, cancellationToken))
        {
            context.UserAddresses.Add(CreateClientAddress(now));
        }
    }

    private static void AddWorkerData(AppDbContext context, DateTime now)
    {
        if (!context.WorkerProfiles.Any(worker => worker.UserId == WorkerId))
        {
            context.WorkerProfiles.Add(CreateWorkerProfile(now));
        }

        AddWorkerServices(context, now);

        if (!context.WorkerAvailabilities.Any(availability => availability.Id == WorkerAvailabilityId))
        {
            context.WorkerAvailabilities.Add(CreateWorkerAvailability(now));
        }
    }

    private static async Task AddWorkerDataAsync(AppDbContext context, DateTime now, CancellationToken cancellationToken)
    {
        if (!await context.WorkerProfiles.AnyAsync(worker => worker.UserId == WorkerId, cancellationToken))
        {
            context.WorkerProfiles.Add(CreateWorkerProfile(now));
        }

        await AddWorkerServicesAsync(context, now, cancellationToken);

        if (!await context.WorkerAvailabilities.AnyAsync(availability => availability.Id == WorkerAvailabilityId, cancellationToken))
        {
            context.WorkerAvailabilities.Add(CreateWorkerAvailability(now));
        }
    }

    private static void AddWorkerServices(AppDbContext context, DateTime now)
    {
        if (!context.WorkerServices.Any(skill => skill.WorkerId == WorkerId && skill.ServiceId == ApartmentServiceId))
        {
            context.WorkerServices.Add(CreateWorkerService(ApartmentServiceId, 24, now));
        }

        if (!context.WorkerServices.Any(skill => skill.WorkerId == WorkerId && skill.ServiceId == HouseServiceId))
        {
            context.WorkerServices.Add(CreateWorkerService(HouseServiceId, 18, now));
        }
    }

    private static async Task AddWorkerServicesAsync(AppDbContext context, DateTime now, CancellationToken cancellationToken)
    {
        if (!await context.WorkerServices.AnyAsync(
                skill => skill.WorkerId == WorkerId && skill.ServiceId == ApartmentServiceId,
                cancellationToken))
        {
            context.WorkerServices.Add(CreateWorkerService(ApartmentServiceId, 24, now));
        }

        if (!await context.WorkerServices.AnyAsync(
                skill => skill.WorkerId == WorkerId && skill.ServiceId == HouseServiceId,
                cancellationToken))
        {
            context.WorkerServices.Add(CreateWorkerService(HouseServiceId, 18, now));
        }
    }

    private static void AddKnowledgeDocuments(AppDbContext context, DateTime now)
    {
        foreach (var document in CreateKnowledgeDocuments(now))
        {
            if (!context.KnowledgeDocuments.Any(existing => existing.Id == document.Id))
            {
                context.KnowledgeDocuments.Add(document);
            }
        }
    }

    private static async Task AddKnowledgeDocumentsAsync(AppDbContext context, DateTime now, CancellationToken cancellationToken)
    {
        foreach (var document in CreateKnowledgeDocuments(now))
        {
            if (!await context.KnowledgeDocuments.AnyAsync(existing => existing.Id == document.Id, cancellationToken))
            {
                context.KnowledgeDocuments.Add(document);
            }
        }
    }

}

using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;

namespace CleaningService.API.Data;

internal static class DevelopmentSeedData
{
    internal const string DevelopmentPassword = "CleanAI123!";

    internal static readonly Guid ApartmentServiceId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    internal static readonly Guid HouseServiceId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    internal static readonly Guid AdminId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    internal static readonly Guid ClientId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    internal static readonly Guid WorkerId = Guid.Parse("20000000-0000-0000-0000-000000000003");
    internal static readonly Guid ClientAddressId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    internal static readonly Guid WorkerAvailabilityId = Guid.Parse("40000000-0000-0000-0000-000000000001");

    internal static Service CreateApartmentService(DateTime now) => new()
    {
        Id = ApartmentServiceId,
        Name = "Apartment Cleaning",
        Description = "Basic and deep cleaning services for apartments.",
        PropertyType = PropertyType.Apartment,
        UnitType = ServiceUnitType.Hour,
        BasePrice = 120_000m,
        MinimumHours = 2,
        IsActive = true,
        CreatedAt = now
    };

    internal static Service CreateHouseService(DateTime now) => new()
    {
        Id = HouseServiceId,
        Name = "House Cleaning",
        Description = "Basic and deep cleaning services for houses.",
        PropertyType = PropertyType.House,
        UnitType = ServiceUnitType.Hour,
        BasePrice = 150_000m,
        MinimumHours = 3,
        IsActive = true,
        CreatedAt = now
    };

    internal static Account CreateAccount(Guid id, string email, UserRole role, DateTime now) => new()
    {
        Id = id,
        Email = email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(DevelopmentPassword),
        Role = role,
        Status = AccountStatus.Active,
        IsEmailVerified = true,
        IsPhoneVerified = true,
        CreatedAt = now,
        UpdatedAt = now
    };

    internal static Profile CreateProfile(Guid id, string fullName, DateTime now) => new()
    {
        Id = id,
        FullName = fullName,
        CreatedAt = now,
        UpdatedAt = now
    };

    internal static UserAddress CreateClientAddress(DateTime now) => new()
    {
        Id = ClientAddressId,
        UserId = ClientId,
        Label = "Home",
        AddressText = "1 Nguyen Hue, District 1, Ho Chi Minh City",
        Latitude = 10.7731m,
        Longitude = 106.7030m,
        PropertyType = PropertyType.Apartment,
        IsDefault = true,
        CreatedAt = now
    };

    internal static WorkerProfile CreateWorkerProfile(DateTime now) => new()
    {
        UserId = WorkerId,
        AverageRating = 5.0m,
        OnlineStatus = WorkerOnlineStatus.Online,
        CurrentLat = 10.7769m,
        CurrentLng = 106.7009m,
        ImmediateBookingEnabled = true,
        VerifiedAt = now,
        CreatedAt = now,
        UpdatedAt = now
    };

    internal static WorkerService CreateWorkerService(Guid serviceId, int experienceMonths, DateTime now) => new()
    {
        WorkerId = WorkerId,
        ServiceId = serviceId,
        ExperienceMonths = experienceMonths,
        IsVerified = true,
        CreatedAt = now
    };

    internal static WorkerAvailability CreateWorkerAvailability(DateTime now) => new()
    {
        Id = WorkerAvailabilityId,
        WorkerId = WorkerId,
        StartTime = now.Date.AddDays(1).AddHours(8),
        EndTime = now.Date.AddDays(30).AddHours(18),
        Status = AvailabilityStatus.Available,
        Note = "Development seed availability",
        CreatedAt = now
    };

    internal static IEnumerable<KnowledgeDocument> CreateKnowledgeDocuments(DateTime now)
    {
        yield return CreateKnowledgeDocument(
            "50000000-0000-0000-0000-000000000001",
            "Cancellation policy",
            "Customers can cancel a booking before work begins. Applicable fees and refunds depend on the booking state.",
            now);
        yield return CreateKnowledgeDocument(
            "50000000-0000-0000-0000-000000000002",
            "Available services",
            "CleanAI offers apartment cleaning and house cleaning charged by the hour.",
            now);
        yield return CreateKnowledgeDocument(
            "50000000-0000-0000-0000-000000000003",
            "Worker matching",
            "Immediate bookings match verified online workers. Scheduled bookings use worker skills and availability.",
            now);
    }

    private static KnowledgeDocument CreateKnowledgeDocument(string id, string title, string content, DateTime now) => new()
    {
        Id = Guid.Parse(id),
        Title = title,
        Content = content,
        Source = "Development seed",
        IsActive = true,
        CreatedAt = now
    };
}

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
        BookingFormSchema = ApartmentBookingFormSchema,
        CreatedAt = now,
        UpdatedAt = now
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
        BookingFormSchema = HouseBookingFormSchema,
        CreatedAt = now,
        UpdatedAt = now
    };

    // D.3 BookingFormSchema example: stepper/single_choice/multi_choice options carry priceDelta (VND) and
    // durationDelta (minutes); text/photos never affect price. "hologram" demonstrates that an unknown
    // question type is skipped by both the API validator and the Flutter renderer (forward compatibility).
    internal const string ApartmentBookingFormSchema = """
        {
          "questions": [
            { "id": "rooms", "type": "stepper", "label": "How many rooms?",
              "min": 1, "max": 10, "required": true,
              "unit": { "priceDelta": 40000, "durationDelta": 45 } },
            { "id": "level", "type": "single_choice", "label": "Cleaning level", "required": true,
              "options": [
                { "id": "light", "label": "Light clean" },
                { "id": "deep", "label": "Deep clean", "priceDelta": 50000, "durationDelta": 30 } ] },
            { "id": "addons", "type": "multi_choice", "label": "Extra tasks",
              "options": [
                { "id": "fridge", "label": "Inside fridge", "priceDelta": 30000, "durationDelta": 20 },
                { "id": "windows", "label": "Windows", "priceDelta": 50000, "durationDelta": 30 } ] },
            { "id": "pets", "type": "yes_no", "label": "Do you have pets?" },
            { "id": "note", "type": "text", "label": "Note for your worker", "maxLength": 500 },
            { "id": "photos", "type": "photos", "label": "Photos of the space", "max": 5 }
          ]
        }
        """;

    internal const string HouseBookingFormSchema = """
        {
          "questions": [
            { "id": "floors", "type": "stepper", "label": "How many floors?",
              "min": 1, "max": 5, "required": true,
              "unit": { "priceDelta": 60000, "durationDelta": 60 } },
            { "id": "level", "type": "single_choice", "label": "Cleaning level", "required": true,
              "options": [
                { "id": "light", "label": "Light clean" },
                { "id": "deep", "label": "Deep clean", "priceDelta": 70000, "durationDelta": 45 } ] },
            { "id": "addons", "type": "multi_choice", "label": "Extra tasks",
              "options": [
                { "id": "oven", "label": "Inside oven", "priceDelta": 40000, "durationDelta": 25 },
                { "id": "garage", "label": "Garage", "priceDelta": 70000, "durationDelta": 40 } ] },
            { "id": "garden", "type": "yes_no", "label": "Does the house have a garden?" },
            { "id": "pets", "type": "yes_no", "label": "Do you have pets?" },
            { "id": "note", "type": "text", "label": "Note for your worker", "maxLength": 500 },
            { "id": "photos", "type": "photos", "label": "Photos of the space", "max": 5 },
            { "id": "future", "type": "hologram", "label": "Unknown from the future" }
          ]
        }
        """;

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
        CreatedAt = now,
        UpdatedAt = now
    };

    internal static WorkerProfile CreateWorkerProfile(DateTime now) => new()
    {
        UserId = WorkerId,
        AverageRating = 5.0m,
        OnlineStatus = WorkerOnlineStatus.Online,
        CurrentLat = 10.7769m,
        CurrentLng = 106.7009m,
        LocationUpdatedAt = now,
        BaseLatitude = 10.7769m,
        BaseLongitude = 106.7009m,
        ServiceRadiusKm = 10m,
        VerifiedAt = now,
        VerificationStatus = "approved",
        CreatedAt = now,
        UpdatedAt = now
    };

    internal static WorkerService CreateWorkerService(Guid serviceId, int experienceMonths, DateTime now) => new()
    {
        WorkerId = WorkerId,
        ServiceId = serviceId,
        ExperienceMonths = experienceMonths,
        IsVerified = true,
        VerifiedAt = now,
        CreatedAt = now,
        UpdatedAt = now
    };

    internal static WorkerAvailability CreateWorkerAvailability(DateTime now) => new()
    {
        Id = WorkerAvailabilityId,
        WorkerId = WorkerId,
        StartTime = now.Date.AddDays(1).AddHours(8),
        EndTime = now.Date.AddDays(30).AddHours(18),
        Status = AvailabilityStatus.Available,
        Note = "Development seed availability",
        CreatedAt = now,
        UpdatedAt = now
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

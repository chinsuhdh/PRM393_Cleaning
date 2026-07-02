using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;

namespace CleaningService.API.Tests;

internal static class BookingApiTestData
{
    public static Account Account(Guid id, string email, UserRole role, DateTime now) => new()
    {
        Id = id, Email = email, PasswordHash = "not-used", Role = role,
        Status = AccountStatus.Active, IsEmailVerified = true,
        CreatedAt = now, UpdatedAt = now
    };

    public static Profile Profile(Guid id, string name, DateTime now) => new()
    {
        Id = id, FullName = name, CreatedAt = now, UpdatedAt = now
    };
}

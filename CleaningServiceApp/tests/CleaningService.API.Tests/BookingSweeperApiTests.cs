using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Data;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CleaningService.API.Tests;

[Collection(ApiTestCollection.Name)]
public sealed class BookingSweeperApiTests(PostgreSqlApiFixture fixture) : IAsyncLifetime
{
    private static readonly Guid ClientId = Guid.Parse("9a000000-0000-0000-0000-000000000001");
    private static readonly Guid ServiceId = Guid.Parse("9a100000-0000-0000-0000-000000000001");
    private static readonly Guid AddressId = Guid.Parse("9a200000-0000-0000-0000-000000000001");
    private static readonly Guid BookingId = Guid.Parse("9a300000-0000-0000-0000-000000000001");

    public async Task InitializeAsync()
    {
        await fixture.ResetDatabaseAsync();
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        db.Accounts.Add(BookingApiTestData.Account(ClientId, "sweep-client@test.local", UserRole.Client, now));
        db.Profiles.Add(BookingApiTestData.Profile(ClientId, "Sweep Client", now));
        db.Services.Add(new Service
        {
            Id = ServiceId,
            Name = "Sweep Service",
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
        db.Bookings.Add(new Booking
        {
            Id = BookingId,
            ClientId = ClientId,
            ServiceId = ServiceId,
            AddressId = AddressId,
            BookingType = BookingType.Scheduled,
            ScheduledStartTime = now.AddMinutes(30),
            ScheduledEndTime = now.AddMinutes(30).AddHours(2),
            DurationHours = 2,
            UnitPrice = 100_000,
            TotalPrice = 200_000,
            Status = BookingStatus.AwaitingWorker,
            AddressSnapshot = "{}",
            OptionAnswers = "{}",
            PricingBreakdown = "{}",
            CreatedAt = now,
            UpdatedAt = now
        });
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "[IT-SWEEP-01] Resolving IBookingSweepService from the real DI container auto-cancels an unanswered scheduled booking within 1 hour")]
    public async Task RunTick_FromRealContainer_AutoCancelsDueBooking()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var sweepService = scope.ServiceProvider.GetRequiredService<IBookingSweepService>();

        await sweepService.RunTickAsync();

        await using var assertScope = fixture.Services.CreateAsyncScope();
        var db = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await db.Bookings.SingleAsync(b => b.Id == BookingId);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }
}

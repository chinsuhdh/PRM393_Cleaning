using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Cleaning.DAL.Interfaces;

namespace CleaningService.API.Services;

// E.9: fans out real eligible-worker positions to every searching client's `booking:{id}` group on a
// ~60s cadence (matching the K.6 idle location cadence) — replaces the client's REST poll on the
// finding-worker map.
public sealed class NearbyWorkerLocationsBroadcastService(
    IServiceScopeFactory scopeFactory,
    ILogger<NearbyWorkerLocationsBroadcastService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await BroadcastAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Nearby-worker broadcast tick failed");
            }
        }
    }

    private async Task BroadcastAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var dispatchPublisher = scope.ServiceProvider.GetRequiredService<IDispatchPublisher>();

        var searchingBookings = await unitOfWork.Repository<Booking>().FindAsync(
            b => b.Status == BookingStatus.AwaitingWorker && b.BookingType == BookingType.Immediate);

        foreach (var booking in searchingBookings)
        {
            stoppingToken.ThrowIfCancellationRequested();
            var locations = await bookingService.GetNearbyOnlineWorkerLocationsAsync(booking.Id, booking.ClientId);
            await dispatchPublisher.NearbyWorkerLocationsAsync(booking.Id, locations);
        }
    }
}

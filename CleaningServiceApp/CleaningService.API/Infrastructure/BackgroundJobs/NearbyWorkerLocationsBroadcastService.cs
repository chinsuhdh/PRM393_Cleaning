using Cleaning.BLL.Features.Bookings;
using Cleaning.BLL.Infrastructure.Dispatch;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Cleaning.DAL.Interfaces;
using CleaningService.API.Common;

namespace CleaningService.API.Infrastructure.BackgroundJobs;

public sealed class NearbyWorkerLocationsBroadcastService(
    IServiceScopeFactory scopeFactory,
    ILogger<NearbyWorkerLocationsBroadcastService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(BackgroundServiceConstants.DefaultPollIntervalSeconds);

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
            b => b.Status == BookingStatus.AwaitingWorker &&
                 (b.BookingType == BookingType.Immediate || b.BookingType == BookingType.Scheduled));

        foreach (var booking in searchingBookings)
        {
            stoppingToken.ThrowIfCancellationRequested();
            var locations = await bookingService.GetNearbyOnlineWorkerLocationsAsync(booking.Id, booking.ClientId);
            await dispatchPublisher.NearbyWorkerLocationsAsync(booking.Id, locations);
        }
    }
}

using Cleaning.BLL.Features.Bookings;
using CleaningService.API.Common;

namespace CleaningService.API.Infrastructure.BackgroundJobs;

public sealed class BookingSweeperService(
    IServiceScopeFactory scopeFactory,
    ILogger<BookingSweeperService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(BackgroundServiceConstants.DefaultPollIntervalSeconds);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var sweepService = scope.ServiceProvider.GetRequiredService<IBookingSweepService>();
                await sweepService.RunTickAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Booking sweeper tick failed");
            }
        }
    }
}

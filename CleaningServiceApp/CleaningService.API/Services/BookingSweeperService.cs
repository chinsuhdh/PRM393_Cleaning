using Cleaning.BLL.Interfaces;

namespace CleaningService.API.Services;

public sealed class BookingSweeperService(
    IServiceScopeFactory scopeFactory,
    ILogger<BookingSweeperService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

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

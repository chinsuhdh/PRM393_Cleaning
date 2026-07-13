using Cleaning.BLL.Interfaces;

namespace CleaningService.API.Services;

public sealed class PayoutSweeperService(
    IServiceScopeFactory scopeFactory,
    ILogger<PayoutSweeperService> logger) : BackgroundService
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
                var sweepService = scope.ServiceProvider.GetRequiredService<IPayoutSweepService>();
                await sweepService.RunTickAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Payout sweeper tick failed");
            }
        }
    }
}

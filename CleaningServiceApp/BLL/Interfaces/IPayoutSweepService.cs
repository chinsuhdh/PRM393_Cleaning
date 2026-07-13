namespace Cleaning.BLL.Interfaces;

public interface IPayoutSweepService
{
    Task RunTickAsync(CancellationToken cancellationToken = default);
}

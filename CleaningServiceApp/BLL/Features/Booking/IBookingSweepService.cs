namespace Cleaning.BLL.Interfaces;

public interface IBookingSweepService
{
    Task RunTickAsync(CancellationToken cancellationToken = default);
}

namespace Cleaning.BLL.Features.Bookings;

public interface IBookingSweepService
{
    Task RunTickAsync(CancellationToken cancellationToken = default);
}

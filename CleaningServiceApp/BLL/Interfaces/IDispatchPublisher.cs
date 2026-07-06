using Cleaning.BLL.DTOs;

namespace Cleaning.BLL.Interfaces;

public interface IDispatchPublisher
{
    Task JobPostedAsync(BookingDto booking, IReadOnlyCollection<Guid> workerIds);
    Task JobTakenAsync(Guid bookingId, IReadOnlyCollection<Guid> workerIds);
    Task JobCancelledAsync(Guid bookingId, IReadOnlyCollection<Guid> workerIds);
}

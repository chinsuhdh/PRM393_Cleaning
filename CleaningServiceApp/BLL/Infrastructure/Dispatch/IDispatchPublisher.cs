using Cleaning.BLL.Features.Bookings;
using Cleaning.BLL.Features.Chat;

namespace Cleaning.BLL.Infrastructure.Dispatch;

public interface IDispatchPublisher
{
    Task JobPostedAsync(BookingDto booking, IReadOnlyCollection<Guid> workerIds);
    Task JobTakenAsync(Guid bookingId, IReadOnlyCollection<Guid> workerIds);
    Task JobCancelledAsync(Guid bookingId, IReadOnlyCollection<Guid> workerIds);

    Task BookingStatusChangedAsync(Guid bookingId, Guid clientId, string newStatus);

    Task WorkerPositionAsync(Guid bookingId, decimal latitude, decimal longitude);

    Task NearbyWorkerLocationsAsync(Guid bookingId, IReadOnlyList<NearbyWorkerLocationDto> locations);

    Task WorkerSuspendedAsync(Guid workerId);

    Task ChatMessageReceivedAsync(Guid bookingId, BookingMessageDto message);
}

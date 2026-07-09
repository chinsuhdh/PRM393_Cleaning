using Cleaning.BLL.DTOs;

namespace Cleaning.BLL.Interfaces;

public interface IDispatchPublisher
{
    Task JobPostedAsync(BookingDto booking, IReadOnlyCollection<Guid> workerIds);
    Task JobTakenAsync(Guid bookingId, IReadOnlyCollection<Guid> workerIds);
    Task JobCancelledAsync(Guid bookingId, IReadOnlyCollection<Guid> workerIds);

    /// Pushed to whoever is subscribed to `booking:{bookingId}` (via DispatchHub.SubscribeBooking) —
    /// lets Booking Detail live-update the status on the *other* party's screen the moment either side
    /// changes it, instead of relying only on the OnTheWay/searching polling timers.
    Task BookingStatusChangedAsync(Guid bookingId, string newStatus);

    /// F.2/F.3: the assigned worker's live position while `OnTheWay`, forwarded on every
    /// `PATCH api/workers/location` call — replaces the client's REST poll on the live-tracking map.
    Task WorkerPositionAsync(Guid bookingId, decimal latitude, decimal longitude);

    /// E.6/E.9: anonymous eligible-worker positions for a searching client, pushed on a ~60s cadence
    /// by a background sweep — replaces the client's REST poll on the finding-worker map.
    Task NearbyWorkerLocationsAsync(Guid bookingId, IReadOnlyList<NearbyWorkerLocationDto> locations);
}

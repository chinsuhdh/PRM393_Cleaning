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
}

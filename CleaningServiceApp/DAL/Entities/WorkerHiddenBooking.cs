namespace Cleaning.DAL.Entities;

// A worker's "hide this job" action on the broadcast feed (E.4) — hidden jobs never resurface for them.
public class WorkerHiddenBooking
{
    public Guid WorkerId { get; set; }
    public Guid BookingId { get; set; }
    public DateTime CreatedAt { get; set; }
}

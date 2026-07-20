namespace Cleaning.DAL.Entities;

public class WorkerHiddenBooking
{
    public Guid WorkerId { get; set; }
    public Guid BookingId { get; set; }
    public DateTime CreatedAt { get; set; }
}

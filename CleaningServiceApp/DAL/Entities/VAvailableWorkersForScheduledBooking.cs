

namespace Cleaning.DAL.Entities;

// One row per broadcasting scheduled booking x eligible worker (full E.1 predicate lives in the view).
public partial class VAvailableWorkersForScheduledBooking
{
    public Guid? BookingId { get; set; }

    public Guid? WorkerId { get; set; }

    public string? FullName { get; set; }

    public decimal? AverageRating { get; set; }

    public Guid? ServiceId { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }
}

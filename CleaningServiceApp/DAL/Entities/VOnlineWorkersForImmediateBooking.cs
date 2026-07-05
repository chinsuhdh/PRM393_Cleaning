

namespace Cleaning.DAL.Entities;

// One row per broadcasting immediate booking x eligible worker (full E.1 predicate lives in the view).
public partial class VOnlineWorkersForImmediateBooking
{
    public Guid? BookingId { get; set; }

    public Guid? WorkerId { get; set; }

    public string? FullName { get; set; }

    public decimal? AverageRating { get; set; }

    public decimal? CurrentLat { get; set; }

    public decimal? CurrentLng { get; set; }

    public Guid? ServiceId { get; set; }
}



namespace Cleaning.DAL.Entities;

public partial class VAvailableWorkersForScheduledBooking
{
    public Guid? BookingId { get; set; }

    public Guid? WorkerId { get; set; }

    public string? FullName { get; set; }

    public decimal? AverageRating { get; set; }

    public Guid? ServiceId { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public decimal? CurrentLat { get; set; }

    public decimal? CurrentLng { get; set; }
}

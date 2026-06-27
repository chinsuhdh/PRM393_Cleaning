

namespace Cleaning.DAL.Entities;

public partial class VOnlineWorkersForImmediateBooking
{
    public Guid? WorkerId { get; set; }

    public string? FullName { get; set; }

    public decimal? AverageRating { get; set; }

    public decimal? CurrentLat { get; set; }

    public decimal? CurrentLng { get; set; }

    public Guid? ServiceId { get; set; }
}

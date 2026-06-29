
using Cleaning.DAL.Enums;

namespace Cleaning.DAL.Entities;

public partial class WorkerProfile
{
    public Guid UserId { get; set; }

    public decimal AverageRating { get; set; }

    public WorkerOnlineStatus OnlineStatus { get; set; }

    public decimal? CurrentLat { get; set; }

    public decimal? CurrentLng { get; set; }

    public bool ImmediateBookingEnabled { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? LocationUpdatedAt { get; set; }

    public decimal? BaseLatitude { get; set; }

    public decimal? BaseLongitude { get; set; }

    public decimal ServiceRadiusKm { get; set; } = 10;

    public string VerificationStatus { get; set; } = "pending";

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Profile User { get; set; } = null!;

    public virtual ICollection<WorkerAvailability> WorkerAvailabilities { get; set; } = new List<WorkerAvailability>();

    public virtual ICollection<WorkerService> WorkerServices { get; set; } = new List<WorkerService>();
}
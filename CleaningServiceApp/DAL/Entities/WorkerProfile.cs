using Cleaning.DAL.Enums;

namespace Cleaning.DAL.Entities;

public partial class WorkerProfile
{
    public Guid UserId { get; set; }

    public decimal AverageRating { get; set; }

    public WorkerOnlineStatus OnlineStatus { get; set; } = WorkerOnlineStatus.Offline;

    public decimal? CurrentLat { get; set; }

    public decimal? CurrentLng { get; set; }

    public bool ImmediateBookingEnabled { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Profile User { get; set; } = null!;

    public virtual ICollection<WorkerAvailability> WorkerAvailabilities { get; set; } = new List<WorkerAvailability>();

    public virtual ICollection<WorkerService> WorkerServices { get; set; } = new List<WorkerService>();
}

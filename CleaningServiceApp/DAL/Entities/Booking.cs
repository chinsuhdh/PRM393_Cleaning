
using Cleaning.DAL.Enums;

namespace Cleaning.DAL.Entities;

public partial class Booking
{
    public Guid Id { get; set; }

    public Guid ClientId { get; set; }

    public Guid? WorkerId { get; set; }

    public Guid ServiceId { get; set; }

    public Guid? AddressId { get; set; }

    public BookingType BookingType { get; set; }

    public DateTime ScheduledStartTime { get; set; }

    public DateTime ScheduledEndTime { get; set; }

    public DateTime? ActualStartTime { get; set; }

    public DateTime? ActualEndTime { get; set; }

    public decimal DurationHours { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal ExtraFee { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalPrice { get; set; }

    public BookingStatus Status { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string OptionAnswers { get; set; } = "{}";

    public string PricingBreakdown { get; set; } = "{}";

    public string AddressSnapshot { get; set; } = "{}";

    public string? IdempotencyKey { get; set; }

    public int Version { get; set; } = 1;

    public virtual UserAddress? Address { get; set; }

    public virtual BookingCancellation? BookingCancellation { get; set; }

    public virtual ICollection<BookingPhoto> BookingPhotos { get; set; } = new List<BookingPhoto>();

    public virtual ICollection<BookingRescheduleRequest> BookingRescheduleRequests { get; set; } = new List<BookingRescheduleRequest>();

    public virtual ICollection<BookingStatusLog> BookingStatusLogs { get; set; } = new List<BookingStatusLog>();

    public virtual Profile Client { get; set; } = null!;

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Payment? Payment { get; set; }

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual Service Service { get; set; } = null!;

    public virtual WorkerProfile? Worker { get; set; }
}
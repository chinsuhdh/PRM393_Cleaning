
using Cleaning.DAL.Enums;

namespace Cleaning.DAL.Entities;

public partial class BookingCancellation
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public Guid CancelledBy { get; set; }

    public UserRole ActorRole { get; set; }

    public string? ReasonCode { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual Account CancelledByNavigation { get; set; } = null!;
}


using Cleaning.DAL.Enums;

namespace Cleaning.DAL.Entities;

public partial class Notification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid? BookingId { get; set; }

    public NotificationType Type { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public DateTime? ArchivedAt { get; set; }

    public string? DeepLink { get; set; }

    public string Payload { get; set; } = "{}";

    public string? DeduplicationKey { get; set; }

    public virtual Booking? Booking { get; set; }

    public virtual Account User { get; set; } = null!;
}
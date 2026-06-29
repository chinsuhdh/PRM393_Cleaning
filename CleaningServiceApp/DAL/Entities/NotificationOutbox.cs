namespace Cleaning.DAL.Entities;

public class NotificationOutbox
{
    public Guid Id { get; set; }
    public Guid? NotificationId { get; set; }
    public Guid UserId { get; set; }
    public string EventType { get; set; } = null!;
    public string Payload { get; set; } = "{}";
    public string Status { get; set; } = "pending";
    public int Attempts { get; set; }
    public DateTime AvailableAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; }
}

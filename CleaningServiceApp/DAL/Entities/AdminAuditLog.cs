namespace Cleaning.DAL.Entities;

public class AdminAuditLog
{
    public Guid Id { get; set; }
    public Guid AdminId { get; set; }
    public string Action { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public string? EntityId { get; set; }
    public string? BeforeState { get; set; }
    public string? AfterState { get; set; }
    public string? Reason { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

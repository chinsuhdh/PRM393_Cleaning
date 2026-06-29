namespace Cleaning.DAL.Entities;

public class WorkerApplication
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = "pending";
    public string? GovernmentId { get; set; }
    public string? ExperienceSummary { get; set; }
    public string Evidence { get; set; } = "{}";
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string? RejectionReason { get; set; }
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

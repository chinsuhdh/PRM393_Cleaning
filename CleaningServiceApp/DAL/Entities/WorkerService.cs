

namespace Cleaning.DAL.Entities;

public partial class WorkerService
{
    public Guid WorkerId { get; set; }

    public Guid ServiceId { get; set; }

    public int ExperienceMonths { get; set; }

    public bool IsVerified { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public Guid? VerifiedBy { get; set; }

    public string? RejectionReason { get; set; }

    public virtual Service Service { get; set; } = null!;

    public virtual WorkerProfile Worker { get; set; } = null!;
}

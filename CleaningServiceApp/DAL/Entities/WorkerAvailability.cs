
using Cleaning.DAL.Enums;

namespace Cleaning.DAL.Entities;

public partial class WorkerAvailability
{
    public Guid Id { get; set; }

    public Guid WorkerId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public AvailabilityStatus Status { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual WorkerProfile Worker { get; set; } = null!;
}
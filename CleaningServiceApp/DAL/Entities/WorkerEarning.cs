namespace Cleaning.DAL.Entities;

public class WorkerEarning
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Guid WorkerId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime EarnedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

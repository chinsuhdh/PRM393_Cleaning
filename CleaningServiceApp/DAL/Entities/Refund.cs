

namespace Cleaning.DAL.Entities;

public partial class Refund
{
    public Guid Id { get; set; }

    public Guid PaymentId { get; set; }

    public decimal Amount { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Status { get; set; } = "pending";

    public string? ProviderRefundId { get; set; }

    public DateTime RequestedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public Guid? RequestedBy { get; set; }

    public string? IdempotencyKey { get; set; }

    public virtual Payment Payment { get; set; } = null!;
}

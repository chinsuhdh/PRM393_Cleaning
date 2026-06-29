
using Cleaning.DAL.Enums;

namespace Cleaning.DAL.Entities;

public partial class Payment
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; }

    public string? TransactionId { get; set; }

    public PaymentStatus Status { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? IdempotencyKey { get; set; }

    public string? ProviderReference { get; set; }

    public string? FailureCode { get; set; }

    public string? FailureMessage { get; set; }

    public DateTime? CallbackVerifiedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();
}
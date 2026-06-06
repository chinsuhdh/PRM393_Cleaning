using System;
using System.Collections.Generic;
using Cleaning.DAL.Enums;

namespace Cleaning.DAL.Entities;

public partial class Payment
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public decimal Amount { get; set; }

    public string? TransactionId { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public virtual Booking Booking { get; set; } = null!;
}

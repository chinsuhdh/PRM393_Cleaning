using System;
using System.Collections.Generic;

namespace Cleaning.DAL.Entities;

public partial class AiRecommendation
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public Guid WorkerId { get; set; }

    public decimal Score { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual WorkerProfile Worker { get; set; } = null!;
}

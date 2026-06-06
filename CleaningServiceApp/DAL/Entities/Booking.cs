using System;
using System.Collections.Generic;
using Cleaning.DAL.Enums;

namespace Cleaning.DAL.Entities;

public partial class Booking
{
    public Guid Id { get; set; }

    public Guid ClientId { get; set; }

    public Guid? WorkerId { get; set; }

    public Guid ServiceId { get; set; }

    public Guid? AddressId { get; set; }

    public DateTime ScheduledTime { get; set; }

    public int DurationHours { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal ExtraFee { get; set; }

    public decimal TotalPrice { get; set; }

    public string? Notes { get; set; }

    public string? CancelReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public virtual UserAddress? Address { get; set; }

    public virtual ICollection<AiRecommendation> AiRecommendations { get; set; } = new List<AiRecommendation>();

    public virtual ICollection<BookingStatusLog> BookingStatusLogs { get; set; } = new List<BookingStatusLog>();

    public virtual Profile Client { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual Service Service { get; set; } = null!;

    public virtual WorkerProfile? Worker { get; set; }
}

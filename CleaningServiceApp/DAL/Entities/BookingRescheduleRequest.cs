using System;
using System.Collections.Generic;
using Cleaning.DAL.Enums;

namespace Cleaning.DAL.Entities;

public partial class BookingRescheduleRequest
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public Guid RequestedBy { get; set; }

    public DateTime OldStartTime { get; set; }

    public DateTime OldEndTime { get; set; }

    public DateTime NewStartTime { get; set; }

    public DateTime NewEndTime { get; set; }

    public RescheduleStatus Status { get; set; }

    public string? Reason { get; set; }

    public Guid? RespondedBy { get; set; }

    public DateTime? RespondedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual Account RequestedByNavigation { get; set; } = null!;

    public virtual Account? RespondedByNavigation { get; set; }
}
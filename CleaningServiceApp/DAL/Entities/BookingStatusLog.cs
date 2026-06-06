using System;
using System.Collections.Generic;
using Cleaning.DAL.Enums;

namespace Cleaning.DAL.Entities;

public partial class BookingStatusLog
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public Guid? ChangedBy { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public BookingStatus? OldStatus { get; set; }
    public BookingStatus NewStatus { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual Account? ChangedByNavigation { get; set; }
}

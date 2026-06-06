using System;
using System.Collections.Generic;

namespace Cleaning.DAL.Entities;

public partial class Review
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public Guid ReviewerId { get; set; }

    public Guid RevieweeId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual Profile Reviewee { get; set; } = null!;

    public virtual Profile Reviewer { get; set; } = null!;
}

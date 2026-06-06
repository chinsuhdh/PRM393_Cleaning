using System;
using System.Collections.Generic;

namespace Cleaning.DAL.Entities;

public partial class Profile
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Account IdNavigation { get; set; } = null!;

    public virtual ICollection<Review> ReviewReviewees { get; set; } = new List<Review>();

    public virtual ICollection<Review> ReviewReviewers { get; set; } = new List<Review>();

    public virtual ICollection<UserAddress> UserAddresses { get; set; } = new List<UserAddress>();

    public virtual WorkerProfile? WorkerProfile { get; set; }
}

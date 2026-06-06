using System;
using System.Collections.Generic;

namespace Cleaning.DAL.Entities;

public partial class WorkerProfile
{
    public Guid UserId { get; set; }

    public string? IdentityCardNumber { get; set; }

    public decimal AverageRating { get; set; }

    public int CompletedJobs { get; set; }

    public decimal? CurrentLat { get; set; }

    public decimal? CurrentLng { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AiRecommendation> AiRecommendations { get; set; } = new List<AiRecommendation>();

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Profile User { get; set; } = null!;

    public virtual ICollection<WorkerSkill> WorkerSkills { get; set; } = new List<WorkerSkill>();
}

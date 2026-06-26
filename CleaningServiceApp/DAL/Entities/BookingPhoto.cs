using System;
using System.Collections.Generic;
using Cleaning.DAL.Enums;

namespace Cleaning.DAL.Entities;

public partial class BookingPhoto
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public Guid UploadedBy { get; set; }

    public string PhotoUrl { get; set; } = null!;

    public PhotoType PhotoType { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AiCleanlinessAnalysis> AiCleanlinessAnalyses { get; set; } = new List<AiCleanlinessAnalysis>();

    public virtual Booking Booking { get; set; } = null!;

    public virtual Account UploadedByNavigation { get; set; } = null!;
}
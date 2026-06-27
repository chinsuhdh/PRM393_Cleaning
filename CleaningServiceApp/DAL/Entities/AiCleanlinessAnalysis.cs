using Cleaning.DAL.Enums;

namespace Cleaning.DAL.Entities;

public partial class AiCleanlinessAnalysis
{
    public Guid Id { get; set; }

    public Guid BookingPhotoId { get; set; }

    public CleanlinessLevel CleanlinessLevel { get; set; }

    public decimal? ConfidenceScore { get; set; }

    public string? DetectedIssues { get; set; }

    public string? SuggestedTasks { get; set; }

    public string? Summary { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual BookingPhoto BookingPhoto { get; set; } = null!;
}

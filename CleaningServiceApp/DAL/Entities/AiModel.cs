using System;
using System.Collections.Generic;

namespace Cleaning.DAL.Entities;

public partial class AiModel
{
    public Guid Id { get; set; }

    public string ModelName { get; set; } = null!;

    public string ModelVersion { get; set; } = null!;

    public string? GithubUrl { get; set; }

    public string? HuggingfaceUrl { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AiInferenceLog> AiInferenceLogs { get; set; } = new List<AiInferenceLog>();
}

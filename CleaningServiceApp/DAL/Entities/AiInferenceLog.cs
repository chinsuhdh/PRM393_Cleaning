using System;
using System.Collections.Generic;

namespace Cleaning.DAL.Entities;

public partial class AiInferenceLog
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string Prompt { get; set; } = null!;

    public string Response { get; set; } = null!;

    public int? LatencyMs { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account? User { get; set; }
}

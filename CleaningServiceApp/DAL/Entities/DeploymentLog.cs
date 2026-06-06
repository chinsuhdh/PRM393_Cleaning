using System;
using System.Collections.Generic;

namespace Cleaning.DAL.Entities;

public partial class DeploymentLog
{
    public Guid Id { get; set; }

    public string Version { get; set; } = null!;

    public string Environment { get; set; } = null!;

    public DateTime StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }
}

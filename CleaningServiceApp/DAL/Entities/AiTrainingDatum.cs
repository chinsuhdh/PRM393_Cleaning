using System;
using System.Collections.Generic;

namespace Cleaning.DAL.Entities;

public partial class AiTrainingDatum
{
    public Guid Id { get; set; }

    public string Question { get; set; } = null!;

    public string Answer { get; set; } = null!;

    public string? Category { get; set; }

    public DateTime CreatedAt { get; set; }
}

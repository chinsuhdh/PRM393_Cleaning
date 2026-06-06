using System;
using System.Collections.Generic;

namespace Cleaning.DAL.Entities;

public partial class SystemLog
{
    public Guid Id { get; set; }

    public string ServiceName { get; set; } = null!;

    public string Message { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}

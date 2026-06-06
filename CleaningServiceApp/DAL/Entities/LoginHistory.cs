using System;
using System.Collections.Generic;

namespace Cleaning.DAL.Entities;

public partial class LoginHistory
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public DateTime LoginTime { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public bool IsSuccess { get; set; }

    public string? FailReason { get; set; }

    public virtual Account Account { get; set; } = null!;
}

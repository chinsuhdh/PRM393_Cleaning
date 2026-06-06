using System;
using System.Collections.Generic;

namespace Cleaning.DAL.Entities;

public partial class OtpVerification
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public string OtpCode { get; set; } = null!;

    public string Purpose { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;
}

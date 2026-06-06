using System;
using System.Collections.Generic;

namespace Cleaning.DAL.Entities;

public partial class ExternalLogin
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public string Provider { get; set; } = null!;

    public string ProviderKey { get; set; } = null!;

    public string? ProviderDisplayName { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;
}

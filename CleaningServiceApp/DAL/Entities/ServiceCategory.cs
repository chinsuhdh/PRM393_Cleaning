using System;
using System.Collections.Generic;

namespace Cleaning.DAL.Entities;

public partial class ServiceCategory
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? IconUrl { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();
}

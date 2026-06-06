using System;
using System.Collections.Generic;
using Cleaning.DAL.Enums;

namespace Cleaning.DAL.Entities;

public partial class Service
{
    public Guid Id { get; set; }

    public Guid CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal BasePrice { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public ServiceUnitType UnitType { get; set; } = ServiceUnitType.Hour;

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ServiceCategory Category { get; set; } = null!;

    public virtual ICollection<WorkerSkill> WorkerSkills { get; set; } = new List<WorkerSkill>();
}

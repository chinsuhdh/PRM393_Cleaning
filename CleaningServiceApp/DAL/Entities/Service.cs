using Cleaning.DAL.Enums;

namespace Cleaning.DAL.Entities;

public partial class Service
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public PropertyType PropertyType { get; set; }

    public ServiceUnitType UnitType { get; set; } = ServiceUnitType.Hour;

    public decimal BasePrice { get; set; }

    public int MinimumHours { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<WorkerService> WorkerServices { get; set; } = new List<WorkerService>();
}

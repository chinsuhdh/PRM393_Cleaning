using System.ComponentModel.DataAnnotations;

namespace Cleaning.BLL.Features.ServiceCatalog
{
    public class ServiceCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? IconUrl { get; set; }
        public int SortOrder { get; set; }
    }

    public class ServiceDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string PropertyType { get; set; } = null!;
        public string UnitType { get; set; } = null!;
        public decimal BasePrice { get; set; }
        public int MinimumHours { get; set; }
        public bool IsActive { get; set; }
        public string BookingFormSchema { get; set; } = "{}";
    }
}
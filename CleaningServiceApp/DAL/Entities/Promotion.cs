namespace Cleaning.DAL.Entities;

public class Promotion
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = "fixed";
    public decimal DiscountValue { get; set; }
    public decimal? MaximumDiscountAmount { get; set; }
    public decimal MinimumBookingAmount { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public int? TotalQuota { get; set; }
    public int PerUserQuota { get; set; } = 1;
    public int RedeemedCount { get; set; }
    public string Status { get; set; } = "draft";
    public string Conditions { get; set; } = "{}";
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
}

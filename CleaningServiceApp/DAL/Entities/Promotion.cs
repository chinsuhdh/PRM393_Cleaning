namespace Cleaning.DAL.Entities;

public class Promotion
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? BannerImageUrl { get; set; }
    public string? BannerTitle { get; set; }
    public string DiscountType { get; set; } = "fixed";
    public decimal DiscountValue { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string Status { get; set; } = "draft";
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
}

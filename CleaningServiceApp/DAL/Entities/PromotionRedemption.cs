namespace Cleaning.DAL.Entities;

public class PromotionRedemption
{
    public Guid Id { get; set; }
    public Guid PromotionId { get; set; }
    public Guid UserId { get; set; }
    public Guid BookingId { get; set; }
    public decimal DiscountAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

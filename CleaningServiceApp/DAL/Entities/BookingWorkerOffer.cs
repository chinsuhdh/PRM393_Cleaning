namespace Cleaning.DAL.Entities;

public class BookingWorkerOffer
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Guid WorkerId { get; set; }
    public string Status { get; set; } = "pending";
    public decimal RankScore { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

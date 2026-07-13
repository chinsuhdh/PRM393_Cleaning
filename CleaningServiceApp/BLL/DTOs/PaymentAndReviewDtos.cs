using System.ComponentModel.DataAnnotations;

namespace Cleaning.BLL.DTOs
{
    // --- PAYMENT DTOs ---
    public class PaymentDto
    {
        public Guid Id { get; set; }
        public Guid BookingId { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? TransactionId { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PayNowRequestDto
    {
        [Required]
        public Guid BookingId { get; set; }
    }

    public class PayNowResponseDto
    {
        public Guid PaymentId { get; set; }
        public string PaymentUrl { get; set; } = null!;
    }

    // --- REVIEW DTOs ---
    public class ReviewDto
    {
        public Guid Id { get; set; }
        public Guid BookingId { get; set; }
        public Guid ReviewerId { get; set; }
        public Guid RevieweeId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateReviewDto
    {
        [Required]
        public Guid BookingId { get; set; }

        [Required]
        public Guid RevieweeId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
        public int Rating { get; set; }

        [MaxLength(500, ErrorMessage = "Comment cannot exceed 500 characters.")]
        public string? Comment { get; set; }
    }
}
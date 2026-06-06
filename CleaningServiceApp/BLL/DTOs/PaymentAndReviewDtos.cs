using System.ComponentModel.DataAnnotations;
using Cleaning.DAL.Enums;

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

    public class CreatePaymentDto
    {
        [Required]
        public Guid BookingId { get; set; }
        [Required]
        public decimal Amount { get; set; }
        [Required]
        public PaymentMethod Method { get; set; }
    }

    public class PaymentCallbackDto
    {
        [Required]
        public PaymentStatus Status { get; set; }
        public string? TransactionId { get; set; }
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
        [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1 đến 5 sao.")]
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
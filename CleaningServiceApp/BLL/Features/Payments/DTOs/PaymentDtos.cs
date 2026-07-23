using System.ComponentModel.DataAnnotations;

namespace Cleaning.BLL.Features.Payments
{
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
}

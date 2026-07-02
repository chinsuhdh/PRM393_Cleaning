using System.ComponentModel.DataAnnotations;
using Cleaning.DAL.Enums;

namespace Cleaning.BLL.DTOs
{
    public class BookingDto
    {
        public Guid Id { get; set; }
        public Guid ClientId { get; set; }
        public Guid? WorkerId { get; set; }
        public Guid ServiceId { get; set; }
        public Guid? AddressId { get; set; }
        public string BookingType { get; set; } = null!; // Thêm dòng này
        public DateTime ScheduledStartTime { get; set; }
        public DateTime ScheduledEndTime { get; set; }
        public decimal DurationHours { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal ExtraFee { get; set; }
        public decimal DiscountAmount { get; set; } // Thêm dòng này
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = null!;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }

        public string? ServiceName { get; set; }
        public string? AddressText { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }

    public class CreateBookingDto
    {
        [Required]
        public Guid ServiceId { get; set; }
        public Guid? AddressId { get; set; }
        [Required]
        public BookingType BookingType { get; set; } // Phải biết khách đặt loại nào
        [Required]
        public DateTime ScheduledStartTime { get; set; }
        public decimal DurationHours { get; set; } = 2;
        public decimal DiscountAmount { get; set; } = 0; // Thêm dòng này
        public string? Notes { get; set; }
    }

    public class UpdateBookingStatusDto
    {
        [Required]
        public BookingStatus NewStatus { get; set; }
        public string? Reason { get; set; }
    }
}
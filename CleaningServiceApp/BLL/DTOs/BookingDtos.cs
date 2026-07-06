using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        public DateTime UpdatedAt { get; set; }

        public string? ServiceName { get; set; }
        public string? AddressText { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        // BOOK-002: normalized answers to the service-defined questions, echoed back for the client summary.
        public string OptionAnswers { get; set; } = "{}";
        public string BookingFormSchema { get; set; } = "{}";

        // BOOK-004: the server-authored pricing breakdown shown on the confirmation screen.
        public PricingBreakdownDto? PricingBreakdown { get; set; }
        public IReadOnlyList<BookingPhotoDto> Photos { get; set; } = [];
        public IReadOnlyList<BookingStatusLogDto> StatusTimeline { get; set; } = [];

        // Only populated once a worker is actually assigned (WorkerId set) — never during broadcast,
        // so candidate workers are still never exposed to the client (matches the dispatch model).
        public WorkerSummaryDto? Worker { get; set; }
    }

    /// Assigned-worker info for the client's Booking Detail screen — identity + rating for the
    /// worker card, plus their last-reported position so the OnTheWay live map has something to show.
    public class WorkerSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public decimal Rating { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public DateTime? LocationUpdatedAt { get; set; }
    }

    public class CreateBookingDto
    {
        [Required]
        public Guid ServiceId { get; set; }
        public Guid? AddressId { get; set; }
        [Required]
        public BookingType BookingType { get; set; } // Phải biết khách đặt loại nào
        public DateTime? ScheduledStartTime { get; set; }
        public int ServiceVersion { get; set; } = 1;
        [JsonIgnore, Obsolete("Duration is derived by the server.")]
        public decimal DurationHours { get; set; }
        [JsonIgnore, Obsolete("Discounts are derived by the server.")]
        public decimal DiscountAmount { get; set; }
        public string? Notes { get; set; }

        // BOOK-002: answers to the service-defined questions, validated by the API against the service schema.
        public Dictionary<string, JsonElement>? OptionAnswers { get; set; }
    }

    // BOOK-004: a server-calculated price quote the client can request before creating a booking.
    public class BookingQuoteRequestDto
    {
        [Required]
        public Guid ServiceId { get; set; }
        public Dictionary<string, JsonElement>? OptionAnswers { get; set; }
        public DateTime? ScheduledStartTime { get; set; }
        [JsonIgnore, Obsolete("Duration is derived by the server.")]
        public decimal DurationHours { get; set; }
        [JsonIgnore, Obsolete("Discounts are derived by the server.")]
        public decimal DiscountAmount { get; set; }
    }

    // BOOK-004: the authoritative line-item breakdown; the client displays these values and never computes them.
    public class PricingBreakdownDto
    {
        public decimal UnitPrice { get; set; }
        public decimal DurationHours { get; set; }
        public decimal LineTotal { get; set; }
        public decimal ExtraFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }
        public int ServiceVersion { get; set; }
        public IReadOnlyList<PricingLineDto> Breakdown { get; set; } = [];
        public string Currency { get; set; } = "VND";
    }

    public class PricingLineDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class BookingPhotoDto
    {
        public Guid Id { get; set; }
        public string PhotoUrl { get; set; } = string.Empty;
        public string PhotoType { get; set; } = string.Empty;
    }

    public class BookingStatusLogDto
    {
        public string? OldStatus { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public Guid? ChangedBy { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class BookingAvailabilityRequestDto
    {
        [Required]
        public Guid ServiceId { get; set; }
        [Required]
        public Guid AddressId { get; set; }
        [Required]
        public BookingType BookingType { get; set; }
        [Range(0.5, 24)]
        public decimal DurationHours { get; set; } = 2;
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }

    public class BookingSlotDto
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class BookingAvailabilityDto
    {
        public BookingType BookingType { get; set; }
        public DateTime GeneratedAt { get; set; }
        public DateTime ValidUntil { get; set; }
        public IReadOnlyList<BookingSlotDto> Slots { get; set; } = [];
        public string? EmptyReasonCode { get; set; }
        public string? EmptyMessage { get; set; }
    }

    public class UpdateBookingStatusDto
    {
        [Required]
        public BookingStatus NewStatus { get; set; }
        public string? Reason { get; set; }
    }
}

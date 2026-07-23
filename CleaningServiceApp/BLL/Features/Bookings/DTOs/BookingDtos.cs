using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cleaning.BLL.Constants;
using Cleaning.DAL.Enums;

namespace Cleaning.BLL.Features.Bookings
{
    public class BookingDto
    {
        public Guid Id { get; set; }
        public Guid ClientId { get; set; }
        public Guid? WorkerId { get; set; }
        public Guid ServiceId { get; set; }
        public Guid? AddressId { get; set; }
        public string BookingType { get; set; } = null!;
        public DateTime ScheduledStartTime { get; set; }
        public DateTime ScheduledEndTime { get; set; }
        public DateTime? ActualStartTime { get; set; }
        public DateTime? ActualEndTime { get; set; }
        public decimal DurationHours { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal ExtraFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = null!;
        public string PaymentMethod { get; set; } = null!;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string? ServiceName { get; set; }
        public string? AddressText { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public decimal? DistanceKm { get; set; }
        public decimal? EstimatedMinutes { get; set; }

        public string OptionAnswers { get; set; } = "{}";
        public string BookingFormSchema { get; set; } = "{}";

        public PricingBreakdownDto? PricingBreakdown { get; set; }
        public IReadOnlyList<BookingPhotoDto> Photos { get; set; } = [];
        public IReadOnlyList<BookingStatusLogDto> StatusTimeline { get; set; } = [];

        public WorkerSummaryDto? Worker { get; set; }

        public BookingRescheduleRequestDto? PendingReschedule { get; set; }
        public IReadOnlyList<BookingRescheduleRequestDto> RescheduleHistory { get; set; } = [];
    }

    public class WorkerSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public decimal Rating { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public DateTime? LocationUpdatedAt { get; set; }
    }

    public class NearbyWorkerLocationDto
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }

    public class CreateBookingDto
    {
        [Required]
        public Guid ServiceId { get; set; }
        public Guid? AddressId { get; set; }
        [Required]
        public BookingType BookingType { get; set; }
        public DateTime? ScheduledStartTime { get; set; }
        public int ServiceVersion { get; set; } = 1;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        // Optional client-stated expected duration — affects scheduling/display only (see
        // BookingCreationService.CreateAsync), never pricing, which always comes from the server's
        // own BookingPricingCalculator computation regardless of this value.
        [Range(0, 24)]
        public decimal DurationHours { get; set; }
        [JsonIgnore, Obsolete("Discounts are derived by the server.")]
        public decimal DiscountAmount { get; set; }
        public string? Notes { get; set; }

        public Dictionary<string, JsonElement>? OptionAnswers { get; set; }
    }

    public class BookingQuoteRequestDto
    {
        [Required]
        public Guid ServiceId { get; set; }
        public Dictionary<string, JsonElement>? OptionAnswers { get; set; }
        public DateTime? ScheduledStartTime { get; set; }
        [Range(0, 24)]
        public decimal DurationHours { get; set; }
        [JsonIgnore, Obsolete("Discounts are derived by the server.")]
        public decimal DiscountAmount { get; set; }
    }

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

    public class UpdateBookingDurationDto
    {
        [Range(0.5, 24)]
        public decimal DurationHours { get; set; }
    }

    public class WorkerCancelBookingDto
    {
        [Required]
        public string ReasonCode { get; set; } = null!;
        public string? FreeText { get; set; }
    }

    public class ClientCancelBookingDto
    {
        [Required]
        public string ReasonCode { get; set; } = null!;
        public string? FreeText { get; set; }
    }

    public class ReportBookingDto
    {
        [Required]
        public string ReasonCode { get; set; } = null!;
        [Required, MinLength(CancellationConstants.ReportMinFreeTextLength)]
        public string FreeText { get; set; } = null!;
    }

    public enum RescheduleAction { Accept, Reject, Withdraw }

    public class ProposeRescheduleDto
    {
        [Required]
        public DateTime NewStartTime { get; set; }
        public string? Message { get; set; }
    }

    public class RespondRescheduleDto
    {
        [Required]
        public RescheduleAction Action { get; set; }
    }

    public class BookingRescheduleRequestDto
    {
        public Guid Id { get; set; }
        public Guid RequestedBy { get; set; }
        public DateTime OldStartTime { get; set; }
        public DateTime OldEndTime { get; set; }
        public DateTime NewStartTime { get; set; }
        public DateTime NewEndTime { get; set; }
        public string Status { get; set; } = null!;
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }
}

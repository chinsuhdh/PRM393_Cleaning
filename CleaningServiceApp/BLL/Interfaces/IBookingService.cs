using Cleaning.BLL.DTOs;

namespace Cleaning.BLL.Interfaces
{
    public interface IBookingService
    {
        Task<BookingAvailabilityDto> GetAvailabilityAsync(Guid clientId, BookingAvailabilityRequestDto request);
        Task<PricingBreakdownDto> GetQuoteAsync(Guid clientId, BookingQuoteRequestDto request);
        Task<BookingDto> CreateBookingAsync(Guid clientId, string idempotencyKey, CreateBookingDto request);
        Task<IEnumerable<BookingDto>> GetClientBookingsAsync(Guid clientId);
        Task<IEnumerable<BookingDto>> GetWorkerBookingsAsync(Guid workerId);
        Task<bool> UpdateBookingStatusAsync(Guid bookingId, Guid accountId, UpdateBookingStatusDto request);
        Task<bool> CancelByClientAsync(Guid bookingId, Guid clientId);
        Task WorkerCancelAsync(Guid bookingId, Guid workerId, WorkerCancelBookingDto request);
        Task SwitchToCashAsync(Guid bookingId, Guid clientId);

        Task<int> CountRecentPlainCancelsAsync(Guid workerId);
        Task ReportBookingAsync(Guid bookingId, Guid actorId, ReportBookingDto request);

        Task<BookingDto?> ProposeRescheduleAsync(Guid bookingId, Guid actorId, ProposeRescheduleDto request);

        Task<BookingDto?> RespondRescheduleAsync(
            Guid bookingId, Guid requestId, Guid actorId, RescheduleAction action, bool isSystemActor = false);

        Task<IEnumerable<BookingDto>> GetAvailableBookingsAsync(Guid workerId);

        Task<bool> AcceptBookingAsync(Guid bookingId, Guid workerId);
        Task<bool> HideBookingAsync(Guid bookingId, Guid workerId);
        Task BroadcastBookingAsync(Guid bookingId);
        Task<IReadOnlyList<NearbyWorkerLocationDto>> GetNearbyOnlineWorkerLocationsAsync(Guid bookingId, Guid requestingClientId);

        Task<BookingDto?> GetBookingByIdAsync(Guid bookingId);
        Task<BookingDto?> GetBookingByIdAsync(Guid bookingId, Guid accountId);
        Task<IReadOnlyList<BookingPhotoDto>?> AddPhotosAsync(Guid bookingId, Guid accountId, IReadOnlyList<string> photoUrls);
    }
}

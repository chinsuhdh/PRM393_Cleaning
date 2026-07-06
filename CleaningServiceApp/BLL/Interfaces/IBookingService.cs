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

        Task<IEnumerable<BookingDto>> GetAvailableBookingsAsync(Guid workerId);

        Task<bool> AcceptBookingAsync(Guid bookingId, Guid workerId);
        Task<bool> HideBookingAsync(Guid bookingId, Guid workerId);
        Task BroadcastBookingAsync(Guid bookingId);

        Task<BookingDto?> GetBookingByIdAsync(Guid bookingId);
        Task<BookingDto?> GetBookingByIdAsync(Guid bookingId, Guid accountId);
        Task<IReadOnlyList<BookingPhotoDto>?> AddPhotosAsync(Guid bookingId, Guid accountId, IReadOnlyList<string> photoUrls);
    }
}

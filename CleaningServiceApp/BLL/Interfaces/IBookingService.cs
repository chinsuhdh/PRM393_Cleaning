using Cleaning.BLL.DTOs;

namespace Cleaning.BLL.Interfaces
{
    public interface IBookingService
    {
        Task<BookingDto> CreateBookingAsync(Guid clientId, CreateBookingDto request);
        Task<IEnumerable<BookingDto>> GetClientBookingsAsync(Guid clientId);
        Task<IEnumerable<BookingDto>> GetWorkerBookingsAsync(Guid workerId);
        Task<bool> UpdateBookingStatusAsync(Guid bookingId, Guid accountId, UpdateBookingStatusDto request);

        Task<IEnumerable<BookingDto>> GetAvailableBookingsAsync();

        Task<bool> AcceptBookingAsync(Guid bookingId, Guid workerId);
    }
}
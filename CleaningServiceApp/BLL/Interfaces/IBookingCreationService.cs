using Cleaning.BLL.DTOs;

namespace Cleaning.BLL.Interfaces;

public interface IBookingCreationService
{
    Task<BookingDto> CreateAsync(Guid clientId, string idempotencyKey, CreateBookingDto request);
    Task<PricingBreakdownDto> GetQuoteAsync(Guid clientId, BookingQuoteRequestDto request);
}

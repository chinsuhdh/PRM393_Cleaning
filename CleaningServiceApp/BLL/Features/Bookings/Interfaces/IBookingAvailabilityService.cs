using Cleaning.DAL.Entities;

namespace Cleaning.BLL.Features.Bookings;

public interface IBookingAvailabilityService
{
    Task<BookingAvailabilityDto> GetAsync(Guid clientId, BookingAvailabilityRequestDto request);
    Task<(Service Service, UserAddress Address)> ValidateAsync(Guid clientId, BookingAvailabilityRequestDto request);
}

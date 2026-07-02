using Cleaning.BLL.DTOs;
using Cleaning.DAL.Entities;

namespace Cleaning.BLL.Interfaces;

public interface IBookingAvailabilityService
{
    Task<BookingAvailabilityDto> GetAsync(Guid clientId, BookingAvailabilityRequestDto request);
    Task<(Service Service, UserAddress Address)> ValidateAsync(Guid clientId, BookingAvailabilityRequestDto request);
}

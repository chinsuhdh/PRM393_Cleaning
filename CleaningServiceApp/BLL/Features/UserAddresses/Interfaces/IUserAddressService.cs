
namespace Cleaning.BLL.Features.UserAddresses
{
    public interface IUserAddressService
    {
        Task<IEnumerable<UserAddressDto>> GetUserAddressesAsync(Guid userId);
        Task<UserAddressDto?> GetAddressByIdAsync(Guid addressId, Guid userId);
        Task<UserAddressDto> CreateAddressAsync(Guid userId, CreateUserAddressDto request);
        Task<bool> UpdateAddressAsync(Guid addressId, Guid userId, UpdateUserAddressDto request);
        Task<bool> DeleteAddressAsync(Guid addressId, Guid userId);
        Task<bool> SetDefaultAddressAsync(Guid addressId, Guid userId);
    }
}
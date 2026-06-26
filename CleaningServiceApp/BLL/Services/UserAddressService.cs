using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Interfaces;

namespace Cleaning.BLL.Services
{
    public class UserAddressService : IUserAddressService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserAddressService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<UserAddressDto>> GetUserAddressesAsync(Guid userId)
        {
            var addresses = await _unitOfWork.Repository<UserAddress>().FindAsync(a => a.UserId == userId);

            return addresses.Select(a => new UserAddressDto
            {
                Id = a.Id,
                UserId = a.UserId,
                Label = a.Label,
                AddressText = a.AddressText,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                PropertyType = a.PropertyType,
                IsDefault = a.IsDefault
            }).OrderByDescending(a => a.IsDefault).ToList();
        }

        public async Task<UserAddressDto?> GetAddressByIdAsync(Guid addressId, Guid userId)
        {
            var address = await _unitOfWork.Repository<UserAddress>()
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

            if (address == null) return null;

            return new UserAddressDto
            {
                Id = address.Id,
                UserId = address.UserId,
                Label = address.Label,
                AddressText = address.AddressText,
                Latitude = address.Latitude,
                Longitude = address.Longitude,
                PropertyType = address.PropertyType,
                IsDefault = address.IsDefault
            };
        }

        public async Task<UserAddressDto> CreateAddressAsync(Guid userId, CreateUserAddressDto request)
        {
            if (request.IsDefault)
            {
                await ResetDefaultAddressesAsync(userId);
            }

            var newAddress = new UserAddress
            {
                UserId = userId,
                Label = request.Label,
                AddressText = request.AddressText,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                PropertyType = request.PropertyType,
                IsDefault = request.IsDefault,
                CreatedAt = DateTime.UtcNow // Đảm bảo dùng UtcNow cho TIMESTAMPTZ
            };

            await _unitOfWork.Repository<UserAddress>().AddAsync(newAddress);
            await _unitOfWork.SaveChangesAsync();

            return new UserAddressDto
            {
                Id = newAddress.Id,
                UserId = newAddress.UserId,
                Label = newAddress.Label,
                AddressText = newAddress.AddressText,
                Latitude = newAddress.Latitude,
                Longitude = newAddress.Longitude,
                PropertyType = newAddress.PropertyType,
                IsDefault = newAddress.IsDefault
            };
        }

        public async Task<bool> UpdateAddressAsync(Guid addressId, Guid userId, UpdateUserAddressDto request)
        {
            var address = await _unitOfWork.Repository<UserAddress>()
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

            if (address == null) return false;

            // Nếu update địa chỉ này thành default, cần reset các default cũ
            if (request.IsDefault && !address.IsDefault)
            {
                await ResetDefaultAddressesAsync(userId);
            }

            address.Label = request.Label;
            address.AddressText = request.AddressText;
            address.Latitude = request.Latitude;
            address.Longitude = request.Longitude;
            address.PropertyType = request.PropertyType;
            address.IsDefault = request.IsDefault;

            _unitOfWork.Repository<UserAddress>().Update(address);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAddressAsync(Guid addressId, Guid userId)
        {
            var address = await _unitOfWork.Repository<UserAddress>()
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

            if (address == null) return false;

            _unitOfWork.Repository<UserAddress>().Remove(address);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> SetDefaultAddressAsync(Guid addressId, Guid userId)
        {
            var address = await _unitOfWork.Repository<UserAddress>()
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

            if (address == null) return false;

            // Nếu đã là default rồi thì không cần làm gì thêm
            if (address.IsDefault) return true;

            await ResetDefaultAddressesAsync(userId);

            address.IsDefault = true;
            _unitOfWork.Repository<UserAddress>().Update(address);

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private async Task ResetDefaultAddressesAsync(Guid userId)
        {
            var existingDefaults = await _unitOfWork.Repository<UserAddress>()
                .FindAsync(a => a.UserId == userId && a.IsDefault);

            foreach (var addr in existingDefaults)
            {
                addr.IsDefault = false;
                _unitOfWork.Repository<UserAddress>().Update(addr);
            }
        }
    }
}
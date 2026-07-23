using AutoMapper;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Interfaces;

namespace Cleaning.BLL.Features.UserAddresses
{
    public class UserAddressService : IUserAddressService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UserAddressService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<UserAddressDto>> GetUserAddressesAsync(Guid userId)
        {
            var addresses = await _unitOfWork.Repository<UserAddress>().FindAsync(a => a.UserId == userId);

            return addresses.Select(_mapper.Map<UserAddressDto>).OrderByDescending(a => a.IsDefault).ToList();
        }

        public async Task<UserAddressDto?> GetAddressByIdAsync(Guid addressId, Guid userId)
        {
            var address = await _unitOfWork.Repository<UserAddress>()
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

            return address == null ? null : _mapper.Map<UserAddressDto>(address);
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
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<UserAddress>().AddAsync(newAddress);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<UserAddressDto>(newAddress);
        }

        public async Task<bool> UpdateAddressAsync(Guid addressId, Guid userId, UpdateUserAddressDto request)
        {
            var address = await _unitOfWork.Repository<UserAddress>()
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

            if (address == null) return false;

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
            address.UpdatedAt = DateTime.UtcNow;

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

            if (address.IsDefault) return true;

            await ResetDefaultAddressesAsync(userId);

            address.IsDefault = true;
            address.UpdatedAt = DateTime.UtcNow;
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
                addr.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Repository<UserAddress>().Update(addr);
            }
        }
    }
}
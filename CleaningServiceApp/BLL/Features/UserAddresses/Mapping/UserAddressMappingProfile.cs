using Cleaning.DAL.Entities;

namespace Cleaning.BLL.Features.UserAddresses;

public sealed class UserAddressMappingProfile : AutoMapper.Profile
{
    public UserAddressMappingProfile()
    {
        CreateMap<UserAddress, UserAddressDto>();
    }
}

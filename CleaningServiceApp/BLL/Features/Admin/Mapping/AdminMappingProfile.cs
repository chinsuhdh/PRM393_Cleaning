using Cleaning.DAL.Entities;

namespace Cleaning.BLL.Features.Admin;

public sealed class AdminMappingProfile : AutoMapper.Profile
{
    public AdminMappingProfile()
    {
        CreateMap<WorkerApplication, WorkerApplicationDto>();

        CreateMap<Account, AccountAdminDto>()
            .ForMember(destination => destination.Email,
                options => options.MapFrom(source => source.Email ?? ""))
            .ForMember(destination => destination.FullName,
                options => options.MapFrom(source => source.Profile == null ? null : source.Profile.FullName))
            .ForMember(destination => destination.Role,
                options => options.MapFrom(source => source.Role.ToString()))
            .ForMember(destination => destination.Status,
                options => options.MapFrom(source => source.Status.ToString()));

        CreateMap<Booking, BookingAdminDto>()
            .ForMember(destination => destination.Status,
                options => options.MapFrom(source => source.Status.ToString()));
    }
}

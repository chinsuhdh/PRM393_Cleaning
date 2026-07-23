using Cleaning.BLL.Features.Admin;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;

namespace Cleaning.BLL.Features.ServiceCatalog;

public sealed class ServiceMappingProfile : AutoMapper.Profile
{
    public ServiceMappingProfile()
    {
        CreateMap<Service, ServiceDto>()
            .ForMember(destination => destination.PropertyType,
                options => options.MapFrom(source => source.PropertyType.ToString()))
            .ForMember(destination => destination.UnitType,
                options => options.MapFrom(source => source.UnitType.ToString()));

        CreateMap<CreateServiceDto, Service>()
            .ForMember(destination => destination.PropertyType,
                options => options.MapFrom(source => Enum.Parse<PropertyType>(source.PropertyType, true)))
            .ForMember(destination => destination.UnitType,
                options => options.MapFrom(source => Enum.Parse<ServiceUnitType>(source.UnitType, true)))
            .ForMember(destination => destination.Id, options => options.Ignore())
            .ForMember(destination => destination.IsActive, options => options.Ignore())
            .ForMember(destination => destination.CreatedAt, options => options.Ignore())
            .ForMember(destination => destination.UpdatedAt, options => options.Ignore())
            .ForMember(destination => destination.ArchivedAt, options => options.Ignore())
            .ForMember(destination => destination.Version, options => options.Ignore())
            .ForMember(destination => destination.Bookings, options => options.Ignore())
            .ForMember(destination => destination.WorkerServices, options => options.Ignore());
    }
}

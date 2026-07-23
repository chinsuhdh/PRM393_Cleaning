using AutoMapper;
using Cleaning.BLL.Common;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Cleaning.DAL.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cleaning.BLL.Features.ServiceCatalog
{

    public class ServiceCatalogService : IServiceCatalogService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ServiceCatalogService> _logger;

        private static readonly Guid ApartmentCategoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid HouseCategoryId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public ServiceCatalogService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ServiceCatalogService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public Task<IEnumerable<ServiceCategoryDto>> GetAllCategoriesAsync()
        {
            var categories = new List<ServiceCategoryDto>
            {
                new ServiceCategoryDto { Id = ApartmentCategoryId, Name = "Dọn dẹp Chung cư", SortOrder = 1 },
                new ServiceCategoryDto { Id = HouseCategoryId, Name = "Dọn dẹp Nhà phố", SortOrder = 2 }
            };

            return Task.FromResult<IEnumerable<ServiceCategoryDto>>(categories);
        }

        public async Task<IEnumerable<ServiceDto>> GetServicesByCategoryIdAsync(Guid categoryId)
        {
            PropertyType targetPropertyType;
            if (categoryId == ApartmentCategoryId)
                targetPropertyType = PropertyType.Apartment;
            else if (categoryId == HouseCategoryId)
                targetPropertyType = PropertyType.House;
            else
                throw new AppException(AppErrors.ServiceCategoryInvalid);

            var services = await _unitOfWork.Repository<Service>()
                .FindAsync(s => s.IsActive && s.PropertyType == targetPropertyType);

            return services.Select(_mapper.Map<ServiceDto>);
        }

        public async Task<ServiceDto?> GetServiceByIdAsync(Guid id)
        {
            var s = await _unitOfWork.Repository<Service>().GetByIdAsync(id);
            return s == null || !s.IsActive ? null : _mapper.Map<ServiceDto>(s);
        }
    }
}
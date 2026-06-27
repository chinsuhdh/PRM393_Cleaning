using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Interfaces;

namespace Cleaning.BLL.Services
{
    public class ServiceCatalogService : IServiceCatalogService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ServiceCatalogService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task<IEnumerable<ServiceCategoryDto>> GetAllCategoriesAsync()
        {
            var categories = new[]
            {
                new ServiceCategoryDto { Id = Guid.Empty, Name = "Apartment Cleaning", IconUrl = "business", SortOrder = 1 },
                new ServiceCategoryDto { Id = Guid.Empty, Name = "House Cleaning", IconUrl = "home", SortOrder = 2 }
            };

            return Task.FromResult<IEnumerable<ServiceCategoryDto>>(categories);
        }

        public async Task<IEnumerable<ServiceDto>> GetServicesByCategoryIdAsync(Guid categoryId)
        {
            var services = await _unitOfWork.Repository<Service>().FindAsync(s => s.IsActive);
            return services.Select(MapToDto);
        }

        public async Task<ServiceDto?> GetServiceByIdAsync(Guid id)
        {
            var service = await _unitOfWork.Repository<Service>().GetByIdAsync(id);
            return service == null || !service.IsActive ? null : MapToDto(service);
        }

        private static ServiceDto MapToDto(Service service)
        {
            return new ServiceDto
            {
                Id = service.Id,
                Name = service.Name,
                Description = service.Description,
                PropertyType = service.PropertyType.ToString(),
                UnitType = service.UnitType.ToString(),
                BasePrice = service.BasePrice,
                MinimumHours = service.MinimumHours,
                IsActive = service.IsActive
            };
        }
    }
}

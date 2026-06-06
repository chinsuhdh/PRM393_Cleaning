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

        public async Task<IEnumerable<ServiceCategoryDto>> GetAllCategoriesAsync()
        {
            var categories = await _unitOfWork.Repository<ServiceCategory>().GetAllAsync();

            return categories.OrderBy(c => c.SortOrder).Select(c => new ServiceCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                IconUrl = c.IconUrl,
                SortOrder = c.SortOrder
            });
        }

        public async Task<IEnumerable<ServiceDto>> GetServicesByCategoryIdAsync(Guid categoryId)
        {
            var services = await _unitOfWork.Repository<Service>().FindAsync(s => s.CategoryId == categoryId && s.IsActive);

            return services.Select(s => new ServiceDto
            {
                Id = s.Id,
                CategoryId = s.CategoryId,
                Name = s.Name,
                Description = s.Description,
                UnitType = s.UnitType.ToString(),
                BasePrice = s.BasePrice,
                IsActive = s.IsActive
            });
        }

        public async Task<ServiceDto?> GetServiceByIdAsync(Guid id)
        {
            var s = await _unitOfWork.Repository<Service>().GetByIdAsync(id);
            if (s == null || !s.IsActive) return null;

            return new ServiceDto
            {
                Id = s.Id,
                CategoryId = s.CategoryId,
                Name = s.Name,
                Description = s.Description,
                UnitType = s.UnitType.ToString(),
                BasePrice = s.BasePrice,
                IsActive = s.IsActive
            };
        }
    }
}
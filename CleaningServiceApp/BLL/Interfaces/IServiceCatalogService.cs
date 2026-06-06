using Cleaning.BLL.DTOs;

namespace Cleaning.BLL.Interfaces
{
    public interface IServiceCatalogService
    {
        Task<IEnumerable<ServiceCategoryDto>> GetAllCategoriesAsync();
        Task<IEnumerable<ServiceDto>> GetServicesByCategoryIdAsync(Guid categoryId);
        Task<ServiceDto?> GetServiceByIdAsync(Guid id);
    }
}
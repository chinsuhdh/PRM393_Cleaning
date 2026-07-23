
namespace Cleaning.BLL.Features.ServiceCatalog
{
    public interface IServiceCatalogService
    {
        Task<IEnumerable<ServiceCategoryDto>> GetAllCategoriesAsync();
        Task<IEnumerable<ServiceDto>> GetServicesByCategoryIdAsync(Guid categoryId);
        Task<ServiceDto?> GetServiceByIdAsync(Guid id);
    }
}
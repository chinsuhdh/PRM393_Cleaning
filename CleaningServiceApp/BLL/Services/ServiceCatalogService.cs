using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Cleaning.DAL.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cleaning.BLL.Services
{
    public class ServiceCatalogService : IServiceCatalogService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ServiceCatalogService> _logger;

        // Định nghĩa GUID tĩnh để giả lập Category, bảo vệ UI Frontend không bị vỡ
        private static readonly Guid ApartmentCategoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid HouseCategoryId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public ServiceCatalogService(IUnitOfWork unitOfWork, ILogger<ServiceCatalogService> logger)
        {
            _unitOfWork = unitOfWork;
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
            try
            {
                // Ánh xạ Fake Category ID thành Enum PropertyType thật dưới Database
                PropertyType targetPropertyType = categoryId == HouseCategoryId
                    ? PropertyType.House
                    : PropertyType.Apartment; // Fallback mặc định

                var services = await _unitOfWork.Repository<Service>()
                    .FindAsync(s => s.IsActive && s.PropertyType == targetPropertyType);

                return services.Select(MapToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi truy xuất danh sách dịch vụ cho CategoryId: {CategoryId}", categoryId);
                return new List<ServiceDto>(); // Trả về mảng rỗng thay vì làm sập App
            }
        }

        public async Task<ServiceDto?> GetServiceByIdAsync(Guid id)
        {
            var s = await _unitOfWork.Repository<Service>().GetByIdAsync(id);
            if (s == null || !s.IsActive) return null;

            return MapToDto(s);
        }

        private static ServiceDto MapToDto(Service s)
        {
            return new ServiceDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                PropertyType = s.PropertyType.ToString(),
                UnitType = s.UnitType.ToString(),
                BasePrice = s.BasePrice,
                MinimumHours = s.MinimumHours,
                IsActive = s.IsActive,
                BookingFormSchema = s.BookingFormSchema
            };
        }
    }
}
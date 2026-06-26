using Cleaning.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleaningService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous] // Cho phép người dùng vãng lai lướt xem dịch vụ
    public class ServiceCatalogController : ControllerBase
    {
        private readonly IServiceCatalogService _catalogService;

        public ServiceCatalogController(IServiceCatalogService catalogService)
        {
            _catalogService = catalogService;
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _catalogService.GetAllCategoriesAsync();
            return Ok(categories);
        }

        [HttpGet("categories/{categoryId}/services")]
        public async Task<IActionResult> GetServicesByCategory(Guid categoryId)
        {
            try
            {
                var services = await _catalogService.GetServicesByCategoryIdAsync(categoryId);
                return Ok(services);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi tải danh sách dịch vụ.", details = ex.Message });
            }
        }

        [HttpGet("services/{id}")]
        public async Task<IActionResult> GetServiceById(Guid id)
        {
            var service = await _catalogService.GetServiceByIdAsync(id);
            if (service == null) return NotFound(new { message = "Không tìm thấy dịch vụ hoặc dịch vụ đang tạm ngưng." });

            return Ok(service);
        }
    }
}
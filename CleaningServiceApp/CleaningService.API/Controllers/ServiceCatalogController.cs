using Cleaning.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleaningService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
            var services = await _catalogService.GetServicesByCategoryIdAsync(categoryId);
            return Ok(services);
        }

        [HttpGet("services/{id}")]
        public async Task<IActionResult> GetServiceById(Guid id)
        {
            var service = await _catalogService.GetServiceByIdAsync(id);
            if (service == null) return NotFound();

            return Ok(service);
        }
    }
}
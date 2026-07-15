using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CleaningService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Role string khớp chính xác với Enum UserRole.Admin.ToString() trong AuthService
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("dashboard-stats")]
        [ProducesResponseType(typeof(AdminDashboardStatsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDashboardStats()
        {
            var stats = await _adminService.GetDashboardStatsAsync();
            return Ok(stats);
        }

        // --- Worker Applications ---

        [HttpGet("worker-applications")]
        [ProducesResponseType(typeof(IEnumerable<WorkerApplicationDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetWorkerApplications()
        {
            var applications = await _adminService.GetWorkerApplicationsAsync();
            return Ok(applications);
        }

        [HttpPut("worker-applications/{id}/approve")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ApproveWorkerApplication(Guid id, [FromBody] ApproveWorkerApplicationDto dto)
        {
            var result = await _adminService.ApproveWorkerApplicationAsync(id, dto);
            if (!result) return BadRequest("Application not found or already processed.");
            return NoContent();
        }

        [HttpPut("worker-applications/{id}/reject")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RejectWorkerApplication(Guid id, [FromBody] RejectWorkerApplicationDto dto)
        {
            var result = await _adminService.RejectWorkerApplicationAsync(id, dto);
            if (!result) return BadRequest("Application not found or already processed.");
            return NoContent();
        }

        // --- Account Management ---

        [HttpGet("accounts")]
        [ProducesResponseType(typeof(IEnumerable<AccountAdminDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAccounts()
        {
            var accounts = await _adminService.GetAccountsAsync();
            return Ok(accounts);
        }

        [HttpPut("accounts/{id}/status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangeAccountStatus(Guid id, [FromBody] UpdateAccountStatusDto dto)
        {
            var result = await _adminService.ChangeAccountStatusAsync(id, dto);
            if (!result) return BadRequest("Account not found or invalid status.");
            return NoContent();
        }

        // --- Service Management ---

        [HttpGet("services")]
        [ProducesResponseType(typeof(IEnumerable<ServiceDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllServices()
        {
            var services = await _adminService.GetAllServicesAsync();
            return Ok(services);
        }

        [HttpPost("services")]
        [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateService([FromBody] CreateServiceDto dto)
        {
            var service = await _adminService.CreateServiceAsync(dto);
            return CreatedAtAction(nameof(GetDashboardStats), new { id = service.Id }, service);
        }

        [HttpPut("services/{id}")]
        [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateService(Guid id, [FromBody] UpdateServiceDto dto)
        {
            var service = await _adminService.UpdateServiceAsync(id, dto);
            if (service == null) return NotFound();
            return Ok(service);
        }

        [HttpDelete("services/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ArchiveService(Guid id)
        {
            var result = await _adminService.ArchiveServiceAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }

        // --- Booking Management ---

        [HttpGet("bookings")]
        [ProducesResponseType(typeof(IEnumerable<BookingAdminDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllBookings()
        {
            var bookings = await _adminService.GetAllBookingsAsync();
            return Ok(bookings);
        }
    }
}

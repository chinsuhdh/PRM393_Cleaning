using System.Security.Claims;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CleaningService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Worker")] // Áp dụng Authorization toàn bộ cho Controller này
    public class WorkersController : ControllerBase
    {
        private readonly IWorkerService _workerService;

        public WorkersController(IWorkerService workerService)
        {
            _workerService = workerService;
        }

        [HttpGet("me")]
        [ProducesResponseType(typeof(WorkerProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyWorkerProfile()
        {
            var workerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var profile = await _workerService.GetWorkerProfileAsync(workerId);

            if (profile == null) return NotFound(new { message = "Worker profile not found." });
            return Ok(profile);
        }

        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterWorkerProfile([FromBody] RegisterWorkerProfileDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var workerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _workerService.RegisterWorkerProfileAsync(workerId, request);

            if (!result) return BadRequest(new { message = "Worker profile already exists." });
            return Ok(new { message = "Worker profile registered successfully." });
        }

        [HttpPatch("location")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var workerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _workerService.UpdateLocationAsync(workerId, request);

            if (!result) return NotFound(new { message = "Worker profile not found." });
            return Ok(new { message = "Location updated." });
        }

        [HttpGet("me/skills")]
        [ProducesResponseType(typeof(IEnumerable<WorkerSkillDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMySkills()
        {
            var workerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var skills = await _workerService.GetWorkerSkillsAsync(workerId);
            return Ok(skills);
        }

        [HttpPost("me/skills")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddOrUpdateSkill([FromBody] WorkerSkillDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var workerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _workerService.AddOrUpdateWorkerSkillAsync(workerId, request);

            return Ok(new { message = "Skill updated successfully." });
        }
    }
}
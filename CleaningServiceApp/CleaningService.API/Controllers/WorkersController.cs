using System.Security.Claims;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleaningService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkersController : ControllerBase
    {
        private readonly IWorkerService _workerService;

        public WorkersController(IWorkerService workerService)
        {
            _workerService = workerService;
        }

        [HttpGet("me")]
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> GetMyWorkerProfile()
        {
            var workerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var profile = await _workerService.GetWorkerProfileAsync(workerId);

            if (profile == null) return NotFound();
            return Ok(profile);
        }

        [HttpPost("register")]
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> RegisterWorkerProfile([FromBody] RegisterWorkerProfileDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var workerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _workerService.RegisterWorkerProfileAsync(workerId, request);

            if (!result) return BadRequest(new { message = "Worker profile already exists." });
            return Ok(new { message = "Worker profile registered successfully." });
        }

        [HttpPatch("location")]
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var workerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _workerService.UpdateLocationAsync(workerId, request);

            if (!result) return NotFound();
            return Ok(new { message = "Location updated." });
        }

        [HttpGet("me/skills")]
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> GetMySkills()
        {
            var workerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var skills = await _workerService.GetWorkerSkillsAsync(workerId);
            return Ok(skills);
        }

        [HttpPost("me/skills")]
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> AddOrUpdateSkill([FromBody] WorkerSkillDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var workerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _workerService.AddOrUpdateWorkerSkillAsync(workerId, request);

            return Ok(new { message = "Skill updated successfully." });
        }
    }
}
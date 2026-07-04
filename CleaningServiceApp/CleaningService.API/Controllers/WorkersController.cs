using System.Security.Claims;
using Cleaning.BLL.Common;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using CleaningService.API.Common;
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

            if (profile == null) throw new AppException(AppErrors.WorkerProfileNotFound);
            return Ok(profile);
        }

        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterWorkerProfile([FromBody] RegisterWorkerProfileDto request)
        {
            var workerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _workerService.RegisterWorkerProfileAsync(workerId, request);

            if (!result) throw new AppException(AppErrors.WorkerProfileExists);
            return Ok(ApiResponse.Message(ResponseMessages.WorkerRegistered));
        }

        [HttpPatch("location")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationDto request)
        {
            var workerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _workerService.UpdateLocationAsync(workerId, request);

            if (!result) throw new AppException(AppErrors.WorkerProfileNotFound);
            return Ok(ApiResponse.Message(ResponseMessages.WorkerLocationUpdated));
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
            var workerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _workerService.AddOrUpdateWorkerSkillAsync(workerId, request);

            return Ok(ApiResponse.Message(ResponseMessages.WorkerSkillUpdated));
        }
    }
}

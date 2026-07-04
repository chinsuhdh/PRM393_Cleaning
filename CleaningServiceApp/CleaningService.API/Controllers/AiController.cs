using System.Security.Claims;
using Cleaning.BLL.Common;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using CleaningService.API.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleaningService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AiController : ControllerBase
    {
        private readonly IAiService _aiService;

        public AiController(IAiService aiService)
        {
            _aiService = aiService;
        }

        [HttpPost("chat")]
        [ProducesResponseType(typeof(ChatResponseDto), 200)]
        public async Task<IActionResult> ChatWithBot([FromBody] ChatRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                throw new AppException(AppErrors.AiMessageRequired);

            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _aiService.ChatWithRagAsync(accountId, request);
            return Ok(result);
        }

        [HttpPost("match-worker/{bookingId}")]
        [AllowAnonymous]
        public async Task<IActionResult> TriggerWorkerMatching(Guid bookingId)
        {
            var success = await _aiService.RecommendWorkerAsync(bookingId);
            if (!success) throw new AppException(AppErrors.AiMatchingFailed);
            return Ok(ApiResponse.Message(ResponseMessages.AiMatchingSuccess));
        }

        [HttpGet("recommended-workers/{bookingId}")]
        [AllowAnonymous] // Tạm thời để ẩn danh cho hệ thống tự động quét nếu cần
        [ProducesResponseType(typeof(List<WorkerDto>), 200)]
        public async Task<IActionResult> GetRecommendedWorkers(Guid bookingId)
        {
            var workers = await _aiService.GetRecommendedWorkersAsync(bookingId);
            return Ok(workers);
        }
    }
}

using Cleaning.BLL.Features.Ai;
using System.Security.Claims;
using Cleaning.BLL.Common;
using CleaningService.API.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CleaningService.API.Features.Ai
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
        [EnableRateLimiting(RateLimiterPolicies.AiChat)]
        [ProducesResponseType(typeof(ChatResponseDto), 200)]
        public async Task<IActionResult> ChatWithBot([FromBody] ChatRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                throw new AppException(AppErrors.AiMessageRequired);

            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _aiService.ChatWithRagAsync(accountId, request);
            return Ok(result);
        }

        [HttpGet("history/{sessionId}")]
        [ProducesResponseType(typeof(IReadOnlyList<AiChatMessageDto>), 200)]
        public async Task<IActionResult> GetHistory(string sessionId)
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var history = await _aiService.GetHistoryAsync(accountId, sessionId);
            return Ok(history);
        }

        [HttpDelete("history/{sessionId}")]
        public async Task<IActionResult> ClearHistory(string sessionId)
        {
            var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _aiService.ClearHistoryAsync(accountId, sessionId);
            return Ok(ApiResponse.Message("Đã xóa lịch sử trò chuyện."));
        }
    }
}

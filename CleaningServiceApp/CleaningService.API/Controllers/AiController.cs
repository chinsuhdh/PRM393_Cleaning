using System.Security.Claims;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
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
                return BadRequest(new { message = "Tin nhắn không được để trống." });

            try
            {
                var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _aiService.ChatWithRagAsync(accountId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Lỗi xử lý AI", details = ex.Message });
            }
        }

        [HttpPost("match-worker/{bookingId}")]
        [AllowAnonymous]
        public async Task<IActionResult> TriggerWorkerMatching(Guid bookingId)
        {
            var success = await _aiService.RecommendWorkerAsync(bookingId);
            if (!success) return BadRequest(new { message = "Không thể phân tích hoặc không tìm thấy thợ phù hợp." });
            return Ok(new { message = "Đã chạy thuật toán Matching thành công." });
        }

        [HttpGet("recommended-workers/{bookingId}")]
        [AllowAnonymous] // Tạm thời để ẩn danh cho hệ thống tự động quét nếu cần
        [ProducesResponseType(typeof(List<WorkerDto>), 200)]
        public async Task<IActionResult> GetRecommendedWorkers(Guid bookingId)
        {
            try
            {
                var workers = await _aiService.GetRecommendedWorkersAsync(bookingId);
                return Ok(workers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Lỗi truy xuất hệ thống Matching", details = ex.Message });
            }
        }
    }
}
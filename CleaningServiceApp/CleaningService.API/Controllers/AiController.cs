using System.Security.Claims;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleaningService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bắt buộc đăng nhập mới được chat
    public class AiController : ControllerBase
    {
        private readonly IAiService _aiService;

        public AiController(IAiService aiService)
        {
            _aiService = aiService;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> ChatWithBot([FromBody] ChatRequestDto request)
        {
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

        // API ẩn dành cho CronJob/Hệ thống gọi để Trigger Matching
        [HttpPost("match-worker/{bookingId}")]
        [AllowAnonymous]
        public async Task<IActionResult> TriggerWorkerMatching(Guid bookingId)
        {
            var success = await _aiService.RecommendWorkerAsync(bookingId);
            if (!success) return BadRequest("Không thể phân tích hoặc không tìm thấy thợ phù hợp.");
            return Ok(new { message = "Đã chạy AI Matching và lưu kết quả." });
        }
    }
}
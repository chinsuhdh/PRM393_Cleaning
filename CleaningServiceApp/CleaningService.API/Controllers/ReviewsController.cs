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
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var reviewerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var review = await _reviewService.CreateReviewAsync(reviewerId, request);
                return Ok(review);
            }
            catch (Exception)
            {
                return BadRequest(new { message = "Không thể lưu đánh giá. Bạn đã đánh giá đơn hàng này rồi." });
            }
        }

        [HttpGet("user/{userId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReviewsForUser(Guid userId)
        {
            var reviews = await _reviewService.GetReviewsForUserAsync(userId);
            return Ok(reviews);
        }
    }
}

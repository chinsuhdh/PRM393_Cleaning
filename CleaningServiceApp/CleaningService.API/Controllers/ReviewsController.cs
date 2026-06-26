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
    [Authorize]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var reviewerId))
                return Unauthorized();

            try
            {
                var review = await _reviewService.CreateReviewAsync(reviewerId, request);
                return Ok(review);
            }
            catch (ArgumentException ex)
            {
                // Xảy ra khi không tìm thấy Booking
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Xảy ra khi vi phạm Business Rules (booking chưa xong, tự review, review 2 lần...)
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("user/{userId}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<ReviewDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReviewsForUser(Guid userId)
        {
            var reviews = await _reviewService.GetReviewsForUserAsync(userId);
            return Ok(reviews);
        }
    }
}
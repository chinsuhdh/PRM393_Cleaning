using System.Security.Claims;
using Cleaning.BLL.Common;
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
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var reviewerId))
                throw new AppException(AppErrors.Unauthorized);

            try
            {
                var review = await _reviewService.CreateReviewAsync(reviewerId, request);
                return Ok(review);
            }
            catch (ArgumentException ex)
            {
                throw new AppException(new AppError(AppErrors.NotFound.Code, ex.Message, 404));
            }
            catch (InvalidOperationException ex)
            {
                throw new AppException(new AppError(AppErrors.ValidationError.Code, ex.Message, 400));
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

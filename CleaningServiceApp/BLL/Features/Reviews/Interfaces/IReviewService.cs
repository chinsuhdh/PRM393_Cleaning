
namespace Cleaning.BLL.Features.Reviews
{
    public interface IReviewService
    {
        Task<ReviewDto> CreateReviewAsync(Guid reviewerId, CreateReviewDto request);
        Task<IEnumerable<ReviewDto>> GetReviewsForUserAsync(Guid revieweeId);
    }
}
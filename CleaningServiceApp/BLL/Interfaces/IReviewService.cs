using Cleaning.BLL.DTOs;

namespace Cleaning.BLL.Interfaces
{
    public interface IReviewService
    {
        Task<ReviewDto> CreateReviewAsync(Guid reviewerId, CreateReviewDto request);
        Task<IEnumerable<ReviewDto>> GetReviewsForUserAsync(Guid revieweeId);
    }
}
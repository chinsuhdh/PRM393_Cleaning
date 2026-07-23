using Cleaning.DAL.Entities;

namespace Cleaning.BLL.Features.Reviews;

public sealed class ReviewMappingProfile : AutoMapper.Profile
{
    public ReviewMappingProfile()
    {
        CreateMap<Review, ReviewDto>();
    }
}

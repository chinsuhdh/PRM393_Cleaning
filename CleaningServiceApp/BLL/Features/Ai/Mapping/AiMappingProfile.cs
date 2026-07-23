using Cleaning.DAL.Entities;

namespace Cleaning.BLL.Features.Ai;

public sealed class AiMappingProfile : AutoMapper.Profile
{
    public AiMappingProfile()
    {
        CreateMap<AiMessage, AiChatMessageDto>()
            .ForMember(destination => destination.SenderType,
                options => options.MapFrom(source => source.SenderType.ToString()));
    }
}

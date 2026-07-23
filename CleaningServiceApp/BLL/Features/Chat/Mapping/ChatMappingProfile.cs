using Cleaning.DAL.Entities;

namespace Cleaning.BLL.Features.Chat;

public sealed class ChatMappingProfile : AutoMapper.Profile
{
    public ChatMappingProfile()
    {
        CreateMap<BookingMessage, BookingMessageDto>();
    }
}

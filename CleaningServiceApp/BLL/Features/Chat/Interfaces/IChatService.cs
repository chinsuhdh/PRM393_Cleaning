
namespace Cleaning.BLL.Features.Chat;

public interface IChatService
{
    Task<IEnumerable<BookingMessageDto>> GetMessagesAsync(Guid bookingId, Guid accountId);
    Task<BookingMessageDto> SendMessageAsync(Guid bookingId, Guid senderId, SendMessageDto request);
    Task MarkMessagesAsReadAsync(Guid bookingId, Guid accountId);
}

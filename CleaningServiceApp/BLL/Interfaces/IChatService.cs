using Cleaning.BLL.DTOs;

namespace Cleaning.BLL.Interfaces;

public interface IChatService
{
    Task<IEnumerable<BookingMessageDto>> GetMessagesAsync(Guid bookingId, Guid accountId);
    Task<BookingMessageDto> SendMessageAsync(Guid bookingId, Guid senderId, SendMessageDto request);
    Task MarkMessagesAsReadAsync(Guid bookingId, Guid accountId);
}

using Cleaning.BLL.Infrastructure.Dispatch;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Cleaning.BLL.Common;
using Cleaning.DAL.Data;
using Cleaning.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cleaning.BLL.Features.Chat;

public class ChatService(AppDbContext dbContext, IMapper mapper, IDispatchPublisher dispatchPublisher) : IChatService
{

    public async Task<IEnumerable<BookingMessageDto>> GetMessagesAsync(Guid bookingId, Guid accountId)
    {
        await EnsureParticipantAsync(bookingId, accountId);

        var messages = await dbContext.BookingMessages
            .Where(m => m.BookingId == bookingId)
            .OrderBy(m => m.CreatedAt)
            .ProjectTo<BookingMessageDto>(mapper.ConfigurationProvider)
            .ToListAsync();

        return messages;
    }

    public async Task<BookingMessageDto> SendMessageAsync(Guid bookingId, Guid senderId, SendMessageDto request)
    {
        await EnsureParticipantAsync(bookingId, senderId);

        var message = new BookingMessage
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            SenderId = senderId,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.BookingMessages.Add(message);
        await dbContext.SaveChangesAsync();

        var dto = mapper.Map<BookingMessageDto>(message);

        await dispatchPublisher.ChatMessageReceivedAsync(bookingId, dto);

        return dto;
    }

    public async Task MarkMessagesAsReadAsync(Guid bookingId, Guid accountId)
    {
        await EnsureParticipantAsync(bookingId, accountId);

        var unreadMessages = await dbContext.BookingMessages
            .Where(m => m.BookingId == bookingId && m.SenderId != accountId && m.ReadAt == null)
            .ToListAsync();

        if (unreadMessages.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var message in unreadMessages)
        {
            message.ReadAt = now;
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task EnsureParticipantAsync(Guid bookingId, Guid accountId)
    {
        var booking = await dbContext.Set<Booking>()
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
            throw new AppException(AppErrors.ChatNotFound);

        if (booking.ClientId != accountId && booking.WorkerId != accountId)
            throw new AppException(AppErrors.ChatForbidden);
    }
}

using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Data;
using Cleaning.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cleaning.BLL.Services;

public class ChatService(AppDbContext dbContext, IDispatchPublisher dispatchPublisher) : IChatService
{
    public async Task<IEnumerable<BookingMessageDto>> GetMessagesAsync(Guid bookingId, Guid accountId)
    {
        await EnsureParticipantAsync(bookingId, accountId);

        var messages = await dbContext.BookingMessages
            .Where(m => m.BookingId == bookingId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new BookingMessageDto
            {
                Id = m.Id,
                BookingId = m.BookingId,
                SenderId = m.SenderId,
                Content = m.Content,
                ReadAt = m.ReadAt,
                CreatedAt = m.CreatedAt
            })
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

        var dto = new BookingMessageDto
        {
            Id = message.Id,
            BookingId = message.BookingId,
            SenderId = message.SenderId,
            Content = message.Content,
            CreatedAt = message.CreatedAt,
            ReadAt = message.ReadAt
        };

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
            throw new ArgumentException("Booking not found");

        if (booking.ClientId != accountId && booking.WorkerId != accountId)
            throw new UnauthorizedAccessException("Only booking participants can access chat");
    }
}

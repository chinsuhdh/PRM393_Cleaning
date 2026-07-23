using Cleaning.BLL.Features.Chat;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleaningService.API.Features.Chat;

[ApiController]
[Route("api/bookings/{bookingId:guid}/messages")]
[Authorize(Roles = "Client,Worker")]
public class ChatController(IChatService chatService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMessages(Guid bookingId)
    {
        var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var messages = await chatService.GetMessagesAsync(bookingId, accountId);
        return Ok(messages);
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage(Guid bookingId, [FromBody] SendMessageDto request)
    {
        var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var message = await chatService.SendMessageAsync(bookingId, accountId, request);
        return Ok(message);
    }

    [HttpPost("read")]
    public async Task<IActionResult> MarkMessagesAsRead(Guid bookingId)
    {
        var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await chatService.MarkMessagesAsReadAsync(bookingId, accountId);
        return Ok(new { message = "OK" });
    }
}

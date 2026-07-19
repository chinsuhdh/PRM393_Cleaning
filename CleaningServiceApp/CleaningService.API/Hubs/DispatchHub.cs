using System.Security.Claims;
using Cleaning.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CleaningService.API.Hubs;

[Authorize]
public sealed class DispatchHub(IBookingService bookingService) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var accountId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (accountId != null && Context.User!.IsInRole("Worker"))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"worker:{accountId}");
        if (accountId != null && Context.User!.IsInRole("Client"))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"client:{accountId}");
        await base.OnConnectedAsync();
    }

    public async Task SubscribeBooking(Guid bookingId)
    {
        var accountId = Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var booking = await bookingService.GetBookingByIdAsync(bookingId, accountId);
        if (booking == null) throw new HubException("Booking not found.");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"booking:{bookingId}");
    }
}

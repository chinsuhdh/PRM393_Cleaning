using Cleaning.BLL.Features.Bookings;
using System.Security.Claims;
using CleaningService.API.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CleaningService.API.Infrastructure.Dispatch;

[Authorize]
public sealed class DispatchHub(IBookingService bookingService) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var accountId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (accountId != null && Context.User!.IsInRole("Worker"))
            await Groups.AddToGroupAsync(Context.ConnectionId, DispatchGroups.Worker(Guid.Parse(accountId)));
        if (accountId != null && Context.User!.IsInRole("Client"))
            await Groups.AddToGroupAsync(Context.ConnectionId, DispatchGroups.Client(Guid.Parse(accountId)));
        await base.OnConnectedAsync();
    }

    public async Task SubscribeBooking(Guid bookingId)
    {
        var accountId = Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var booking = await bookingService.GetBookingByIdAsync(bookingId, accountId);
        if (booking == null) throw new HubException("Booking not found.");
        await Groups.AddToGroupAsync(Context.ConnectionId, DispatchGroups.Booking(bookingId));
    }
}

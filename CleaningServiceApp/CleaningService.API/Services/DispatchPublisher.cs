using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using CleaningService.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CleaningService.API.Services;

public interface IWorkerPushSender
{
    Task SendJobPostedAsync(Guid workerId, BookingDto booking);
}

public sealed class NullWorkerPushSender : IWorkerPushSender
{
    public Task SendJobPostedAsync(Guid workerId, BookingDto booking) => Task.CompletedTask;
}

public sealed class DispatchPublisher(
    IHubContext<DispatchHub> hub,
    IWorkerPushSender pushSender) : IDispatchPublisher
{
    public async Task JobPostedAsync(BookingDto booking, IReadOnlyCollection<Guid> workerIds)
    {
        foreach (var workerId in workerIds)
        {
            await hub.Clients.Group($"worker:{workerId}").SendAsync("jobPosted", booking);
            await pushSender.SendJobPostedAsync(workerId, booking);
        }
    }

    public Task JobTakenAsync(Guid bookingId, IReadOnlyCollection<Guid> workerIds) =>
        SendToWorkersAsync("jobTaken", bookingId, workerIds);

    public Task JobCancelledAsync(Guid bookingId, IReadOnlyCollection<Guid> workerIds) =>
        SendToWorkersAsync("jobCancelled", bookingId, workerIds);

    public async Task BookingStatusChangedAsync(Guid bookingId, Guid clientId, string newStatus)
    {
        await hub.Clients.Group($"booking:{bookingId}").SendAsync("bookingStatusChanged", bookingId, newStatus);
        // Also reaches the client's always-joined client:{clientId} group, so the persistent active-
        // booking bar / booking list refresh live even when the client isn't on this booking's detail
        // screen (and thus never called SubscribeBooking for it).
        await hub.Clients.Group($"client:{clientId}").SendAsync("bookingStatusChanged", bookingId, newStatus);
    }

    public Task WorkerPositionAsync(Guid bookingId, decimal latitude, decimal longitude) =>
        hub.Clients.Group($"booking:{bookingId}").SendAsync("workerPosition", new
        {
            latitude,
            longitude,
            updatedAt = DateTime.UtcNow
        });

    public Task NearbyWorkerLocationsAsync(Guid bookingId, IReadOnlyList<NearbyWorkerLocationDto> locations) =>
        hub.Clients.Group($"booking:{bookingId}").SendAsync("nearbyWorkersUpdated", locations);

    public Task WorkerSuspendedAsync(Guid workerId) =>
        hub.Clients.Group($"worker:{workerId}").SendAsync("workerSuspended", workerId);

    private async Task SendToWorkersAsync(string eventName, Guid bookingId, IEnumerable<Guid> workerIds)
    {
        foreach (var workerId in workerIds)
            await hub.Clients.Group($"worker:{workerId}").SendAsync(eventName, bookingId);
    }
}

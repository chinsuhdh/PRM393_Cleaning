using Cleaning.BLL.DTOs;
using Cleaning.DAL.Enums;

namespace Cleaning.BLL.Tests;

public sealed partial class BookingDispatchTests
{
    [Fact]
    public async Task Create_DuplicateIdempotencyKey_ReturnsExistingBooking()
    {
        var scenario = DispatchScenario.Create();
        var first = await scenario.BookingService.CreateBookingAsync(
            scenario.ClientId, "dup-key", scenario.CreateRequest());
        var second = await scenario.BookingService.CreateBookingAsync(
            scenario.ClientId, "dup-key", scenario.CreateRequest());
        Assert.Equal(first.Id, second.Id);
        Assert.Single(scenario.Bookings);
    }

    [Fact]
    public async Task UpdateStatus_NonParticipant_ReturnsFalseAndLeavesStatusUnchanged()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);
        var updated = await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, Guid.NewGuid(), new UpdateBookingStatusDto { NewStatus = BookingStatus.Cancelled });
        Assert.False(updated);
        Assert.Equal(BookingStatus.Accepted, booking.Status);
    }

    [Fact]
    public async Task UpdateStatus_OwningClient_Succeeds()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.AwaitingWorker);
        var updated = await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, scenario.ClientId, new UpdateBookingStatusDto { NewStatus = BookingStatus.Cancelled });
        Assert.True(updated);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }

    [Fact]
    public async Task UpdateStatus_AssignedWorker_FollowsStateMachine()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);
        var updated = await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, scenario.WorkerId, new UpdateBookingStatusDto { NewStatus = BookingStatus.OnTheWay });
        Assert.True(updated);
        Assert.Equal(BookingStatus.OnTheWay, booking.Status);
    }

    [Theory]
    [InlineData(BookingStatus.AwaitingWorker, BookingStatus.Cancelled, "client")]
    [InlineData(BookingStatus.Accepted, BookingStatus.OnTheWay, "worker")]
    [InlineData(BookingStatus.OnTheWay, BookingStatus.InProgress, "worker")]
    [InlineData(BookingStatus.InProgress, BookingStatus.PendingPayment, "worker")]
    [InlineData(BookingStatus.PendingPayment, BookingStatus.Completed, "worker")]
    [InlineData(BookingStatus.Accepted, BookingStatus.RescheduleRequested, "client")]
    [InlineData(BookingStatus.Accepted, BookingStatus.RescheduleRequested, "worker")]
    [InlineData(BookingStatus.RescheduleRequested, BookingStatus.Accepted, "client")]
    [InlineData(BookingStatus.Accepted, BookingStatus.Cancelled, "client")]
    [InlineData(BookingStatus.RescheduleRequested, BookingStatus.Cancelled, "worker")]
    [InlineData(BookingStatus.OnTheWay, BookingStatus.Cancelled, "client")]
    [InlineData(BookingStatus.InProgress, BookingStatus.Cancelled, "worker")]
    [InlineData(BookingStatus.PendingPayment, BookingStatus.Cancelled, "client")]
    public async Task UpdateStatus_AllowedArrow_Succeeds(BookingStatus from, BookingStatus to, string actor)
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(from, workerId: from == BookingStatus.AwaitingWorker ? null : scenario.WorkerId);
        var actorId = actor == "client" ? scenario.ClientId : scenario.WorkerId;
        var updated = await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, actorId, new UpdateBookingStatusDto { NewStatus = to });
        Assert.True(updated);
        Assert.Equal(to, booking.Status);
        Assert.Single(scenario.StatusLogs,
            log => log.BookingId == booking.Id && log.OldStatus == from && log.NewStatus == to && log.ChangedBy == actorId);
    }

    [Theory]
    [InlineData(BookingStatus.Accepted, BookingStatus.InProgress, "worker")]
    [InlineData(BookingStatus.Accepted, BookingStatus.OnTheWay, "client")]
    [InlineData(BookingStatus.OnTheWay, BookingStatus.InProgress, "client")]
    [InlineData(BookingStatus.InProgress, BookingStatus.Completed, "worker")]
    [InlineData(BookingStatus.AwaitingWorker, BookingStatus.Accepted, "client")]
    [InlineData(BookingStatus.Completed, BookingStatus.Cancelled, "client")]
    [InlineData(BookingStatus.Cancelled, BookingStatus.Accepted, "worker")]
    [InlineData(BookingStatus.Accepted, BookingStatus.AwaitingWorker, "client")]
    public async Task UpdateStatus_ForbiddenArrow_ReturnsFalse(BookingStatus from, BookingStatus to, string actor)
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(from, workerId: from == BookingStatus.AwaitingWorker ? null : scenario.WorkerId);
        var actorId = actor == "client" ? scenario.ClientId : scenario.WorkerId;
        var updated = await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, actorId, new UpdateBookingStatusDto { NewStatus = to });
        Assert.False(updated);
        Assert.Equal(from, booking.Status);
        Assert.DoesNotContain(scenario.StatusLogs, log => log.BookingId == booking.Id);
    }
}

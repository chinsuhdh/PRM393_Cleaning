using Cleaning.DAL.Enums;

namespace Cleaning.BLL.Tests;

public sealed partial class BookingDispatchTests
{
    [Fact(DisplayName = "[UT-BOOK-ACC-01] Accepting an unassigned job assigns the worker and moves it to Accepted")]
    public async Task Accept_UnassignedAwaitingWorker_AssignsWorker()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.AwaitingWorker);

        var accepted = await scenario.BookingService.AcceptBookingAsync(booking.Id, scenario.WorkerId);

        Assert.True(accepted);
        Assert.Equal(scenario.WorkerId, booking.WorkerId);
        Assert.Equal(BookingStatus.Accepted, booking.Status);
        Assert.Single(scenario.StatusLogs, log => log.BookingId == booking.Id && log.NewStatus == BookingStatus.Accepted);
    }

    [Fact(DisplayName = "[UT-BOOK-ACC-02] A job already claimed by another worker cannot be accepted")]
    public async Task Accept_AlreadyAssigned_ReturnsFalse()
    {
        var scenario = DispatchScenario.Create();
        var otherWorker = Guid.NewGuid();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: otherWorker);

        var accepted = await scenario.BookingService.AcceptBookingAsync(booking.Id, scenario.WorkerId);

        Assert.False(accepted);
        Assert.Equal(otherWorker, booking.WorkerId);
    }

    [Fact(DisplayName = "[UT-BOOK-ACC-03] A job in a non-awaiting status cannot be accepted")]
    public async Task Accept_NonAwaitingStatus_ReturnsFalse()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Completed);

        var accepted = await scenario.BookingService.AcceptBookingAsync(booking.Id, scenario.WorkerId);

        Assert.False(accepted);
        Assert.Null(booking.WorkerId);
    }

    [Fact(DisplayName = "[UT-BOOK-ACC-04] Accepting a non-existent job returns false")]
    public async Task Accept_MissingBooking_ReturnsFalse()
    {
        var scenario = DispatchScenario.Create();

        var accepted = await scenario.BookingService.AcceptBookingAsync(Guid.NewGuid(), scenario.WorkerId);

        Assert.False(accepted);
    }

    [Fact(DisplayName = "[UT-BOOK-ACC-05] Two workers racing for the same job: only the first succeeds")]
    public async Task Accept_SecondWorkerAfterFirst_ReturnsFalse()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.AwaitingWorker);
        var firstWorker = scenario.WorkerId;
        var secondWorker = Guid.NewGuid();

        var first = await scenario.BookingService.AcceptBookingAsync(booking.Id, firstWorker);
        var second = await scenario.BookingService.AcceptBookingAsync(booking.Id, secondWorker);

        Assert.True(first);
        Assert.False(second);
        Assert.Equal(firstWorker, booking.WorkerId);
    }

    [Fact(DisplayName = "[UT-BOOK-ACC-06] Accepting a job that time-overlaps the worker's own accepted booking is rejected")]
    public async Task Accept_OverlapsOwnAcceptedBooking_ReturnsFalse()
    {
        var scenario = DispatchScenario.Create();
        var start = DateTime.UtcNow.AddHours(3);
        scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId, start: start, durationHours: 2);
        var overlapping = scenario.AddBooking(BookingStatus.AwaitingWorker, start: start.AddHours(1), durationHours: 2);

        var accepted = await scenario.BookingService.AcceptBookingAsync(overlapping.Id, scenario.WorkerId);

        Assert.False(accepted);
        Assert.Null(overlapping.WorkerId);
    }

    [Fact(DisplayName = "[UT-BOOK-ACC-07] Accepting a job that does not overlap the worker's own accepted booking succeeds")]
    public async Task Accept_NoOverlapWithOwnBooking_Succeeds()
    {
        var scenario = DispatchScenario.Create();
        var start = DateTime.UtcNow.AddHours(3);
        scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId, start: start, durationHours: 2);
        var later = scenario.AddBooking(BookingStatus.AwaitingWorker, start: start.AddHours(3), durationHours: 2);

        var accepted = await scenario.BookingService.AcceptBookingAsync(later.Id, scenario.WorkerId);

        Assert.True(accepted);
        Assert.Equal(scenario.WorkerId, later.WorkerId);
    }

    [Fact(DisplayName = "[UT-BOOK-ACC-08] A Busy worker can still accept a job that doesn't overlap their current one")]
    public async Task Accept_BusyWorkerNoOverlap_Succeeds()
    {
        var scenario = DispatchScenario.Create(workerOnlineStatus: WorkerOnlineStatus.Busy);
        var start = DateTime.UtcNow.AddHours(3);
        scenario.AddBooking(BookingStatus.InProgress, workerId: scenario.WorkerId, start: start, durationHours: 2);
        var later = scenario.AddBooking(
            BookingStatus.AwaitingWorker, bookingType: BookingType.Immediate, start: start.AddHours(3), durationHours: 2);

        var accepted = await scenario.BookingService.AcceptBookingAsync(later.Id, scenario.WorkerId);

        Assert.True(accepted);
    }
}

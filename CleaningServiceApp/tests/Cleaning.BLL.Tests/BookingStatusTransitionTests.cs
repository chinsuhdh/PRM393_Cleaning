using Cleaning.BLL.Features.Bookings;
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

    [Fact(DisplayName = "[UT-BOOK-STS-09] Client cancelling a pre-accept AwaitingWorker booking notifies "
        + "the workers who had it in their feed, so it disappears there too")]
    public async Task UpdateStatus_ClientCancelsAwaitingWorker_NotifiesEligibleWorkers()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.AwaitingWorker, bookingType: BookingType.Immediate);

        var updated = await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, scenario.ClientId, new UpdateBookingStatusDto { NewStatus = BookingStatus.Cancelled });

        Assert.True(updated);
        var recipients = Assert.Single(scenario.DispatchPublisher.CancelledRecipients);
        Assert.Contains(scenario.WorkerId, recipients);
    }

    [Fact(DisplayName = "[UT-BOOK-STS-10] Reporting/cancelling an already-accepted booking notifies "
        + "specifically the assigned worker, so their own My Jobs / active-job view updates live")]
    public async Task UpdateStatus_CancelAlreadyAcceptedBooking_NotifiesAssignedWorker()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.InProgress, workerId: scenario.WorkerId);

        var updated = await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, scenario.WorkerId,
            new UpdateBookingStatusDto { NewStatus = BookingStatus.Cancelled, Reason = "Khach vang mat" });

        Assert.True(updated);
        var recipients = Assert.Single(scenario.DispatchPublisher.CancelledRecipients);
        Assert.Equal([scenario.WorkerId], recipients);
    }

    [Fact(DisplayName = "[UT-BOOK-DTL-01] Once a worker is assigned, booking detail exposes their "
        + "name, rating, and current position (needed for the worker card + OnTheWay live map)")]
    public async Task GetBookingById_WorkerAssigned_ExposesWorkerSummary()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);

        var dto = await scenario.BookingService.GetBookingByIdAsync(booking.Id, scenario.ClientId);

        Assert.NotNull(dto!.Worker);
        Assert.Equal(scenario.WorkerId, dto.Worker!.Id);
        Assert.Equal("Anh Ba", dto.Worker.Name);
        Assert.Equal(10.7769m, dto.Worker.Latitude);
        Assert.Equal(106.7009m, dto.Worker.Longitude);
    }

    [Fact(DisplayName = "[UT-BOOK-DTL-02] An unassigned AwaitingWorker booking exposes no worker "
        + "info — candidate workers are never shown to the client during broadcast")]
    public async Task GetBookingById_Unassigned_ExposesNoWorker()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.AwaitingWorker);

        var dto = await scenario.BookingService.GetBookingByIdAsync(booking.Id, scenario.ClientId);

        Assert.Null(dto!.Worker);
    }

    [Fact(DisplayName = "[UT-BOOK-STS-06] Worker plain-cancel releases the job: WorkerId cleared and re-broadcast (§ 4.10)")]
    public async Task UpdateStatus_WorkerReleasesJob_ClearsWorkerAndRebroadcasts()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);

        var updated = await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, scenario.WorkerId, new UpdateBookingStatusDto { NewStatus = BookingStatus.AwaitingWorker });

        Assert.True(updated);
        Assert.Equal(BookingStatus.AwaitingWorker, booking.Status);
        Assert.Null(booking.WorkerId);
        // The released job must show up again in the broadcast feed for eligible workers.
        var available = await scenario.BookingService.GetAvailableBookingsAsync(scenario.WorkerId);
        Assert.Single(available, dto => dto.Id == booking.Id);
    }

    [Fact(DisplayName = "[UT-BOOK-STS-07] Start and finish stamp ActualStartTime and ActualEndTime")]
    public async Task UpdateStatus_StartAndFinish_StampActualTimes()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.OnTheWay, workerId: scenario.WorkerId);

        await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, scenario.WorkerId, new UpdateBookingStatusDto { NewStatus = BookingStatus.InProgress });
        Assert.NotNull(booking.ActualStartTime);
        Assert.Null(booking.ActualEndTime);

        await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, scenario.WorkerId, new UpdateBookingStatusDto { NewStatus = BookingStatus.PendingPayment });
        Assert.NotNull(booking.ActualEndTime);
    }

    [Fact(DisplayName = "[UT-BOOK-STS-08] A report-cancel from InProgress records who cancelled and why")]
    public async Task UpdateStatus_ReportCancel_WritesCancellationRecord()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.InProgress, workerId: scenario.WorkerId);

        var updated = await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, scenario.WorkerId,
            new UpdateBookingStatusDto { NewStatus = BookingStatus.Cancelled, Reason = "Khach vang mat" });

        Assert.True(updated);
        var record = Assert.Single(scenario.Cancellations);
        Assert.Equal(booking.Id, record.BookingId);
        Assert.Equal(scenario.WorkerId, record.CancelledBy);
        Assert.Equal(UserRole.Worker, record.ActorRole);
        Assert.Equal("Khach vang mat", record.Reason);
    }

    [Fact(DisplayName = "[UT-BOOK-STS-11] Accepting a booking pushes a booking-scoped status-changed event, " +
        "so the client's Booking Detail live-updates without waiting on a poll — and carries the " +
        "booking's ClientId, so the client's always-joined client:{clientId} group also gets it (the " +
        "active-booking bar/list live-update without that specific booking's detail screen open)")]
    public async Task AcceptBooking_PublishesBookingStatusChanged()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.AwaitingWorker, bookingType: BookingType.Immediate);

        await scenario.BookingService.AcceptBookingAsync(booking.Id, scenario.WorkerId);

        Assert.Contains(scenario.DispatchPublisher.StatusChanges, change =>
            change.BookingId == booking.Id &&
            change.ClientId == scenario.ClientId &&
            change.NewStatus == nameof(BookingStatus.Accepted));
    }

    [Fact(DisplayName = "[UT-BOOK-STS-12] Every allowed status transition pushes a booking-scoped " +
        "status-changed event, not just Accept/Cancel")]
    public async Task UpdateStatus_PublishesBookingStatusChanged()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.OnTheWay, workerId: scenario.WorkerId);

        await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, scenario.WorkerId, new UpdateBookingStatusDto { NewStatus = BookingStatus.InProgress });

        Assert.Contains(scenario.DispatchPublisher.StatusChanges, change =>
            change.BookingId == booking.Id && change.NewStatus == nameof(BookingStatus.InProgress));
    }
}

using Cleaning.BLL.Services;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;

namespace Cleaning.BLL.Tests;

public sealed partial class BookingDispatchTests
{
    private static BookingSweepService CreateSweepService(DispatchScenario scenario) =>
        new(scenario.UnitOfWork, scenario.BookingService, scenario.DispatchPublisher);

    [Fact(DisplayName = "[UT-SWEEP-01] A Scheduled AwaitingWorker booking within 1 hour of its start is auto-cancelled")]
    public async Task RunTick_ScheduledBookingWithinOneHour_AutoCancelled()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.AwaitingWorker,
            bookingType: BookingType.Scheduled, start: DateTime.UtcNow.AddMinutes(45));
        var sweeper = CreateSweepService(scenario);

        await sweeper.RunTickAsync();

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }

    [Fact(DisplayName = "[UT-SWEEP-02] Immediate bookings are never touched by the unanswered-scheduled sweep")]
    public async Task RunTick_ImmediateBooking_Untouched()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.AwaitingWorker,
            bookingType: BookingType.Immediate, start: DateTime.UtcNow.AddMinutes(10));
        var sweeper = CreateSweepService(scenario);

        await sweeper.RunTickAsync();

        Assert.Equal(BookingStatus.AwaitingWorker, booking.Status);
    }

    [Fact(DisplayName = "[UT-SWEEP-03] A Scheduled AwaitingWorker booking more than 1 hour out is left untouched")]
    public async Task RunTick_ScheduledBookingNotYetDue_Untouched()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.AwaitingWorker,
            bookingType: BookingType.Scheduled, start: DateTime.UtcNow.AddHours(5));
        var sweeper = CreateSweepService(scenario);

        await sweeper.RunTickAsync();

        Assert.Equal(BookingStatus.AwaitingWorker, booking.Status);
    }

    [Fact(DisplayName = "[UT-SWEEP-04] An expired pending reschedule request is auto-rejected")]
    public async Task RunTick_ExpiredReschedule_AutoRejected()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.RescheduleRequested, workerId: scenario.WorkerId);
        var request = new BookingRescheduleRequest
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            RequestedBy = scenario.ClientId,
            OldStartTime = booking.ScheduledStartTime,
            OldEndTime = booking.ScheduledEndTime,
            NewStartTime = booking.ScheduledStartTime.AddDays(1),
            NewEndTime = booking.ScheduledEndTime.AddDays(1),
            Status = RescheduleStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        };
        scenario.RescheduleRequests.Add(request);
        var sweeper = CreateSweepService(scenario);

        await sweeper.RunTickAsync();

        Assert.Equal(RescheduleStatus.Rejected, request.Status);
        Assert.Null(request.RespondedBy);
        Assert.Equal(BookingStatus.Accepted, booking.Status);
    }

    [Fact(DisplayName = "[UT-SWEEP-05] A pending reschedule request not yet expired is left untouched")]
    public async Task RunTick_RescheduleNotYetExpired_Untouched()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.RescheduleRequested, workerId: scenario.WorkerId);
        var request = new BookingRescheduleRequest
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            RequestedBy = scenario.ClientId,
            OldStartTime = booking.ScheduledStartTime,
            OldEndTime = booking.ScheduledEndTime,
            NewStartTime = booking.ScheduledStartTime.AddDays(1),
            NewEndTime = booking.ScheduledEndTime.AddDays(1),
            Status = RescheduleStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        scenario.RescheduleRequests.Add(request);
        var sweeper = CreateSweepService(scenario);

        await sweeper.RunTickAsync();

        Assert.Equal(RescheduleStatus.Pending, request.Status);
        Assert.Equal(BookingStatus.RescheduleRequested, booking.Status);
    }

    [Fact(DisplayName = "[UT-SWEEP-06] Running the tick twice in a row is idempotent")]
    public async Task RunTick_TwiceInARow_IsIdempotent()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.AwaitingWorker,
            bookingType: BookingType.Scheduled, start: DateTime.UtcNow.AddMinutes(30));
        var sweeper = CreateSweepService(scenario);

        await sweeper.RunTickAsync();
        await sweeper.RunTickAsync();

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Single(scenario.Cancellations);
    }

    [Fact(DisplayName = "[UT-SWEEP-07] The safety net suspends a worker whose recent cancels already reached the threshold")]
    public async Task RunTick_WorkerOverThreshold_SuspendedBySafetyNet()
    {
        var scenario = DispatchScenario.Create();
        for (var i = 0; i < 3; i++)
        {
            scenario.Cancellations.Add(new BookingCancellation
            {
                Id = Guid.NewGuid(),
                BookingId = Guid.NewGuid(),
                CancelledBy = scenario.WorkerId,
                ActorRole = UserRole.Worker,
                ReasonCode = "worker_cancel.too_far",
                Reason = "Địa chỉ quá xa",
                CreatedAt = DateTime.UtcNow
            });
        }
        var sweeper = CreateSweepService(scenario);

        await sweeper.RunTickAsync();

        Assert.NotNull(scenario.Worker.SuspendedAt);
        Assert.Equal(WorkerOnlineStatus.Offline, scenario.Worker.OnlineStatus);
        Assert.Contains(scenario.WorkerId, scenario.DispatchPublisher.SuspendedWorkerIds);
    }
}

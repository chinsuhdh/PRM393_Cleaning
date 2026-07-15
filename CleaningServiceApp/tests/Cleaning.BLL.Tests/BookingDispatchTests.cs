using Cleaning.BLL.Constants;
using Cleaning.DAL.Enums;

namespace Cleaning.BLL.Tests;

public sealed partial class BookingDispatchTests
{
    [Fact(DisplayName = "[UT-BOOK-DSP-01] Dispatch surfaces unassigned AwaitingWorker jobs for a verified service")]
    public async Task GetAvailable_UnassignedAwaitingWorker_ForVerifiedService_IsSurfaced()
    {
        var scenario = DispatchScenario.Create();
        scenario.AddBooking(BookingStatus.AwaitingWorker);

        var available = await scenario.BookingService.GetAvailableBookingsAsync(scenario.WorkerId);

        var dto = Assert.Single(available);
        Assert.Equal(scenario.ServiceEntity.Id, dto.ServiceId);
        Assert.Null(dto.WorkerId);
    }

    [Fact(DisplayName = "[UT-BOOK-DSP-01b] EstimatedMinutes is populated alongside DistanceKm, computed from the same distance")]
    public async Task GetAvailable_SetsEstimatedMinutes_ConsistentWithDistanceKm()
    {
        var scenario = DispatchScenario.Create();
        scenario.AddBooking(BookingStatus.AwaitingWorker);

        var available = await scenario.BookingService.GetAvailableBookingsAsync(scenario.WorkerId);

        var dto = Assert.Single(available);
        Assert.NotNull(dto.DistanceKm);
        Assert.NotNull(dto.EstimatedMinutes);
        var expectedMinutes = (decimal)GeoConstants.EstimatedMinutes((double)dto.DistanceKm!.Value);
        Assert.Equal(expectedMinutes, dto.EstimatedMinutes!.Value);
    }

    [Fact(DisplayName = "[UT-BOOK-DSP-02] Dispatch does not surface post-job PendingPayment bookings")]
    public async Task GetAvailable_PendingPayment_IsHidden()
    {
        var scenario = DispatchScenario.Create();
        scenario.AddBooking(BookingStatus.PendingPayment);

        var available = await scenario.BookingService.GetAvailableBookingsAsync(scenario.WorkerId);

        Assert.Empty(available);
    }

    [Fact(DisplayName = "[UT-BOOK-DSP-03] Dispatch hides jobs the worker is not verified to perform")]
    public async Task GetAvailable_ServiceNotVerified_IsHidden()
    {
        var scenario = DispatchScenario.Create();
        scenario.AddBooking(BookingStatus.AwaitingWorker, serviceId: Guid.NewGuid());

        var available = await scenario.BookingService.GetAvailableBookingsAsync(scenario.WorkerId);

        Assert.Empty(available);
    }

    [Fact(DisplayName = "[UT-BOOK-DSP-04] Dispatch hides jobs already claimed by a worker")]
    public async Task GetAvailable_AlreadyAssigned_IsHidden()
    {
        var scenario = DispatchScenario.Create();
        scenario.AddBooking(BookingStatus.Accepted, workerId: Guid.NewGuid());

        var available = await scenario.BookingService.GetAvailableBookingsAsync(scenario.WorkerId);

        Assert.Empty(available);
    }

    [Fact(DisplayName = "[UT-BOOK-DSP-05] Dispatch hides jobs whose skill is not yet verified for this worker")]
    public async Task GetAvailable_SkillPendingVerification_IsHidden()
    {
        var scenario = DispatchScenario.Create(workerSkillVerified: false);
        scenario.AddBooking(BookingStatus.AwaitingWorker);

        var available = await scenario.BookingService.GetAvailableBookingsAsync(scenario.WorkerId);

        Assert.Empty(available);
    }

    [Fact(DisplayName = "[UT-BOOK-DSP-06] A Busy worker still sees available immediate jobs (only Offline hides them)")]
    public async Task GetAvailable_WorkerBusy_ImmediateJobStillSurfaced()
    {
        var scenario = DispatchScenario.Create(workerOnlineStatus: WorkerOnlineStatus.Busy);
        scenario.AddBooking(BookingStatus.AwaitingWorker, bookingType: BookingType.Immediate);

        var available = await scenario.BookingService.GetAvailableBookingsAsync(scenario.WorkerId);

        Assert.Single(available);
    }

    [Fact(DisplayName = "[UT-BOOK-DSP-07] An Offline worker does not see available immediate jobs")]
    public async Task GetAvailable_WorkerOffline_ImmediateJobHidden()
    {
        var scenario = DispatchScenario.Create(workerOnlineStatus: WorkerOnlineStatus.Offline);
        scenario.AddBooking(BookingStatus.AwaitingWorker, bookingType: BookingType.Immediate);

        var available = await scenario.BookingService.GetAvailableBookingsAsync(scenario.WorkerId);

        Assert.Empty(available);
    }

    [Fact(DisplayName = "[UT-BOOK-DSP-08] A Scheduled job overlapping the worker's own accepted booking is " +
        "hidden from browse — E.1 calendar/overlap eligibility applies to Scheduled, not just Accept-time")]
    public async Task GetAvailable_ScheduledOverlapsOwnAcceptedBooking_IsHidden()
    {
        var scenario = DispatchScenario.Create();
        var start = DateTime.UtcNow.AddHours(3);
        scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId, start: start, durationHours: 2);
        scenario.AddBooking(BookingStatus.AwaitingWorker, start: start.AddHours(1), durationHours: 2);

        var available = await scenario.BookingService.GetAvailableBookingsAsync(scenario.WorkerId);

        Assert.Empty(available);
    }

    [Fact(DisplayName = "[UT-BOOK-DSP-08b] An Immediate job overlapping the worker's own accepted booking is " +
        "still visible to browse — E.1 marks overlap as ignored for Immediate (only Accept-time is blocked)")]
    public async Task GetAvailable_ImmediateOverlapsOwnAcceptedBooking_StillSurfaced()
    {
        var scenario = DispatchScenario.Create();
        var start = DateTime.UtcNow.AddHours(3);
        scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId, start: start, durationHours: 2);
        var overlapping = scenario.AddBooking(
            BookingStatus.AwaitingWorker, bookingType: BookingType.Immediate, start: start.AddHours(1), durationHours: 2);

        var available = await scenario.BookingService.GetAvailableBookingsAsync(scenario.WorkerId);

        Assert.Single(available, dto => dto.Id == overlapping.Id);
    }

    [Fact(DisplayName = "[UT-BOOK-DSP-09] Broadcasting (first post or a retry) bumps UpdatedAt, so the " +
        "client's search countdown restarts instead of staying anchored to the original CreatedAt")]
    public async Task BroadcastBooking_BumpsUpdatedAt()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.AwaitingWorker, bookingType: BookingType.Immediate);
        booking.UpdatedAt = DateTime.UtcNow.AddMinutes(-10);
        var staleUpdatedAt = booking.UpdatedAt;

        await scenario.BookingService.BroadcastBookingAsync(booking.Id);

        Assert.True(booking.UpdatedAt > staleUpdatedAt);
        Assert.True(booking.UpdatedAt > DateTime.UtcNow.AddSeconds(-5));
    }
}

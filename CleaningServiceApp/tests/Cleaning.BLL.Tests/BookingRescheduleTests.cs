using Cleaning.BLL.Common;
using Cleaning.BLL.DTOs;
using Cleaning.DAL.Enums;

namespace Cleaning.BLL.Tests;

public sealed partial class BookingDispatchTests
{
    private static DateTime ValidNewStart() =>
        RoundToSlot(DateTime.UtcNow.AddHours(5));

    private static DateTime RoundToSlot(DateTime value) =>
        value.AddMinutes(-(value.Minute % 30)).AddSeconds(-value.Second).AddMilliseconds(-value.Millisecond);

    [Fact(DisplayName = "[UT-BOOK-RSC-01] Proposing a reschedule on an accepted scheduled booking succeeds")]
    public async Task ProposeReschedule_AcceptedScheduledBooking_Succeeds()
    {
        var scenario = DispatchScenario.Create();
        var start = DateTime.UtcNow.AddHours(10);
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId, start: start);

        var dto = await scenario.BookingService.ProposeRescheduleAsync(
            booking.Id, scenario.ClientId, new ProposeRescheduleDto { NewStartTime = ValidNewStart() });

        Assert.NotNull(dto);
        Assert.Equal(nameof(BookingStatus.RescheduleRequested), dto!.Status);
        Assert.NotNull(dto.PendingReschedule);
        Assert.Equal(scenario.ClientId, dto.PendingReschedule!.RequestedBy);
    }

    [Fact(DisplayName = "[UT-BOOK-RSC-02] Proposing a reschedule on an Immediate booking is rejected")]
    public async Task ProposeReschedule_ImmediateBooking_Throws()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId, bookingType: BookingType.Immediate);

        await Assert.ThrowsAsync<AppException>(() => scenario.BookingService.ProposeRescheduleAsync(
            booking.Id, scenario.ClientId, new ProposeRescheduleDto { NewStartTime = ValidNewStart() }));
    }

    [Fact(DisplayName = "[UT-BOOK-RSC-03] Proposing a reschedule on a non-Accepted booking is rejected")]
    public async Task ProposeReschedule_WrongStatus_Throws()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.OnTheWay, workerId: scenario.WorkerId);

        await Assert.ThrowsAsync<AppException>(() => scenario.BookingService.ProposeRescheduleAsync(
            booking.Id, scenario.WorkerId, new ProposeRescheduleDto { NewStartTime = ValidNewStart() }));
    }

    [Fact(DisplayName = "[UT-BOOK-RSC-04] A second reschedule proposal while one is already pending is rejected")]
    public async Task ProposeReschedule_AlreadyPending_Throws()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId, start: DateTime.UtcNow.AddHours(10));
        await scenario.BookingService.ProposeRescheduleAsync(
            booking.Id, scenario.ClientId, new ProposeRescheduleDto { NewStartTime = ValidNewStart() });
        // Proposing again requires the booking to still look Accepted-eligible; simulate a second
        // attempt while the first is still Pending by resetting status back (the real UI wouldn't
        // allow this, but the server-side guard must hold regardless).
        booking.Status = BookingStatus.Accepted;

        await Assert.ThrowsAsync<AppException>(() => scenario.BookingService.ProposeRescheduleAsync(
            booking.Id, scenario.WorkerId, new ProposeRescheduleDto { NewStartTime = ValidNewStart().AddHours(1) }));
    }

    [Fact(DisplayName = "[UT-BOOK-RSC-05] A proposed time under the 2-hour lead is rejected")]
    public async Task ProposeReschedule_TooSoon_Throws()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId, start: DateTime.UtcNow.AddHours(10));

        await Assert.ThrowsAsync<AppException>(() => scenario.BookingService.ProposeRescheduleAsync(
            booking.Id, scenario.ClientId, new ProposeRescheduleDto { NewStartTime = DateTime.UtcNow.AddMinutes(30) }));
    }

    [Fact(DisplayName = "[UT-BOOK-RSC-06] A proposed time beyond 30 days is rejected")]
    public async Task ProposeReschedule_TooFarAhead_Throws()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId, start: DateTime.UtcNow.AddHours(10));

        await Assert.ThrowsAsync<AppException>(() => scenario.BookingService.ProposeRescheduleAsync(
            booking.Id, scenario.ClientId, new ProposeRescheduleDto { NewStartTime = DateTime.UtcNow.AddDays(31) }));
    }

    [Fact(DisplayName = "[UT-BOOK-RSC-07] A proposed time not aligned to a 30-minute slot is rejected")]
    public async Task ProposeReschedule_UnalignedSlot_Throws()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId, start: DateTime.UtcNow.AddHours(10));

        await Assert.ThrowsAsync<AppException>(() => scenario.BookingService.ProposeRescheduleAsync(
            booking.Id, scenario.ClientId, new ProposeRescheduleDto { NewStartTime = ValidNewStart().AddMinutes(10) }));
    }

    [Fact(DisplayName = "[UT-BOOK-RSC-08] Accepting a reschedule updates the booking's scheduled times")]
    public async Task RespondReschedule_Accept_UpdatesScheduledTimes()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId, start: DateTime.UtcNow.AddHours(10));
        var newStart = ValidNewStart();
        var proposed = await scenario.BookingService.ProposeRescheduleAsync(
            booking.Id, scenario.ClientId, new ProposeRescheduleDto { NewStartTime = newStart });
        var requestId = proposed!.PendingReschedule!.Id;

        var responded = await scenario.BookingService.RespondRescheduleAsync(
            booking.Id, requestId, scenario.WorkerId, RescheduleAction.Accept);

        Assert.Equal(nameof(BookingStatus.Accepted), responded!.Status);
        Assert.Equal(newStart, booking.ScheduledStartTime);
        Assert.Null(responded.PendingReschedule);
    }

    [Fact(DisplayName = "[UT-BOOK-RSC-09] Rejecting a reschedule keeps the booking's original scheduled time")]
    public async Task RespondReschedule_Reject_KeepsOldTime()
    {
        var scenario = DispatchScenario.Create();
        var originalStart = DateTime.UtcNow.AddHours(10);
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId, start: originalStart);
        var proposed = await scenario.BookingService.ProposeRescheduleAsync(
            booking.Id, scenario.ClientId, new ProposeRescheduleDto { NewStartTime = ValidNewStart() });
        var requestId = proposed!.PendingReschedule!.Id;

        var responded = await scenario.BookingService.RespondRescheduleAsync(
            booking.Id, requestId, scenario.WorkerId, RescheduleAction.Reject);

        Assert.Equal(nameof(BookingStatus.Accepted), responded!.Status);
        Assert.Equal(booking.ScheduledStartTime, originalStart);
    }

    [Fact(DisplayName = "[UT-BOOK-RSC-10] Withdrawing a reschedule keeps the booking's original scheduled time")]
    public async Task RespondReschedule_Withdraw_KeepsOldTime()
    {
        var scenario = DispatchScenario.Create();
        var originalStart = DateTime.UtcNow.AddHours(10);
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId, start: originalStart);
        var proposed = await scenario.BookingService.ProposeRescheduleAsync(
            booking.Id, scenario.ClientId, new ProposeRescheduleDto { NewStartTime = ValidNewStart() });
        var requestId = proposed!.PendingReschedule!.Id;

        var responded = await scenario.BookingService.RespondRescheduleAsync(
            booking.Id, requestId, scenario.ClientId, RescheduleAction.Withdraw);

        Assert.Equal(booking.ScheduledStartTime, originalStart);
        Assert.Equal(nameof(BookingStatus.Accepted), responded!.Status);
    }

    [Fact(DisplayName = "[UT-BOOK-RSC-11] The requester cannot Accept or Reject their own proposal")]
    public async Task RespondReschedule_RequesterCannotAcceptOwnProposal_Throws()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId, start: DateTime.UtcNow.AddHours(10));
        var proposed = await scenario.BookingService.ProposeRescheduleAsync(
            booking.Id, scenario.ClientId, new ProposeRescheduleDto { NewStartTime = ValidNewStart() });
        var requestId = proposed!.PendingReschedule!.Id;

        await Assert.ThrowsAsync<AppException>(() => scenario.BookingService.RespondRescheduleAsync(
            booking.Id, requestId, scenario.ClientId, RescheduleAction.Accept));
    }

    [Fact(DisplayName = "[UT-BOOK-RSC-12] Only the original requester can Withdraw their proposal")]
    public async Task RespondReschedule_NonRequesterCannotWithdraw_Throws()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId, start: DateTime.UtcNow.AddHours(10));
        var proposed = await scenario.BookingService.ProposeRescheduleAsync(
            booking.Id, scenario.ClientId, new ProposeRescheduleDto { NewStartTime = ValidNewStart() });
        var requestId = proposed!.PendingReschedule!.Id;

        await Assert.ThrowsAsync<AppException>(() => scenario.BookingService.RespondRescheduleAsync(
            booking.Id, requestId, scenario.WorkerId, RescheduleAction.Withdraw));
    }

    [Fact(DisplayName = "[UT-BOOK-RSC-13] Responding to an already-resolved reschedule request is rejected")]
    public async Task RespondReschedule_AlreadyResolved_Throws()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId, start: DateTime.UtcNow.AddHours(10));
        var proposed = await scenario.BookingService.ProposeRescheduleAsync(
            booking.Id, scenario.ClientId, new ProposeRescheduleDto { NewStartTime = ValidNewStart() });
        var requestId = proposed!.PendingReschedule!.Id;
        await scenario.BookingService.RespondRescheduleAsync(booking.Id, requestId, scenario.WorkerId, RescheduleAction.Reject);

        await Assert.ThrowsAsync<AppException>(() => scenario.BookingService.RespondRescheduleAsync(
            booking.Id, requestId, scenario.WorkerId, RescheduleAction.Accept));
    }

    [Fact(DisplayName = "[UT-BOOK-RSC-14] A system actor can auto-reject an expired reschedule request without a role check")]
    public async Task RespondReschedule_SystemActor_BypassesRoleCheck()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId, start: DateTime.UtcNow.AddHours(10));
        var proposed = await scenario.BookingService.ProposeRescheduleAsync(
            booking.Id, scenario.ClientId, new ProposeRescheduleDto { NewStartTime = ValidNewStart() });
        var requestId = proposed!.PendingReschedule!.Id;

        var result = await scenario.BookingService.RespondRescheduleAsync(
            booking.Id, requestId, Guid.NewGuid(), RescheduleAction.Reject, isSystemActor: true);

        Assert.Null(result);
        Assert.Equal(BookingStatus.Accepted, booking.Status);
    }
}

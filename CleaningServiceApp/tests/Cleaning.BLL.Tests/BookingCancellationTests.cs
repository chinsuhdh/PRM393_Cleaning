using Cleaning.BLL.Common;
using Cleaning.BLL.Constants;
using Cleaning.BLL.DTOs;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;

namespace Cleaning.BLL.Tests;

public sealed partial class BookingDispatchTests
{
    [Fact(DisplayName = "[UT-BOOK-CXL-01] Client cancel succeeds pre-accept (AwaitingWorker)")]
    public async Task CancelByClient_AwaitingWorker_Succeeds()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.AwaitingWorker);

        var cancelled = await scenario.BookingService.CancelByClientAsync(booking.Id, scenario.ClientId);

        Assert.True(cancelled);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }

    [Fact(DisplayName = "[UT-BOOK-CXL-02] Client cancel loses the race once a worker has already accepted")]
    public async Task CancelByClient_AlreadyAccepted_ReturnsFalse()
    {
        var scenario = DispatchScenario.Create();
        // Simulates the losing side of an accept-vs-cancel race: by the time this runs, the
        // booking has already moved to Accepted in another transaction.
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);

        var cancelled = await scenario.BookingService.CancelByClientAsync(booking.Id, scenario.ClientId);

        Assert.False(cancelled);
        Assert.Equal(BookingStatus.Accepted, booking.Status);
    }

    [Theory(DisplayName = "[UT-BOOK-CXL-03] Worker plain-cancel succeeds for every reason code and releases the job")]
    [InlineData("worker_cancel.schedule_conflict")]
    [InlineData("worker_cancel.too_far")]
    public async Task WorkerCancel_EachReasonCode_ReleasesJob(string reasonCode)
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);

        await scenario.BookingService.WorkerCancelAsync(
            booking.Id, scenario.WorkerId, new WorkerCancelBookingDto { ReasonCode = reasonCode });

        Assert.Equal(BookingStatus.AwaitingWorker, booking.Status);
        Assert.Null(booking.WorkerId);
        var record = Assert.Single(scenario.Cancellations);
        Assert.Equal(reasonCode, record.ReasonCode);
        Assert.Equal(UserRole.Worker, record.ActorRole);
    }

    [Fact(DisplayName = "[UT-BOOK-CXL-04] Worker plain-cancel with \"other\" requires free text and records it")]
    public async Task WorkerCancel_OtherWithText_RecordsFreeText()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);

        await scenario.BookingService.WorkerCancelAsync(
            booking.Id, scenario.WorkerId,
            new WorkerCancelBookingDto { ReasonCode = WorkerCancelReasonCodes.Other, FreeText = "Xe hỏng dọc đường" });

        var record = Assert.Single(scenario.Cancellations);
        Assert.Contains("Xe hỏng dọc đường", record.Reason);
    }

    [Fact(DisplayName = "[UT-BOOK-CXL-05] Worker plain-cancel with \"other\" and no free text is rejected")]
    public async Task WorkerCancel_OtherWithoutText_Throws()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);

        await Assert.ThrowsAsync<AppException>(() => scenario.BookingService.WorkerCancelAsync(
            booking.Id, scenario.WorkerId, new WorkerCancelBookingDto { ReasonCode = WorkerCancelReasonCodes.Other }));
        Assert.Equal(BookingStatus.Accepted, booking.Status);
    }

    [Fact(DisplayName = "[UT-BOOK-CXL-06] An unrecognized reason code is rejected")]
    public async Task WorkerCancel_UnknownReasonCode_Throws()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);

        await Assert.ThrowsAsync<AppException>(() => scenario.BookingService.WorkerCancelAsync(
            booking.Id, scenario.WorkerId, new WorkerCancelBookingDto { ReasonCode = "not_a_real_code" }));
    }

    [Fact(DisplayName = "[UT-BOOK-CXL-07] Worker plain-cancel re-broadcasts the released job to eligible workers")]
    public async Task WorkerCancel_ReleasesJob_TriggersRebroadcast()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);

        await scenario.BookingService.WorkerCancelAsync(
            booking.Id, scenario.WorkerId, new WorkerCancelBookingDto { ReasonCode = WorkerCancelReasonCodes.TooFar });

        Assert.Contains(scenario.DispatchPublisher.PostedJobs, dto => dto.Id == booking.Id);
    }

    [Fact(DisplayName = "[UT-WRK-SUS-01] A 3rd plain-cancel within 30 days suspends the worker")]
    public async Task WorkerCancel_ThirdStrikeWithinWindow_Suspends()
    {
        var scenario = DispatchScenario.Create();

        for (var i = 0; i < 3; i++)
        {
            var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);
            await scenario.BookingService.WorkerCancelAsync(
                booking.Id, scenario.WorkerId, new WorkerCancelBookingDto { ReasonCode = WorkerCancelReasonCodes.TooFar });
        }

        Assert.NotNull(scenario.Worker.SuspendedAt);
        Assert.Equal(WorkerOnlineStatus.Offline, scenario.Worker.OnlineStatus);
        Assert.Contains(scenario.WorkerId, scenario.DispatchPublisher.SuspendedWorkerIds);
    }

    [Fact(DisplayName = "[UT-WRK-SUS-02] A 2nd plain-cancel within 30 days does not suspend the worker")]
    public async Task WorkerCancel_SecondStrike_DoesNotSuspend()
    {
        var scenario = DispatchScenario.Create();

        for (var i = 0; i < 2; i++)
        {
            var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);
            await scenario.BookingService.WorkerCancelAsync(
                booking.Id, scenario.WorkerId, new WorkerCancelBookingDto { ReasonCode = WorkerCancelReasonCodes.TooFar });
        }

        Assert.Null(scenario.Worker.SuspendedAt);
        Assert.Empty(scenario.DispatchPublisher.SuspendedWorkerIds);
    }

    [Fact(DisplayName = "[UT-WRK-SUS-03] Plain-cancels older than the 30-day window do not count toward the strike total")]
    public async Task WorkerCancel_StaleCancelsOutsideWindow_DoNotCount()
    {
        var scenario = DispatchScenario.Create();
        for (var i = 0; i < 2; i++)
        {
            scenario.Cancellations.Add(new BookingCancellation
            {
                Id = Guid.NewGuid(),
                BookingId = Guid.NewGuid(),
                CancelledBy = scenario.WorkerId,
                ActorRole = UserRole.Worker,
                ReasonCode = WorkerCancelReasonCodes.TooFar,
                Reason = WorkerCancelReasonCodes.Labels[WorkerCancelReasonCodes.TooFar],
                CreatedAt = DateTime.UtcNow.AddDays(-(CancellationConstants.SuspensionWindowDays + 1))
            });
        }
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);

        await scenario.BookingService.WorkerCancelAsync(
            booking.Id, scenario.WorkerId, new WorkerCancelBookingDto { ReasonCode = WorkerCancelReasonCodes.ScheduleConflict });

        Assert.Null(scenario.Worker.SuspendedAt);
    }

    [Fact(DisplayName = "[UT-WRK-SUS-04] Reports (report.* reason codes) do not count toward the worker-cancel strike total")]
    public async Task WorkerCancel_ExistingReports_DoNotCount()
    {
        var scenario = DispatchScenario.Create();
        for (var i = 0; i < 2; i++)
        {
            scenario.Cancellations.Add(new BookingCancellation
            {
                Id = Guid.NewGuid(),
                BookingId = Guid.NewGuid(),
                CancelledBy = scenario.WorkerId,
                ActorRole = UserRole.Worker,
                ReasonCode = "report.worker.client_absent",
                Reason = "Khách vắng mặt",
                CreatedAt = DateTime.UtcNow
            });
        }
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);

        await scenario.BookingService.WorkerCancelAsync(
            booking.Id, scenario.WorkerId, new WorkerCancelBookingDto { ReasonCode = WorkerCancelReasonCodes.ScheduleConflict });

        Assert.Null(scenario.Worker.SuspendedAt);
    }

    [Theory(DisplayName = "[UT-BOOK-CXL-08] Client plain-cancel succeeds for every reason code and releases the job")]
    [InlineData("client_cancel.no_longer_needed")]
    [InlineData("client_cancel.found_another_provider")]
    public async Task ClientCancel_EachReasonCode_ReleasesJob(string reasonCode)
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);

        await scenario.BookingService.ClientCancelAsync(
            booking.Id, scenario.ClientId, new ClientCancelBookingDto { ReasonCode = reasonCode });

        Assert.Equal(BookingStatus.AwaitingWorker, booking.Status);
        Assert.Null(booking.WorkerId);
        var record = Assert.Single(scenario.Cancellations);
        Assert.Equal(reasonCode, record.ReasonCode);
        Assert.Equal(UserRole.Client, record.ActorRole);
    }

    [Fact(DisplayName = "[UT-BOOK-CXL-09] Client plain-cancel with \"other\" requires free text and records it")]
    public async Task ClientCancel_OtherWithText_RecordsFreeText()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);

        await scenario.BookingService.ClientCancelAsync(
            booking.Id, scenario.ClientId,
            new ClientCancelBookingDto { ReasonCode = ClientCancelReasonCodes.Other, FreeText = "Đổi ý" });

        var record = Assert.Single(scenario.Cancellations);
        Assert.Contains("Đổi ý", record.Reason);
    }

    [Fact(DisplayName = "[UT-BOOK-CXL-10] Client plain-cancel with \"other\" and no free text is rejected")]
    public async Task ClientCancel_OtherWithoutText_Throws()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);

        await Assert.ThrowsAsync<AppException>(() => scenario.BookingService.ClientCancelAsync(
            booking.Id, scenario.ClientId, new ClientCancelBookingDto { ReasonCode = ClientCancelReasonCodes.Other }));
        Assert.Equal(BookingStatus.Accepted, booking.Status);
    }

    [Fact(DisplayName = "[UT-BOOK-CXL-11] An unrecognized client-cancel reason code is rejected")]
    public async Task ClientCancel_UnknownReasonCode_Throws()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);

        await Assert.ThrowsAsync<AppException>(() => scenario.BookingService.ClientCancelAsync(
            booking.Id, scenario.ClientId, new ClientCancelBookingDto { ReasonCode = "not_a_real_code" }));
    }

    [Fact(DisplayName = "[UT-BOOK-CXL-12] Client plain-cancel is rejected once the job has moved past Accepted")]
    public async Task ClientCancel_PastAccepted_Throws()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.OnTheWay, workerId: scenario.WorkerId);

        await Assert.ThrowsAsync<AppException>(() => scenario.BookingService.ClientCancelAsync(
            booking.Id, scenario.ClientId,
            new ClientCancelBookingDto { ReasonCode = ClientCancelReasonCodes.NoLongerNeeded }));
        Assert.Equal(BookingStatus.OnTheWay, booking.Status);
    }

    [Fact(DisplayName = "[UT-BOOK-CXL-13] Client plain-cancel re-broadcasts the released job to eligible workers")]
    public async Task ClientCancel_ReleasesJob_TriggersRebroadcast()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);

        await scenario.BookingService.ClientCancelAsync(
            booking.Id, scenario.ClientId,
            new ClientCancelBookingDto { ReasonCode = ClientCancelReasonCodes.FoundAnotherProvider });

        Assert.Contains(scenario.DispatchPublisher.PostedJobs, dto => dto.Id == booking.Id);
    }

    [Fact(DisplayName = "[UT-BOOK-CXL-14] Client plain-cancel does not apply the worker-suspension penalty")]
    public async Task ClientCancel_DoesNotAffectWorkerSuspension()
    {
        var scenario = DispatchScenario.Create();

        for (var i = 0; i < 3; i++)
        {
            var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);
            await scenario.BookingService.ClientCancelAsync(
                booking.Id, scenario.ClientId,
                new ClientCancelBookingDto { ReasonCode = ClientCancelReasonCodes.NoLongerNeeded });
        }

        Assert.Null(scenario.Worker.SuspendedAt);
        Assert.Empty(scenario.DispatchPublisher.SuspendedWorkerIds);
    }

    [Fact(DisplayName = "[UT-WRK-SUS-05] A suspended worker is still excluded from eligibility (regression)")]
    public async Task GetAvailableBookings_SuspendedWorker_ExcludesJobs()
    {
        var scenario = DispatchScenario.Create();
        scenario.Worker.SuspendedAt = DateTime.UtcNow;
        scenario.AddBooking(BookingStatus.AwaitingWorker);

        var available = await scenario.BookingService.GetAvailableBookingsAsync(scenario.WorkerId);

        Assert.Empty(available);
    }
}

using Cleaning.BLL.Common;
using Cleaning.BLL.DTOs;
using Cleaning.DAL.Enums;

namespace Cleaning.BLL.Tests;

public sealed partial class BookingDispatchTests
{
    private static ReportBookingDto Report(string reasonCode, string freeText = "Mo ta chi tiet van de gap phai") =>
        new() { ReasonCode = reasonCode, FreeText = freeText };

    [Fact(DisplayName = "[UT-BOOK-RPT-01] A valid client report cancels the booking and tags it with the client reason code")]
    public async Task Report_ValidClientReport_CancelsAndTagsReason()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.InProgress, workerId: scenario.WorkerId);

        await scenario.BookingService.ReportBookingAsync(
            booking.Id, scenario.ClientId, Report("report.client.worker_no_show"));

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        var record = Assert.Single(scenario.Cancellations);
        Assert.Equal("report.client.worker_no_show", record.ReasonCode);
        Assert.Equal(UserRole.Client, record.ActorRole);
    }

    [Fact(DisplayName = "[UT-BOOK-RPT-02] A valid worker report cancels the booking and tags it with the worker reason code")]
    public async Task Report_ValidWorkerReport_CancelsAndTagsReason()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);

        await scenario.BookingService.ReportBookingAsync(
            booking.Id, scenario.WorkerId, Report("report.worker.client_absent"));

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        var record = Assert.Single(scenario.Cancellations);
        Assert.Equal(UserRole.Worker, record.ActorRole);
    }

    [Fact(DisplayName = "[UT-BOOK-RPT-03] A client using a worker-only reason code is rejected")]
    public async Task Report_ClientUsingWorkerReasonCode_Throws()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);

        await Assert.ThrowsAsync<AppException>(() => scenario.BookingService.ReportBookingAsync(
            booking.Id, scenario.ClientId, Report("report.worker.client_absent")));
        Assert.Equal(BookingStatus.Accepted, booking.Status);
    }

    [Theory(DisplayName = "[UT-BOOK-RPT-04] Free text right at the 20-character boundary is accepted; 19 is rejected")]
    [InlineData(19, false)]
    [InlineData(20, true)]
    public async Task Report_FreeTextBoundary_EnforcesMinimumLength(int length, bool shouldSucceed)
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);
        var freeText = new string('a', length);

        if (shouldSucceed)
        {
            await scenario.BookingService.ReportBookingAsync(
                booking.Id, scenario.ClientId, Report("report.client.other", freeText));
            Assert.Equal(BookingStatus.Cancelled, booking.Status);
        }
        else
        {
            await Assert.ThrowsAsync<AppException>(() => scenario.BookingService.ReportBookingAsync(
                booking.Id, scenario.ClientId, Report("report.client.other", freeText)));
            Assert.Equal(BookingStatus.Accepted, booking.Status);
        }
    }

    [Fact(DisplayName = "[UT-BOOK-RPT-05] A non-participant cannot report a booking")]
    public async Task Report_NonParticipant_Throws()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);

        await Assert.ThrowsAsync<AppException>(() => scenario.BookingService.ReportBookingAsync(
            booking.Id, Guid.NewGuid(), Report("report.client.other")));
    }

    [Fact(DisplayName = "[UT-BOOK-RPT-06] A report on a pre-accept AwaitingWorker booking is rejected")]
    public async Task Report_AwaitingWorkerBooking_Throws()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(BookingStatus.AwaitingWorker);

        await Assert.ThrowsAsync<AppException>(() => scenario.BookingService.ReportBookingAsync(
            booking.Id, scenario.ClientId, Report("report.client.other")));
    }

    [Fact(DisplayName = "[UT-BOOK-RPT-07] Report cancellations are excluded from the worker-cancel suspension count")]
    public async Task Report_DoesNotCountTowardWorkerSuspension()
    {
        var scenario = DispatchScenario.Create();
        var first = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);
        await scenario.BookingService.ReportBookingAsync(first.Id, scenario.WorkerId, Report("report.worker.client_absent"));
        var second = scenario.AddBooking(BookingStatus.Accepted, workerId: scenario.WorkerId);
        await scenario.BookingService.ReportBookingAsync(second.Id, scenario.WorkerId, Report("report.worker.client_absent"));

        var count = await scenario.BookingService.CountRecentPlainCancelsAsync(scenario.WorkerId);

        Assert.Equal(0, count);
        Assert.Null(scenario.Worker.SuspendedAt);
    }
}

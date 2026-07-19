using Cleaning.BLL.Common;
using Cleaning.BLL.DTOs;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;

namespace Cleaning.BLL.Tests;

public sealed partial class BookingDispatchTests
{
    [Fact(DisplayName = "[UT-BOOK-PAY-01] VNPay: worker Finish parks the booking at PendingPayment with no " +
        "payment row and no auto-charge — the client must pay through the real VNPay flow")]
    public async Task UpdateStatus_VnpayFinish_ParksAtPendingPaymentWithoutCharging()
    {
        var scenario = DispatchScenario.Create(workerOnlineStatus: WorkerOnlineStatus.Busy);
        var booking = scenario.AddBooking(
            BookingStatus.InProgress, workerId: scenario.WorkerId, paymentMethod: PaymentMethod.Vnpay);
        booking.TotalPrice = 250_000;

        var updated = await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, scenario.WorkerId, new UpdateBookingStatusDto { NewStatus = BookingStatus.PendingPayment });

        Assert.True(updated);
        Assert.Equal(BookingStatus.PendingPayment, booking.Status);
        Assert.Empty(scenario.Payments);
        Assert.Empty(scenario.WorkerEarnings);
        Assert.Equal(WorkerOnlineStatus.Busy, scenario.Worker.OnlineStatus);

        Assert.DoesNotContain(scenario.DispatchPublisher.StatusChanges, change =>
            change.BookingId == booking.Id && change.NewStatus == nameof(BookingStatus.Completed));
    }

    [Fact(DisplayName = "[UT-BOOK-PAY-01b] VNPay: a worker cannot bypass real payment by completing a " +
        "PendingPayment booking directly through the generic status endpoint")]
    public async Task UpdateStatus_VnpayDirectComplete_Rejected()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(
            BookingStatus.PendingPayment, workerId: scenario.WorkerId, paymentMethod: PaymentMethod.Vnpay);

        var updated = await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, scenario.WorkerId, new UpdateBookingStatusDto { NewStatus = BookingStatus.Completed });

        Assert.False(updated);
        Assert.Equal(BookingStatus.PendingPayment, booking.Status);
        Assert.Empty(scenario.Payments);
    }

    [Fact(DisplayName = "[UT-BOOK-PAY-02] Cash: Finish parks the booking at PendingPayment with no payment " +
        "row; the worker's cash confirm then completes it, records a Cash/Success payment and a settled " +
        "worker earning")]
    public async Task UpdateStatus_CashFinishThenConfirm_WritesCashPaymentAndSettledEarning()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(
            BookingStatus.InProgress, workerId: scenario.WorkerId, paymentMethod: PaymentMethod.Cash);
        booking.TotalPrice = 180_000;

        await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, scenario.WorkerId, new UpdateBookingStatusDto { NewStatus = BookingStatus.PendingPayment });
        Assert.Equal(BookingStatus.PendingPayment, booking.Status);
        Assert.Empty(scenario.Payments);

        var confirmed = await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, scenario.WorkerId, new UpdateBookingStatusDto { NewStatus = BookingStatus.Completed });

        Assert.True(confirmed);
        Assert.Equal(BookingStatus.Completed, booking.Status);
        var payment = Assert.Single(scenario.Payments);
        Assert.Equal(PaymentMethod.Cash, payment.Method);
        Assert.Equal(PaymentStatus.Success, payment.Status);
        Assert.Equal(180_000, payment.Amount);
        Assert.NotNull(payment.PaidAt);
        Assert.Null(payment.TransactionId);

        var earning = Assert.Single(scenario.WorkerEarnings);
        Assert.Equal(booking.Id, earning.BookingId);
        Assert.Equal(scenario.WorkerId, earning.WorkerId);
        Assert.Equal(180_000, earning.Amount);
        Assert.Equal("settled", earning.Status);
    }

    [Fact(DisplayName = "[UT-BOOK-PAY-02b] Cash confirm is idempotent: writing the earning twice never " +
        "produces more than one row")]
    public async Task UpdateStatus_CashConfirmTwice_WritesEarningOnce()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(
            BookingStatus.PendingPayment, workerId: scenario.WorkerId, paymentMethod: PaymentMethod.Cash);
        booking.TotalPrice = 180_000;
        scenario.WorkerEarnings.Add(new WorkerEarning
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            WorkerId = scenario.WorkerId,
            Amount = 180_000,
            Status = "settled",
            EarnedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, scenario.WorkerId, new UpdateBookingStatusDto { NewStatus = BookingStatus.Completed });

        Assert.Single(scenario.WorkerEarnings);
    }

    [Fact(DisplayName = "[UT-BOOK-PAY-04] Creating a VNPay booking succeeds (no account-linking gate — VNPay " +
        "checkout needs no pre-linking) and the booking carries PaymentMethod=Vnpay end to end")]
    public async Task Create_Vnpay_Succeeds()
    {
        var scenario = DispatchScenario.Create();
        scenario.AddClientAccount();
        var request = scenario.CreateRequest();
        request.PaymentMethod = PaymentMethod.Vnpay;

        var dto = await scenario.BookingService.CreateBookingAsync(scenario.ClientId, "vnpay-booking", request);

        Assert.Equal(nameof(PaymentMethod.Vnpay), dto.PaymentMethod);
        var booking = Assert.Single(scenario.Bookings);
        Assert.Equal(PaymentMethod.Vnpay, booking.PaymentMethod);
    }
}

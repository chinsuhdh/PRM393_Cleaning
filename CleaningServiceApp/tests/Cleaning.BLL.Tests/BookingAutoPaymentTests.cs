using Cleaning.BLL.Common;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Services;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cleaning.BLL.Tests;

public sealed partial class BookingDispatchTests
{
    [Fact(DisplayName = "[UT-BOOK-PAY-01] VNPay: worker Finish auto-charges (simulated) and closes the " +
        "booking as Completed in one step, publishing Completed — the client never acts")]
    public async Task UpdateStatus_VnpayFinish_AutoChargesAndCompletes()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(
            BookingStatus.InProgress, workerId: scenario.WorkerId, paymentMethod: PaymentMethod.Vnpay);
        booking.TotalPrice = 250_000;

        var updated = await scenario.BookingService.UpdateBookingStatusAsync(
            booking.Id, scenario.WorkerId, new UpdateBookingStatusDto { NewStatus = BookingStatus.PendingPayment });

        Assert.True(updated);
        Assert.Equal(BookingStatus.Completed, booking.Status);

        var payment = Assert.Single(scenario.Payments);
        Assert.Equal(booking.Id, payment.BookingId);
        Assert.Equal(PaymentMethod.Vnpay, payment.Method);
        Assert.Equal(PaymentStatus.Success, payment.Status);
        Assert.Equal(250_000, payment.Amount);
        Assert.NotNull(payment.PaidAt);
        Assert.StartsWith("SIM-VNPAY-", payment.TransactionId);

        // Both hops are recorded: the worker's Finish, then the system's auto-payment close.
        Assert.Single(scenario.StatusLogs, log =>
            log.OldStatus == BookingStatus.InProgress && log.NewStatus == BookingStatus.PendingPayment &&
            log.ChangedBy == scenario.WorkerId);
        Assert.Single(scenario.StatusLogs, log =>
            log.OldStatus == BookingStatus.PendingPayment && log.NewStatus == BookingStatus.Completed &&
            log.ChangedBy == null);

        // Screens must land on the final state, not the transient PendingPayment.
        Assert.Contains(scenario.DispatchPublisher.StatusChanges, change =>
            change.BookingId == booking.Id && change.NewStatus == nameof(BookingStatus.Completed));
        Assert.DoesNotContain(scenario.DispatchPublisher.StatusChanges, change =>
            change.BookingId == booking.Id && change.NewStatus == nameof(BookingStatus.PendingPayment));
    }

    [Fact(DisplayName = "[UT-BOOK-PAY-02] Cash: Finish parks the booking at PendingPayment with no payment " +
        "row; the worker's cash confirm then completes it and records a Cash/Success payment")]
    public async Task UpdateStatus_CashFinishThenConfirm_WritesCashPayment()
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
    }

    [Fact(DisplayName = "[UT-BOOK-PAY-03] Creating a VNPay booking without a linked VNPay account is " +
        "rejected with VNPAY_NOT_LINKED")]
    public async Task Create_VnpayWithoutLinkedAccount_Throws()
    {
        var scenario = DispatchScenario.Create();
        scenario.AddClientAccount(vnpayAccount: null);
        var request = scenario.CreateRequest();
        request.PaymentMethod = PaymentMethod.Vnpay;

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            scenario.BookingService.CreateBookingAsync(scenario.ClientId, "vnpay-unlinked", request));

        Assert.Equal(AppErrors.VnpayNotLinked.Code, exception.Code);
        Assert.Empty(scenario.Bookings);
    }

    [Fact(DisplayName = "[UT-BOOK-PAY-04] Creating a VNPay booking with a linked account succeeds and the " +
        "booking carries PaymentMethod=Vnpay end to end")]
    public async Task Create_VnpayWithLinkedAccount_Succeeds()
    {
        var scenario = DispatchScenario.Create();
        scenario.AddClientAccount(vnpayAccount: "0901234567");
        var request = scenario.CreateRequest();
        request.PaymentMethod = PaymentMethod.Vnpay;

        var dto = await scenario.BookingService.CreateBookingAsync(scenario.ClientId, "vnpay-linked", request);

        Assert.Equal(nameof(PaymentMethod.Vnpay), dto.PaymentMethod);
        var booking = Assert.Single(scenario.Bookings);
        Assert.Equal(PaymentMethod.Vnpay, booking.PaymentMethod);
    }

    [Fact(DisplayName = "[UT-BOOK-PAY-05] Linking a VNPay account (simulated gateway) stores the trimmed " +
        "value; blank input is rejected with VNPAY_ACCOUNT_INVALID")]
    public async Task LinkVnpayAccount_SimulatedGateway_StoresValueAndRejectsBlank()
    {
        var accountId = Guid.NewGuid();
        var unitOfWork = new InMemoryUnitOfWork().With<Account>([new Account
        {
            Id = accountId,
            Email = "client@test.local",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }]);
        var paymentService = new PaymentService(unitOfWork, NullLogger<PaymentService>.Instance);

        var blank = await Assert.ThrowsAsync<AppException>(() =>
            paymentService.LinkVnpayAccountAsync(accountId, new VnpayAccountDto { VnpayAccount = "   " }));
        Assert.Equal(AppErrors.VnpayAccountInvalid.Code, blank.Code);

        var linked = await paymentService.LinkVnpayAccountAsync(
            accountId, new VnpayAccountDto { VnpayAccount = "  0901234567  " });
        Assert.Equal("0901234567", linked.VnpayAccount);

        var fetched = await paymentService.GetVnpayAccountAsync(accountId);
        Assert.Equal("0901234567", fetched.VnpayAccount);
    }
}

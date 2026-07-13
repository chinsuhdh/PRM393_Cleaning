using Cleaning.BLL.Common;
using Cleaning.DAL.Enums;

namespace Cleaning.BLL.Tests;

public sealed partial class BookingDispatchTests
{
    [Fact(DisplayName = "[UT-BOOK-SWC-01] Khách chuyển đơn PendingPayment/payOS sang tiền mặt: đổi phương " +
        "thức và ghi lại lịch sử")]
    public async Task SwitchToCashAsync_HappyPath_FlipsMethodAndLogsHistory()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(
            BookingStatus.PendingPayment, workerId: scenario.WorkerId, paymentMethod: PaymentMethod.Payos);

        await scenario.BookingService.SwitchToCashAsync(booking.Id, scenario.ClientId);

        Assert.Equal(PaymentMethod.Cash, booking.PaymentMethod);
        Assert.Single(scenario.StatusLogs, log =>
            log.BookingId == booking.Id &&
            log.OldStatus == BookingStatus.PendingPayment &&
            log.NewStatus == BookingStatus.PendingPayment &&
            log.ChangedBy == scenario.ClientId);
        Assert.Contains(scenario.DispatchPublisher.StatusChanges, change =>
            change.BookingId == booking.Id && change.NewStatus == nameof(BookingStatus.PendingPayment));
    }

    [Fact(DisplayName = "[UT-BOOK-SWC-02] Người không phải chủ đơn không thể chuyển sang tiền mặt")]
    public async Task SwitchToCashAsync_NonOwningClient_Rejected()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(
            BookingStatus.PendingPayment, workerId: scenario.WorkerId, paymentMethod: PaymentMethod.Payos);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            scenario.BookingService.SwitchToCashAsync(booking.Id, Guid.NewGuid()));

        Assert.Equal(AppErrors.Forbidden.Code, exception.Code);
        Assert.Equal(PaymentMethod.Payos, booking.PaymentMethod);
    }

    [Fact(DisplayName = "[UT-BOOK-SWC-03] Không thể chuyển sang tiền mặt khi đơn không ở trạng thái chờ " +
        "thanh toán")]
    public async Task SwitchToCashAsync_WrongStatus_Rejected()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(
            BookingStatus.InProgress, workerId: scenario.WorkerId, paymentMethod: PaymentMethod.Payos);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            scenario.BookingService.SwitchToCashAsync(booking.Id, scenario.ClientId));

        Assert.Equal(AppErrors.BookingNotPendingPayment.Code, exception.Code);
    }

    [Fact(DisplayName = "[UT-BOOK-SWC-04] Đơn đã dùng tiền mặt thì không thể chuyển lại")]
    public async Task SwitchToCashAsync_AlreadyCash_Rejected()
    {
        var scenario = DispatchScenario.Create();
        var booking = scenario.AddBooking(
            BookingStatus.PendingPayment, workerId: scenario.WorkerId, paymentMethod: PaymentMethod.Cash);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            scenario.BookingService.SwitchToCashAsync(booking.Id, scenario.ClientId));

        Assert.Equal(AppErrors.PaymentMethodAlreadyCash.Code, exception.Code);
    }
}

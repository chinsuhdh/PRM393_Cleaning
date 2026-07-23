using Cleaning.BLL.Features.Payments;
using Cleaning.BLL.Common;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Cleaning.BLL.Tests;

public class PaymentServiceTests
{
    private static PaymentService CreateService(
        InMemoryUnitOfWork unitOfWork,
        IVnpayCheckoutService? checkoutService = null) =>
        new(unitOfWork, NullLogger<PaymentService>.Instance, TestMapperFactory.Create(),
            checkoutService ?? Mock.Of<IVnpayCheckoutService>());

    private static Booking CreatePendingVnpayBooking(Guid clientId, Guid workerId, decimal totalPrice) => new()
    {
        Id = Guid.NewGuid(),
        ClientId = clientId,
        WorkerId = workerId,
        Status = BookingStatus.PendingPayment,
        PaymentMethod = PaymentMethod.Vnpay,
        TotalPrice = totalPrice,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Fact(DisplayName = "[UT-BE-PAY-001-01] Pay-now dùng giá booking từ máy chủ, bỏ qua mọi giá trị client gửi lên")]
    public async Task PayNowAsync_UsesServerBookingPrice()
    {
        var clientId = Guid.NewGuid();
        var workerId = Guid.NewGuid();
        var booking = CreatePendingVnpayBooking(clientId, workerId, 250_000m);
        var unitOfWork = new InMemoryUnitOfWork().With([booking]).With(new List<Payment>());

        var checkoutService = new Mock<IVnpayCheckoutService>();
        checkoutService
            .Setup(s => s.CreatePaymentUrl(250_000m, It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new VnpayCheckoutLink("txn-123456", "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?vnp_TxnRef=txn-123456"));
        var service = CreateService(unitOfWork, checkoutService.Object);

        var result = await service.PayNowAsync(clientId, new PayNowRequestDto { BookingId = booking.Id }, "127.0.0.1");

        Assert.Equal("https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?vnp_TxnRef=txn-123456", result.PaymentUrl);
        var payment = Assert.Single(unitOfWork.Repository<Payment>().GetQueryable());
        Assert.Equal(250_000m, payment.Amount);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal("txn-123456", payment.VnpTxnRef);
    }

    [Fact(DisplayName = "[UT-BE-PAY-002] Pay-now bị từ chối nếu người gọi không phải chủ đơn")]
    public async Task PayNowAsync_NonOwningClient_Throws()
    {
        var clientId = Guid.NewGuid();
        var booking = CreatePendingVnpayBooking(clientId, Guid.NewGuid(), 100_000m);
        var unitOfWork = new InMemoryUnitOfWork().With([booking]);
        var service = CreateService(unitOfWork);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.PayNowAsync(Guid.NewGuid(), new PayNowRequestDto { BookingId = booking.Id }, "127.0.0.1"));

        Assert.Equal(AppErrors.Forbidden.Code, exception.Code);
    }

    [Fact(DisplayName = "[UT-BE-PAY-003] Pay-now bị từ chối khi đơn không ở trạng thái chờ thanh toán")]
    public async Task PayNowAsync_WrongStatus_Throws()
    {
        var clientId = Guid.NewGuid();
        var booking = CreatePendingVnpayBooking(clientId, Guid.NewGuid(), 100_000m);
        booking.Status = BookingStatus.InProgress;
        var unitOfWork = new InMemoryUnitOfWork().With([booking]);
        var service = CreateService(unitOfWork);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.PayNowAsync(clientId, new PayNowRequestDto { BookingId = booking.Id }, "127.0.0.1"));

        Assert.Equal(AppErrors.BookingNotPendingPayment.Code, exception.Code);
    }

    [Fact(DisplayName = "[UT-BE-PAY-004] Pay-now bị từ chối nếu đơn dùng thanh toán tiền mặt")]
    public async Task PayNowAsync_CashBooking_Throws()
    {
        var clientId = Guid.NewGuid();
        var booking = CreatePendingVnpayBooking(clientId, Guid.NewGuid(), 100_000m);
        booking.PaymentMethod = PaymentMethod.Cash;
        var unitOfWork = new InMemoryUnitOfWork().With([booking]);
        var service = CreateService(unitOfWork);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.PayNowAsync(clientId, new PayNowRequestDto { BookingId = booking.Id }, "127.0.0.1"));

        Assert.Equal(AppErrors.PaymentMethodNotVnpay.Code, exception.Code);
    }

    [Fact(DisplayName = "[UT-BE-PAY-005] Pay-now bị từ chối nếu đơn đã thanh toán thành công")]
    public async Task PayNowAsync_AlreadyCompleted_Throws()
    {
        var clientId = Guid.NewGuid();
        var booking = CreatePendingVnpayBooking(clientId, Guid.NewGuid(), 100_000m);
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            Amount = 100_000m,
            Method = PaymentMethod.Vnpay,
            Status = PaymentStatus.Success,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var unitOfWork = new InMemoryUnitOfWork().With([booking]).With([payment]);
        var service = CreateService(unitOfWork);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.PayNowAsync(clientId, new PayNowRequestDto { BookingId = booking.Id }, "127.0.0.1"));

        Assert.Equal(AppErrors.PaymentAlreadyCompleted.Code, exception.Code);
    }

    private static Dictionary<string, string> SuccessfulConfirmParams(string txnRef = "txn-123456") => new()
    {
        ["vnp_TxnRef"] = txnRef,
        ["vnp_ResponseCode"] = "00",
        ["vnp_TransactionStatus"] = "00"
    };

    [Fact(DisplayName = "[UT-BE-CONFIRM-001] Xác nhận VNPay không xác thực được chữ ký bị từ chối và không thay đổi dữ liệu")]
    public async Task ConfirmVnpayPaymentAsync_InvalidSignature_Rejected()
    {
        var unitOfWork = new InMemoryUnitOfWork().With(new List<Payment>());
        var checkoutService = new Mock<IVnpayCheckoutService>();
        checkoutService.Setup(s => s.VerifyCallback(It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Returns(new VnpayCallbackResult(false, false, "", 0, null, "97"));
        var service = CreateService(unitOfWork, checkoutService.Object);

        var outcome = await service.ConfirmVnpayPaymentAsync(new Dictionary<string, string>());

        Assert.Equal(VnpayConfirmOutcome.InvalidSignature, outcome);
    }

    [Fact(DisplayName = "[UT-BE-CONFIRM-002] Xác nhận với TxnRef không tồn tại bị từ chối")]
    public async Task ConfirmVnpayPaymentAsync_UnknownTxnRef_Rejected()
    {
        var unitOfWork = new InMemoryUnitOfWork().With(new List<Payment>());
        var checkoutService = new Mock<IVnpayCheckoutService>();
        checkoutService.Setup(s => s.VerifyCallback(It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Returns(new VnpayCallbackResult(true, true, "unknown-txn", 100_000m, "ref-1", "00"));
        var service = CreateService(unitOfWork, checkoutService.Object);

        var outcome = await service.ConfirmVnpayPaymentAsync(SuccessfulConfirmParams("unknown-txn"));

        Assert.Equal(VnpayConfirmOutcome.OrderNotFound, outcome);
    }

    [Fact(DisplayName = "[UT-BE-CONFIRM-003] Xác nhận với số tiền không khớp bị từ chối")]
    public async Task ConfirmVnpayPaymentAsync_AmountMismatch_Rejected()
    {
        var clientId = Guid.NewGuid();
        var workerId = Guid.NewGuid();
        var booking = CreatePendingVnpayBooking(clientId, workerId, 250_000m);
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            Amount = 250_000m,
            Method = PaymentMethod.Vnpay,
            Status = PaymentStatus.Pending,
            VnpTxnRef = "txn-123456",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var unitOfWork = new InMemoryUnitOfWork().With([booking]).With([payment]);
        var checkoutService = new Mock<IVnpayCheckoutService>();
        checkoutService.Setup(s => s.VerifyCallback(It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Returns(new VnpayCallbackResult(true, true, "txn-123456", 100_000m, "ref-1", "00"));
        var service = CreateService(unitOfWork, checkoutService.Object);

        var outcome = await service.ConfirmVnpayPaymentAsync(SuccessfulConfirmParams());

        Assert.Equal(VnpayConfirmOutcome.InvalidAmount, outcome);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
    }

    [Fact(DisplayName = "[UT-BE-CONFIRM-004] Xác nhận trùng lặp cho giao dịch đã thành công báo đã xác nhận, không ghi đè")]
    public async Task ConfirmVnpayPaymentAsync_DuplicateSuccessful_ReportsAlreadyConfirmed()
    {
        var clientId = Guid.NewGuid();
        var workerId = Guid.NewGuid();
        var booking = CreatePendingVnpayBooking(clientId, workerId, 250_000m);
        booking.Status = BookingStatus.Completed;
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            Amount = 250_000m,
            Method = PaymentMethod.Vnpay,
            Status = PaymentStatus.Success,
            VnpTxnRef = "txn-123456",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var unitOfWork = new InMemoryUnitOfWork().With([booking]).With([payment]).With(new List<WorkerEarning>());
        var checkoutService = new Mock<IVnpayCheckoutService>();
        checkoutService.Setup(s => s.VerifyCallback(It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Returns(new VnpayCallbackResult(true, true, "txn-123456", 250_000m, "ref-1", "00"));
        var service = CreateService(unitOfWork, checkoutService.Object);

        var outcome = await service.ConfirmVnpayPaymentAsync(SuccessfulConfirmParams());

        Assert.Equal(VnpayConfirmOutcome.OrderAlreadyConfirmed, outcome);
        Assert.Empty(unitOfWork.Repository<WorkerEarning>().GetQueryable());
    }

    [Fact(DisplayName = "[UT-BE-CONFIRM-005] Xác nhận thành công hoàn tất đơn, ghi nhận thu nhập pending và có " +
        "tính idempotent khi xác nhận gửi lại lần hai")]
    public async Task ConfirmVnpayPaymentAsync_Successful_CompletesBookingAndWritesEarningOnce()
    {
        var clientId = Guid.NewGuid();
        var workerId = Guid.NewGuid();
        var booking = CreatePendingVnpayBooking(clientId, workerId, 250_000m);
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            Amount = 250_000m,
            Method = PaymentMethod.Vnpay,
            Status = PaymentStatus.Pending,
            VnpTxnRef = "txn-123456",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var unitOfWork = new InMemoryUnitOfWork()
            .With([booking]).With([payment]).With(new List<WorkerEarning>()).With(new List<BookingStatusLog>());
        var checkoutService = new Mock<IVnpayCheckoutService>();
        checkoutService.Setup(s => s.VerifyCallback(It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Returns(new VnpayCallbackResult(true, true, "txn-123456", 250_000m, "ref-777", "00"));
        var service = CreateService(unitOfWork, checkoutService.Object);

        var outcome = await service.ConfirmVnpayPaymentAsync(SuccessfulConfirmParams());

        Assert.Equal(VnpayConfirmOutcome.Success, outcome);
        Assert.Equal(BookingStatus.Completed, booking.Status);
        Assert.Equal(PaymentStatus.Success, payment.Status);
        Assert.Equal("ref-777", payment.TransactionId);
        var earning = Assert.Single(unitOfWork.Repository<WorkerEarning>().GetQueryable());
        Assert.Equal("pending", earning.Status);
        Assert.Equal(workerId, earning.WorkerId);

        var secondOutcome = await service.ConfirmVnpayPaymentAsync(SuccessfulConfirmParams());
        Assert.Equal(VnpayConfirmOutcome.OrderAlreadyConfirmed, secondOutcome);
        Assert.Single(unitOfWork.Repository<WorkerEarning>().GetQueryable());
    }

    [Fact(DisplayName = "[UT-BE-CONFIRM-006] Xác nhận báo giao dịch thất bại tại VNPay: đơn giữ nguyên, thanh toán đánh dấu Failed")]
    public async Task ConfirmVnpayPaymentAsync_BankDeclined_MarksPaymentFailedWithoutCompletingBooking()
    {
        var clientId = Guid.NewGuid();
        var workerId = Guid.NewGuid();
        var booking = CreatePendingVnpayBooking(clientId, workerId, 250_000m);
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            Amount = 250_000m,
            Method = PaymentMethod.Vnpay,
            Status = PaymentStatus.Pending,
            VnpTxnRef = "txn-123456",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var unitOfWork = new InMemoryUnitOfWork().With([booking]).With([payment]).With(new List<WorkerEarning>());
        var checkoutService = new Mock<IVnpayCheckoutService>();
        checkoutService.Setup(s => s.VerifyCallback(It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Returns(new VnpayCallbackResult(true, false, "txn-123456", 250_000m, null, "24"));
        var service = CreateService(unitOfWork, checkoutService.Object);

        var outcome = await service.ConfirmVnpayPaymentAsync(new Dictionary<string, string>
        {
            ["vnp_TxnRef"] = "txn-123456",
            ["vnp_ResponseCode"] = "24",
            ["vnp_TransactionStatus"] = "02"
        });

        Assert.Equal(VnpayConfirmOutcome.Success, outcome);
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("24", payment.FailureCode);
        Assert.Equal(BookingStatus.PendingPayment, booking.Status);
        Assert.Empty(unitOfWork.Repository<WorkerEarning>().GetQueryable());
    }
}

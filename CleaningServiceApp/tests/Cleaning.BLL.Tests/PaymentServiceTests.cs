using AutoMapper;
using Cleaning.BLL.Common;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.BLL.Mapping;
using Cleaning.BLL.Services;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Cleaning.BLL.Tests;

public class PaymentServiceTests
{
    private static IMapper CreateMapper() =>
        new MapperConfiguration(
            configuration => configuration.AddProfile<BookingMappingProfile>(),
            NullLoggerFactory.Instance).CreateMapper();

    private static PaymentService CreateService(
        InMemoryUnitOfWork unitOfWork,
        IPayOsCheckoutService? checkoutService = null,
        IPayoutGateway? payoutGateway = null) =>
        new(unitOfWork, NullLogger<PaymentService>.Instance, CreateMapper(),
            checkoutService ?? Mock.Of<IPayOsCheckoutService>(),
            payoutGateway ?? Mock.Of<IPayoutGateway>());

    private static Booking CreatePendingPayosBooking(Guid clientId, Guid workerId, decimal totalPrice) => new()
    {
        Id = Guid.NewGuid(),
        ClientId = clientId,
        WorkerId = workerId,
        Status = BookingStatus.PendingPayment,
        PaymentMethod = PaymentMethod.Payos,
        TotalPrice = totalPrice,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Fact(DisplayName = "[UT-BE-PAY-001-01] Pay-now dùng giá booking từ máy chủ, bỏ qua mọi giá trị client gửi lên")]
    public async Task PayNowAsync_UsesServerBookingPrice()
    {
        var clientId = Guid.NewGuid();
        var workerId = Guid.NewGuid();
        var booking = CreatePendingPayosBooking(clientId, workerId, 250_000m);
        var unitOfWork = new InMemoryUnitOfWork().With([booking]).With(new List<Payment>());

        var checkoutService = new Mock<IPayOsCheckoutService>();
        checkoutService
            .Setup(s => s.CreatePaymentLinkAsync(250_000m, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayOsCheckoutLink(123456, "https://pay.payos.vn/web/abc123"));
        var service = CreateService(unitOfWork, checkoutService.Object);

        var result = await service.PayNowAsync(clientId, new PayNowRequestDto { BookingId = booking.Id }, "127.0.0.1");

        Assert.Equal("https://pay.payos.vn/web/abc123", result.PaymentUrl);
        var payment = Assert.Single(unitOfWork.Repository<Payment>().GetQueryable());
        Assert.Equal(250_000m, payment.Amount);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(123456, payment.PayosOrderCode);
    }

    [Fact(DisplayName = "[UT-BE-PAY-002] Pay-now bị từ chối nếu người gọi không phải chủ đơn")]
    public async Task PayNowAsync_NonOwningClient_Throws()
    {
        var clientId = Guid.NewGuid();
        var booking = CreatePendingPayosBooking(clientId, Guid.NewGuid(), 100_000m);
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
        var booking = CreatePendingPayosBooking(clientId, Guid.NewGuid(), 100_000m);
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
        var booking = CreatePendingPayosBooking(clientId, Guid.NewGuid(), 100_000m);
        booking.PaymentMethod = PaymentMethod.Cash;
        var unitOfWork = new InMemoryUnitOfWork().With([booking]);
        var service = CreateService(unitOfWork);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.PayNowAsync(clientId, new PayNowRequestDto { BookingId = booking.Id }, "127.0.0.1"));

        Assert.Equal(AppErrors.PaymentMethodNotPayos.Code, exception.Code);
    }

    [Fact(DisplayName = "[UT-BE-PAY-005] Pay-now bị từ chối nếu đơn đã thanh toán thành công")]
    public async Task PayNowAsync_AlreadyCompleted_Throws()
    {
        var clientId = Guid.NewGuid();
        var booking = CreatePendingPayosBooking(clientId, Guid.NewGuid(), 100_000m);
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            Amount = 100_000m,
            Method = PaymentMethod.Payos,
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

    [Fact(DisplayName = "[UT-BE-WEBHOOK-001] Webhook không xác thực được chữ ký bị từ chối và không thay đổi dữ liệu")]
    public async Task ProcessPayOsWebhookAsync_InvalidSignature_Rejected()
    {
        var unitOfWork = new InMemoryUnitOfWork().With(new List<Payment>());
        var checkoutService = new Mock<IPayOsCheckoutService>();
        checkoutService.Setup(s => s.VerifyWebhookAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PayOsWebhookResult?)null);
        var service = CreateService(unitOfWork, checkoutService.Object);

        var success = await service.ProcessPayOsWebhookAsync("{}");

        Assert.False(success);
    }

    [Fact(DisplayName = "[UT-BE-WEBHOOK-002] Webhook với orderCode không tồn tại bị từ chối")]
    public async Task ProcessPayOsWebhookAsync_UnknownOrderCode_Rejected()
    {
        var unitOfWork = new InMemoryUnitOfWork().With(new List<Payment>());
        var checkoutService = new Mock<IPayOsCheckoutService>();
        checkoutService.Setup(s => s.VerifyWebhookAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayOsWebhookResult(true, 999999, 100_000m, "ref-1"));
        var service = CreateService(unitOfWork, checkoutService.Object);

        var success = await service.ProcessPayOsWebhookAsync("{}");

        Assert.False(success);
    }

    [Fact(DisplayName = "[UT-BE-WEBHOOK-003] Webhook với số tiền không khớp bị từ chối")]
    public async Task ProcessPayOsWebhookAsync_AmountMismatch_Rejected()
    {
        var clientId = Guid.NewGuid();
        var workerId = Guid.NewGuid();
        var booking = CreatePendingPayosBooking(clientId, workerId, 250_000m);
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            Amount = 250_000m,
            Method = PaymentMethod.Payos,
            Status = PaymentStatus.Pending,
            PayosOrderCode = 123456,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var unitOfWork = new InMemoryUnitOfWork().With([booking]).With([payment]);
        var checkoutService = new Mock<IPayOsCheckoutService>();
        checkoutService.Setup(s => s.VerifyWebhookAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayOsWebhookResult(true, 123456, 100_000m, "ref-1"));
        var service = CreateService(unitOfWork, checkoutService.Object);

        var success = await service.ProcessPayOsWebhookAsync("{}");

        Assert.False(success);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
    }

    [Fact(DisplayName = "[UT-BE-WEBHOOK-004] Webhook trùng lặp cho giao dịch đã thành công là no-op")]
    public async Task ProcessPayOsWebhookAsync_DuplicateSuccessful_NoOp()
    {
        var clientId = Guid.NewGuid();
        var workerId = Guid.NewGuid();
        var booking = CreatePendingPayosBooking(clientId, workerId, 250_000m);
        booking.Status = BookingStatus.Completed;
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            Amount = 250_000m,
            Method = PaymentMethod.Payos,
            Status = PaymentStatus.Success,
            PayosOrderCode = 123456,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var unitOfWork = new InMemoryUnitOfWork().With([booking]).With([payment]).With(new List<WorkerEarning>());
        var checkoutService = new Mock<IPayOsCheckoutService>();
        checkoutService.Setup(s => s.VerifyWebhookAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayOsWebhookResult(true, 123456, 250_000m, "ref-1"));
        var service = CreateService(unitOfWork, checkoutService.Object);

        var success = await service.ProcessPayOsWebhookAsync("{}");

        Assert.True(success);
        Assert.Empty(unitOfWork.Repository<WorkerEarning>().GetQueryable());
    }

    [Fact(DisplayName = "[UT-BE-WEBHOOK-005] Webhook thành công hoàn tất đơn, ghi nhận thu nhập pending và có " +
        "tính idempotent khi webhook gửi lại lần hai")]
    public async Task ProcessPayOsWebhookAsync_Successful_CompletesBookingAndWritesEarningOnce()
    {
        var clientId = Guid.NewGuid();
        var workerId = Guid.NewGuid();
        var booking = CreatePendingPayosBooking(clientId, workerId, 250_000m);
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            Amount = 250_000m,
            Method = PaymentMethod.Payos,
            Status = PaymentStatus.Pending,
            PayosOrderCode = 123456,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var unitOfWork = new InMemoryUnitOfWork()
            .With([booking]).With([payment]).With(new List<WorkerEarning>()).With(new List<BookingStatusLog>());
        var checkoutService = new Mock<IPayOsCheckoutService>();
        checkoutService.Setup(s => s.VerifyWebhookAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayOsWebhookResult(true, 123456, 250_000m, "ref-777"));
        var service = CreateService(unitOfWork, checkoutService.Object);

        var success = await service.ProcessPayOsWebhookAsync("{}");

        Assert.True(success);
        Assert.Equal(BookingStatus.Completed, booking.Status);
        Assert.Equal(PaymentStatus.Success, payment.Status);
        Assert.Equal("ref-777", payment.TransactionId);
        var earning = Assert.Single(unitOfWork.Repository<WorkerEarning>().GetQueryable());
        Assert.Equal("pending", earning.Status);
        Assert.Equal(workerId, earning.WorkerId);

        var secondSuccess = await service.ProcessPayOsWebhookAsync("{}");
        Assert.True(secondSuccess);
        Assert.Single(unitOfWork.Repository<WorkerEarning>().GetQueryable());
    }

    private static (Booking booking, Payment payment, InMemoryUnitOfWork unitOfWork) SetUpSuccessfulWebhook(
        Guid workerId, WorkerProfile? workerProfile = null)
    {
        var clientId = Guid.NewGuid();
        var booking = CreatePendingPayosBooking(clientId, workerId, 250_000m);
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            Amount = 250_000m,
            Method = PaymentMethod.Payos,
            Status = PaymentStatus.Pending,
            PayosOrderCode = 123456,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var unitOfWork = new InMemoryUnitOfWork()
            .With([booking]).With([payment]).With(new List<WorkerEarning>()).With(new List<BookingStatusLog>())
            .With(workerProfile != null ? [workerProfile] : new List<WorkerProfile>());
        return (booking, payment, unitOfWork);
    }

    private static Mock<IPayOsCheckoutService> SuccessfulCheckoutService() =>
        new Mock<IPayOsCheckoutService>()
            .Also(m => m.Setup(s => s.VerifyWebhookAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PayOsWebhookResult(true, 123456, 250_000m, "ref-1")));

    [Fact(DisplayName = "[UT-BE-WEBHOOK-006] Thợ chưa cấu hình tài khoản nhận tiền: thu nhập giữ nguyên pending, " +
        "không gọi cổng chi trả")]
    public async Task ProcessPayOsWebhookAsync_NoPayoutAccount_StaysPending()
    {
        var workerId = Guid.NewGuid();
        var (_, _, unitOfWork) = SetUpSuccessfulWebhook(
            workerId, new WorkerProfile { UserId = workerId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        var checkoutService = SuccessfulCheckoutService();
        var payoutGateway = new Mock<IPayoutGateway>(MockBehavior.Strict);
        var service = CreateService(unitOfWork, checkoutService.Object, payoutGateway.Object);

        var success = await service.ProcessPayOsWebhookAsync("{}");

        Assert.True(success);
        var earning = Assert.Single(unitOfWork.Repository<WorkerEarning>().GetQueryable());
        Assert.Equal("pending", earning.Status);
        Assert.Null(earning.PayoutId);
        payoutGateway.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "[UT-BE-WEBHOOK-007] Chi trả tự động thành công ngay: thu nhập chuyển sang paid kèm PaidAt")]
    public async Task ProcessPayOsWebhookAsync_PayoutSucceeds_MarksPaid()
    {
        var workerId = Guid.NewGuid();
        var workerProfile = new WorkerProfile
        {
            UserId = workerId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            PayoutBankBin = "970422", PayoutBankAccountNumber = "0123456789"
        };
        var (_, _, unitOfWork) = SetUpSuccessfulWebhook(workerId, workerProfile);
        var checkoutService = SuccessfulCheckoutService();
        var payoutGateway = new Mock<IPayoutGateway>();
        payoutGateway
            .Setup(g => g.PayAsync(
                It.IsAny<Guid>(), 250_000m, "970422", "0123456789", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayoutResult(PayoutState.Succeeded, "payout-1", null));
        var service = CreateService(unitOfWork, checkoutService.Object, payoutGateway.Object);

        await service.ProcessPayOsWebhookAsync("{}");

        var earning = Assert.Single(unitOfWork.Repository<WorkerEarning>().GetQueryable());
        Assert.Equal("paid", earning.Status);
        Assert.Equal("payout-1", earning.PayoutId);
        Assert.NotNull(earning.PaidAt);
    }

    [Fact(DisplayName = "[UT-BE-WEBHOOK-008] Chi trả tự động đang xử lý: thu nhập chuyển sang processing kèm PayoutId")]
    public async Task ProcessPayOsWebhookAsync_PayoutProcessing_MarksProcessing()
    {
        var workerId = Guid.NewGuid();
        var workerProfile = new WorkerProfile
        {
            UserId = workerId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            PayoutBankBin = "970422", PayoutBankAccountNumber = "0123456789"
        };
        var (_, _, unitOfWork) = SetUpSuccessfulWebhook(workerId, workerProfile);
        var checkoutService = SuccessfulCheckoutService();
        var payoutGateway = new Mock<IPayoutGateway>();
        payoutGateway
            .Setup(g => g.PayAsync(
                It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PayoutResult(PayoutState.Processing, "payout-2", null));
        var service = CreateService(unitOfWork, checkoutService.Object, payoutGateway.Object);

        await service.ProcessPayOsWebhookAsync("{}");

        var earning = Assert.Single(unitOfWork.Repository<WorkerEarning>().GetQueryable());
        Assert.Equal("processing", earning.Status);
        Assert.Equal("payout-2", earning.PayoutId);
        Assert.Null(earning.PaidAt);
    }

    [Fact(DisplayName = "[UT-BE-WEBHOOK-009] Chi trả tự động lỗi: thu nhập giữ pending kèm lý do lỗi, webhook " +
        "vẫn báo thành công cho payOS")]
    public async Task ProcessPayOsWebhookAsync_PayoutThrows_KeepsPendingButWebhookStillSucceeds()
    {
        var workerId = Guid.NewGuid();
        var workerProfile = new WorkerProfile
        {
            UserId = workerId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            PayoutBankBin = "970422", PayoutBankAccountNumber = "0123456789"
        };
        var (_, _, unitOfWork) = SetUpSuccessfulWebhook(workerId, workerProfile);
        var checkoutService = SuccessfulCheckoutService();
        var payoutGateway = new Mock<IPayoutGateway>();
        payoutGateway
            .Setup(g => g.PayAsync(
                It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("payOS unreachable"));
        var service = CreateService(unitOfWork, checkoutService.Object, payoutGateway.Object);

        var success = await service.ProcessPayOsWebhookAsync("{}");

        Assert.True(success);
        var earning = Assert.Single(unitOfWork.Repository<WorkerEarning>().GetQueryable());
        Assert.Equal("pending", earning.Status);
        Assert.Null(earning.PayoutId);
    }
}

internal static class MockExtensions
{
    public static Mock<T> Also<T>(this Mock<T> mock, Action<Mock<T>> configure) where T : class
    {
        configure(mock);
        return mock;
    }
}

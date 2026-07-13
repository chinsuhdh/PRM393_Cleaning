using AutoMapper;
using Cleaning.BLL.Common;
using Cleaning.BLL.Constants;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Cleaning.DAL.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cleaning.BLL.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PaymentService> _logger;
        private readonly IMapper _mapper;
        private readonly IPayOsCheckoutService _checkoutService;
        private readonly IPayoutGateway _payoutGateway;
        private readonly IDispatchPublisher? _dispatchPublisher;

        public PaymentService(
            IUnitOfWork unitOfWork,
            ILogger<PaymentService> logger,
            IMapper mapper,
            IPayOsCheckoutService checkoutService,
            IPayoutGateway payoutGateway,
            IDispatchPublisher? dispatchPublisher = null)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
            _checkoutService = checkoutService;
            _payoutGateway = payoutGateway;
            _dispatchPublisher = dispatchPublisher;
        }

        public async Task<PayNowResponseDto> PayNowAsync(Guid clientId, PayNowRequestDto request, string ipAddress)
        {
            var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(request.BookingId)
                          ?? throw new AppException(AppErrors.BookingNotFound);

            if (booking.ClientId != clientId)
                throw new AppException(AppErrors.Forbidden);
            if (booking.Status != BookingStatus.PendingPayment)
                throw new AppException(AppErrors.BookingNotPendingPayment);
            if (booking.PaymentMethod != PaymentMethod.Payos)
                throw new AppException(AppErrors.PaymentMethodNotPayos);

            var paymentRecord = (await _unitOfWork.Repository<Payment>()
                .FindAsync(p => p.BookingId == request.BookingId)).FirstOrDefault();

            if (paymentRecord != null && paymentRecord.Status == PaymentStatus.Success)
                throw new AppException(AppErrors.PaymentAlreadyCompleted);

            var link = await _checkoutService.CreatePaymentLinkAsync(booking.TotalPrice, $"Thanh toan don {booking.Id}");

            var now = DateTime.UtcNow;
            if (paymentRecord == null)
            {
                paymentRecord = new Payment
                {
                    BookingId = booking.Id,
                    Amount = booking.TotalPrice,
                    Method = PaymentMethod.Payos,
                    Status = PaymentStatus.Pending,
                    PayosOrderCode = link.OrderCode,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                await _unitOfWork.Repository<Payment>().AddAsync(paymentRecord);
            }
            else
            {
                paymentRecord.Amount = booking.TotalPrice;
                paymentRecord.Method = PaymentMethod.Payos;
                paymentRecord.Status = PaymentStatus.Pending;
                paymentRecord.PayosOrderCode = link.OrderCode;
                paymentRecord.UpdatedAt = now;
                _unitOfWork.Repository<Payment>().Update(paymentRecord);
            }

            await _unitOfWork.SaveChangesAsync();

            return new PayNowResponseDto { PaymentId = paymentRecord.Id, PaymentUrl = link.CheckoutUrl };
        }

        public async Task<PaymentDto?> GetPaymentByBookingAsync(Guid bookingId)
        {
            var payments = await _unitOfWork.Repository<Payment>().FindAsync(p => p.BookingId == bookingId);
            var payment = payments.FirstOrDefault();

            return payment != null ? _mapper.Map<PaymentDto>(payment) : null;
        }

        public async Task<bool> ProcessPayOsWebhookAsync(string rawJson)
        {
            var webhookResult = await _checkoutService.VerifyWebhookAsync(rawJson);
            if (webhookResult == null || !webhookResult.Success)
                return false;

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var payment = (await _unitOfWork.Repository<Payment>()
                    .FindAsync(p => p.PayosOrderCode == webhookResult.OrderCode)).FirstOrDefault();
                if (payment == null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                if (payment.Amount != webhookResult.Amount)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                if (payment.Status == PaymentStatus.Success)
                {
                    await transaction.RollbackAsync();
                    return true;
                }

                var now = DateTime.UtcNow;
                Guid? completedBookingId = null;
                WorkerEarning? newEarning = null;

                payment.Status = PaymentStatus.Success;
                payment.TransactionId = webhookResult.Reference;
                payment.CallbackVerifiedAt = now;
                payment.PaidAt = now;
                payment.UpdatedAt = now;
                _unitOfWork.Repository<Payment>().Update(payment);

                var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(payment.BookingId);
                if (booking != null && booking.Status == BookingStatus.PendingPayment && booking.WorkerId.HasValue)
                {
                    var oldStatus = booking.Status;
                    booking.Status = BookingStatus.Completed;
                    booking.UpdatedAt = now;
                    _unitOfWork.Repository<Booking>().Update(booking);

                    await _unitOfWork.Repository<BookingStatusLog>().AddAsync(new BookingStatusLog
                    {
                        BookingId = booking.Id,
                        OldStatus = oldStatus,
                        NewStatus = BookingStatus.Completed,
                        ChangedBy = null,
                        Reason = BookingReasons.SystemConfirmedPayosPayment,
                        CreatedAt = now
                    });

                    if (!await _unitOfWork.Repository<WorkerEarning>().ExistsAsync(e => e.BookingId == booking.Id))
                    {
                        newEarning = new WorkerEarning
                        {
                            BookingId = booking.Id,
                            WorkerId = booking.WorkerId.Value,
                            Amount = booking.TotalPrice,
                            Status = "pending",
                            EarnedAt = now,
                            CreatedAt = now
                        };
                        await _unitOfWork.Repository<WorkerEarning>().AddAsync(newEarning);
                    }

                    var workerProfile = await _unitOfWork.Repository<WorkerProfile>()
                        .GetByIdAsync(booking.WorkerId.Value);
                    if (workerProfile?.OnlineStatus == WorkerOnlineStatus.Busy)
                    {
                        workerProfile.OnlineStatus = WorkerOnlineStatus.Online;
                        workerProfile.UpdatedAt = now;
                        _unitOfWork.Repository<WorkerProfile>().Update(workerProfile);
                    }

                    completedBookingId = booking.Id;
                }

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                if (completedBookingId.HasValue && _dispatchPublisher != null)
                    await _dispatchPublisher.BookingStatusChangedAsync(completedBookingId.Value, nameof(BookingStatus.Completed));

                if (newEarning != null)
                    await TryAutoPayoutAsync(newEarning);

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi xử lý payOS webhook cho OrderCode: {OrderCode}", webhookResult.OrderCode);
                return false;
            }
        }

        private async Task TryAutoPayoutAsync(WorkerEarning earning)
        {
            try
            {
                var workerProfile = await _unitOfWork.Repository<WorkerProfile>().GetByIdAsync(earning.WorkerId);
                if (string.IsNullOrWhiteSpace(workerProfile?.PayoutBankBin) ||
                    string.IsNullOrWhiteSpace(workerProfile.PayoutBankAccountNumber))
                    return;

                var result = await _payoutGateway.PayAsync(
                    earning.Id, earning.Amount, workerProfile.PayoutBankBin, workerProfile.PayoutBankAccountNumber,
                    $"Thanh toan don {earning.BookingId}");

                earning.PayoutId = result.PayoutId;
                earning.PayoutFailureReason = result.FailureReason;
                if (result.State == PayoutState.Succeeded)
                {
                    earning.Status = "paid";
                    earning.PaidAt = DateTime.UtcNow;
                }
                else if (result.State == PayoutState.Processing)
                {
                    earning.Status = "processing";
                }

                _unitOfWork.Repository<WorkerEarning>().Update(earning);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tự động chi trả cho thợ thất bại, EarningId: {EarningId}", earning.Id);
            }
        }
    }
}

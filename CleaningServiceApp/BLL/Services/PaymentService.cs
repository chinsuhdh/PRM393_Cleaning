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
        private readonly IVnpayCheckoutService _checkoutService;
        private readonly IDispatchPublisher? _dispatchPublisher;

        public PaymentService(
            IUnitOfWork unitOfWork,
            ILogger<PaymentService> logger,
            IMapper mapper,
            IVnpayCheckoutService checkoutService,
            IDispatchPublisher? dispatchPublisher = null)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
            _checkoutService = checkoutService;
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
            if (booking.PaymentMethod != PaymentMethod.Vnpay)
                throw new AppException(AppErrors.PaymentMethodNotVnpay);

            var paymentRecord = (await _unitOfWork.Repository<Payment>()
                .FindAsync(p => p.BookingId == request.BookingId)).FirstOrDefault();

            if (paymentRecord != null && paymentRecord.Status == PaymentStatus.Success)
                throw new AppException(AppErrors.PaymentAlreadyCompleted);

            var link = _checkoutService.CreatePaymentUrl(booking.TotalPrice, $"Thanh toan don {booking.Id}", ipAddress);

            var now = DateTime.UtcNow;
            if (paymentRecord == null)
            {
                paymentRecord = new Payment
                {
                    BookingId = booking.Id,
                    Amount = booking.TotalPrice,
                    Method = PaymentMethod.Vnpay,
                    Status = PaymentStatus.Pending,
                    VnpTxnRef = link.TxnRef,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                await _unitOfWork.Repository<Payment>().AddAsync(paymentRecord);
            }
            else
            {
                paymentRecord.Amount = booking.TotalPrice;
                paymentRecord.Method = PaymentMethod.Vnpay;
                paymentRecord.Status = PaymentStatus.Pending;
                paymentRecord.VnpTxnRef = link.TxnRef;
                paymentRecord.UpdatedAt = now;
                _unitOfWork.Repository<Payment>().Update(paymentRecord);
            }

            await _unitOfWork.SaveChangesAsync();

            return new PayNowResponseDto { PaymentId = paymentRecord.Id, PaymentUrl = link.PaymentUrl };
        }

        public async Task<PaymentDto?> GetPaymentByBookingAsync(Guid bookingId)
        {
            var payments = await _unitOfWork.Repository<Payment>().FindAsync(p => p.BookingId == bookingId);
            var payment = payments.FirstOrDefault();

            return payment != null ? _mapper.Map<PaymentDto>(payment) : null;
        }

        public async Task<VnpayConfirmOutcome> ConfirmVnpayPaymentAsync(IReadOnlyDictionary<string, string> queryParams)
        {
            var callback = _checkoutService.VerifyCallback(queryParams);
            if (!callback.SignatureValid)
                return VnpayConfirmOutcome.InvalidSignature;

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var payment = (await _unitOfWork.Repository<Payment>()
                    .FindAsync(p => p.VnpTxnRef == callback.TxnRef)).FirstOrDefault();
                if (payment == null)
                {
                    await transaction.RollbackAsync();
                    return VnpayConfirmOutcome.OrderNotFound;
                }

                if (payment.Amount != callback.Amount)
                {
                    await transaction.RollbackAsync();
                    return VnpayConfirmOutcome.InvalidAmount;
                }

                if (payment.Status == PaymentStatus.Success)
                {
                    await transaction.RollbackAsync();
                    return VnpayConfirmOutcome.OrderAlreadyConfirmed;
                }

                if (!callback.Success)
                {
                    var now = DateTime.UtcNow;
                    payment.Status = PaymentStatus.Failed;
                    payment.FailureCode = callback.ResponseCode;
                    payment.UpdatedAt = now;
                    _unitOfWork.Repository<Payment>().Update(payment);
                    await _unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return VnpayConfirmOutcome.Success;
                }

                var completedAt = DateTime.UtcNow;
                Guid? completedBookingId = null;

                payment.Status = PaymentStatus.Success;
                payment.TransactionId = callback.TransactionNo;
                payment.CallbackVerifiedAt = completedAt;
                payment.PaidAt = completedAt;
                payment.UpdatedAt = completedAt;
                _unitOfWork.Repository<Payment>().Update(payment);

                var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(payment.BookingId);
                if (booking != null && booking.Status == BookingStatus.PendingPayment && booking.WorkerId.HasValue)
                {
                    var oldStatus = booking.Status;
                    booking.Status = BookingStatus.Completed;
                    booking.UpdatedAt = completedAt;
                    _unitOfWork.Repository<Booking>().Update(booking);

                    await _unitOfWork.Repository<BookingStatusLog>().AddAsync(new BookingStatusLog
                    {
                        BookingId = booking.Id,
                        OldStatus = oldStatus,
                        NewStatus = BookingStatus.Completed,
                        ChangedBy = null,
                        Reason = BookingReasons.SystemConfirmedVnpayPayment,
                        CreatedAt = completedAt
                    });

                    if (!await _unitOfWork.Repository<WorkerEarning>().ExistsAsync(e => e.BookingId == booking.Id))
                    {
                        await _unitOfWork.Repository<WorkerEarning>().AddAsync(new WorkerEarning
                        {
                            BookingId = booking.Id,
                            WorkerId = booking.WorkerId.Value,
                            Amount = booking.TotalPrice,
                            Status = "pending",
                            EarnedAt = completedAt,
                            CreatedAt = completedAt
                        });
                    }

                    var workerProfile = await _unitOfWork.Repository<WorkerProfile>()
                        .GetByIdAsync(booking.WorkerId.Value);
                    if (workerProfile?.OnlineStatus == WorkerOnlineStatus.Busy)
                    {
                        workerProfile.OnlineStatus = WorkerOnlineStatus.Online;
                        workerProfile.UpdatedAt = completedAt;
                        _unitOfWork.Repository<WorkerProfile>().Update(workerProfile);
                    }

                    completedBookingId = booking.Id;
                }

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                if (completedBookingId.HasValue && _dispatchPublisher != null)
                    await _dispatchPublisher.BookingStatusChangedAsync(completedBookingId.Value, nameof(BookingStatus.Completed));

                return VnpayConfirmOutcome.Success;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi xử lý VNPay IPN cho TxnRef: {TxnRef}", callback.TxnRef);
                return VnpayConfirmOutcome.UnknownError;
            }
        }
    }
}

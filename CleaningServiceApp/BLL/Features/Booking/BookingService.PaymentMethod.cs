using Cleaning.BLL.Common;
using Cleaning.BLL.Constants;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Microsoft.Extensions.Logging;

namespace Cleaning.BLL.Services;

public partial class BookingService
{
    public async Task SwitchToCashAsync(Guid bookingId, Guid clientId)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId);
            if (booking == null || booking.ClientId != clientId)
                throw new AppException(AppErrors.Forbidden);
            if (booking.Status != BookingStatus.PendingPayment)
                throw new AppException(AppErrors.BookingNotPendingPayment);
            if (booking.PaymentMethod == PaymentMethod.Cash)
                throw new AppException(AppErrors.PaymentMethodAlreadyCash);

            booking.PaymentMethod = PaymentMethod.Cash;
            booking.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<Booking>().Update(booking);

            await _unitOfWork.Repository<BookingStatusLog>().AddAsync(new BookingStatusLog
            {
                BookingId = booking.Id,
                OldStatus = BookingStatus.PendingPayment,
                NewStatus = BookingStatus.PendingPayment,
                ChangedBy = clientId,
                Reason = BookingReasons.ClientSwitchedToCash,
                CreatedAt = DateTime.UtcNow
            });

            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            if (_dispatchPublisher != null)
                await _dispatchPublisher.BookingStatusChangedAsync(booking.Id, clientId, booking.Status.ToString());
        }
        catch (AppException)
        {
            await transaction.RollbackAsync();
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Lỗi khi chuyển đơn sang thanh toán tiền mặt BookingId: {BookingId}", bookingId);
            throw new AppException(AppErrors.InternalError);
        }
    }
}

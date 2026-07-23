using Cleaning.BLL.Common;
using Cleaning.BLL.Constants;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;

namespace Cleaning.BLL.Features.Bookings;

public partial class BookingService
{
    public async Task SwitchToCashAsync(Guid bookingId, Guid clientId)
    {
        var booking = await _unitOfWork.ExecuteInTransactionAsync(
            _logger, AppErrors.InternalError, async () =>
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

            return booking;
        });

        if (_dispatchPublisher != null)
            await _dispatchPublisher.BookingStatusChangedAsync(booking.Id, clientId, booking.Status.ToString());
    }
}

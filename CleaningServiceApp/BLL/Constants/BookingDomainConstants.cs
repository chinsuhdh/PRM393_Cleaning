namespace Cleaning.BLL.Constants;

public static class BookingDomainConstants
{
    public const string WorkerVerificationStatusApproved = "approved";
    public const string PromotionStatusActive = "active";
    public const int MaxPhotosPerBooking = 5;
    public const int MaxPhotoRequestBytes = 5_242_880;
    public const int MaxPhotoBytes = 1_048_576;
}

// BookingStatusLog.Reason values — echoed to the client via BookingDto.StatusTimeline, so these
// must stay Vietnamese like every other user-facing string in the booking flow.
public static class BookingReasons
{
    public const string ClientCreatedBooking = "Khách hàng tạo đơn đặt lịch";
    public const string SystemAutoChargedVnpay = "Hệ thống tự động thanh toán VNPay (mô phỏng)";
    public const string SystemConfirmedCashPayment = "Hệ thống xác nhận thanh toán thành công";
    public const string WorkerAcceptedBooking = "Nhân viên đã nhận đơn";
}

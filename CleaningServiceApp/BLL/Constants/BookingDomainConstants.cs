namespace Cleaning.BLL.Constants;

public static class BookingDomainConstants
{
    public const string WorkerVerificationStatusApproved = "approved";
    public const string PromotionStatusActive = "active";
    public const int MaxPhotosPerBooking = 5;
    public const int MaxPhotoRequestBytes = 5_242_880;
    public const int MaxPhotoBytes = 1_048_576;
}

public static class BookingReasons
{
    public const string ClientCreatedBooking = "Khách hàng tạo đơn đặt lịch";
    public const string SystemAutoChargedVnpay = "Hệ thống tự động thanh toán VNPay (mô phỏng)";
    public const string SystemConfirmedCashPayment = "Hệ thống xác nhận thanh toán thành công";
    public const string WorkerAcceptedBooking = "Nhân viên đã nhận đơn";

    public const string RescheduleProposed = "Đã đề nghị dời lịch hẹn";
    public const string RescheduleAccepted = "Đã đồng ý dời lịch hẹn";
    public const string RescheduleRejected = "Đã từ chối dời lịch hẹn";
    public const string RescheduleWithdrawn = "Đã hủy đề nghị dời lịch hẹn";
    public const string SystemRescheduleExpired = "Hệ thống tự động từ chối do quá hạn phản hồi";
    public const string SystemAutoCancelledUnanswered = "Hệ thống tự động hủy do không tìm được nhân viên trước giờ hẹn";
}

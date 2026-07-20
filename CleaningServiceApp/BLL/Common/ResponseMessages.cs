namespace Cleaning.BLL.Common;

public static class ResponseMessages
{
    public const string Success = "Thành công.";
    public const string ValidationFailed = "Dữ liệu không hợp lệ.";
    public const string InternalError = "Đã xảy ra lỗi, vui lòng thử lại.";

    public const string RegisterSuccess = "Đăng ký thành công! Vui lòng kiểm tra email để nhận mã OTP.";
    public const string LogoutSuccess = "Đã đăng xuất khỏi thiết bị.";
    public const string ForgotPasswordOtpSent = "Mã OTP đã được gửi đến email của bạn.";
    public const string ResetPasswordSuccess = "Đặt lại mật khẩu thành công!";
    public const string VerifyAccountSuccess = "Xác thực tài khoản thành công! Bạn có thể đăng nhập.";
    public const string ChangePasswordSuccess = "Thay đổi mật khẩu thành công!";
    public const string PhoneOtpSent = "Mã xác thực SMS đã được gửi.";
    public const string VerifyPhoneSuccess = "Xác thực số điện thoại thành công!";

    public const string BookingStatusUpdated = "Cập nhật trạng thái đơn thành công.";
    public const string BookingAccepted = "Nhận đơn đặt lịch thành công!";
    public const string BroadcastRestarted = "Đã phát lại yêu cầu tìm nhân viên.";
    public const string JobHidden = "Đã ẩn công việc.";
    public const string BookingCancelled = "Đã hủy đơn đặt lịch.";
    public const string BookingWorkerCancelled = "Đã hủy nhận việc và trả đơn về danh sách chờ.";
    public const string BookingClientCancelled = "Đã hủy đơn và trả về danh sách chờ nhân viên.";
    public const string BookingReported = "Đã gửi báo cáo và hủy đơn đặt lịch.";
    public const string BookingSwitchedToCash = "Đã chuyển sang thanh toán tiền mặt.";

    public const string AiMatchingSuccess = "Đã chạy thuật toán Matching thành công.";

    public const string PaymentCallbackProcessed = "Đã cập nhật trạng thái thanh toán.";

    public const string ProfileUpdated = "Profile updated successfully.";
    public const string AvatarUploaded = "Upload ảnh đại diện thành công!";

    public const string AddressUpdated = "Address updated successfully.";
    public const string AddressDeleted = "Address deleted successfully.";
    public const string DefaultAddressUpdated = "Default address updated successfully.";

    public const string WorkerRegistered = "Worker profile registered successfully.";
    public const string WorkerLocationUpdated = "Location updated.";
    public const string WorkerSearchRadiusUpdated = "Search radius updated.";
    public const string WorkerOnlineStatusUpdated = "Online status updated.";
    public const string WorkerSkillUpdated = "Skill updated successfully.";
    public const string WorkerPayoutAccountUpdated = "Payout account updated successfully.";
}

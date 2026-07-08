namespace Cleaning.BLL.Common;

public sealed record AppError(string Code, string Message, int StatusCode = 400);

public static class AppErrors
{
    public static readonly AppError IdempotencyKeyRequired = new(
        "BOOKING_IDEMPOTENCY_KEY_REQUIRED", "Yêu cầu đặt dịch vụ không có mã chống trùng lặp hợp lệ.");
    public static readonly AppError ServiceUnavailable = new(
        "BOOKING_SERVICE_UNAVAILABLE", "Dịch vụ này hiện không khả dụng.", 404);
    public static readonly AppError AddressRequired = new(
        "BOOKING_ADDRESS_REQUIRED", "Vui lòng chọn địa chỉ trước khi đặt dịch vụ.");
    public static readonly AppError AddressForbidden = new(
        "BOOKING_ADDRESS_FORBIDDEN", "Bạn không thể sử dụng địa chỉ này.", 403);
    public static readonly AppError DurationInvalid = new(
        "BOOKING_DURATION_INVALID", "Thời lượng dịch vụ thấp hơn mức tối thiểu.");
    public static readonly AppError QuoteStale = new(
        "QUOTE_STALE", "Báo giá đã thay đổi. Vui lòng tải lại.", 409);
    public static readonly AppError OptionAnswersInvalid = new(
        "BOOKING_OPTION_ANSWERS_INVALID", "Thông tin trả lời cho dịch vụ không hợp lệ.");
    public static readonly AppError StartRequired = new(
        "BOOKING_START_REQUIRED", "Vui lòng chọn thời gian đặt dịch vụ.");
    public static readonly AppError StartTooSoon = new(
        "BOOKING_START_TOO_SOON", "Đặt lịch cần được thực hiện trước ít nhất 2 giờ.");
    public static readonly AppError TimeSlotInvalid = new(
        "TIME_SLOT_INVALID", "Thời gian đặt lịch phải nằm trong 30 ngày tới.");
    public static readonly AppError OutsideOperatingHours = new(
        "BOOKING_OUTSIDE_OPERATING_HOURS", "Thời gian đã chọn nằm ngoài giờ hoạt động của dịch vụ.");
    public static readonly AppError NoAvailableWorker = new(
        "BOOKING_NO_AVAILABLE_WORKER", "Không có nhân viên phù hợp trong thời gian đã chọn.", 409);
    public static readonly AppError ImmediateBookingAlreadyActive = new(
        "BOOKING_IMMEDIATE_ALREADY_ACTIVE",
        "Bạn đang có một đơn đặt ngay chờ nhân viên. Vui lòng chờ hoặc hủy đơn đó trước khi đặt đơn mới.", 409);
    public static readonly AppError BookingConflict = new(
        "BOOKING_CONFLICT", "Dữ liệu đặt dịch vụ đã thay đổi. Vui lòng thử lại.", 409);
    public static readonly AppError BookingCreateFailed = new(
        "BOOKING_CREATE_FAILED", "Không thể tạo đơn đặt dịch vụ. Vui lòng thử lại sau.", 500);

    public static readonly AppError ValidationError = new(
        "VALIDATION_ERROR", "Dữ liệu không hợp lệ.", 400);
    public static readonly AppError Unauthorized = new(
        "UNAUTHORIZED", "Bạn cần đăng nhập để thực hiện thao tác này.", 401);
    public static readonly AppError Forbidden = new(
        "FORBIDDEN", "Bạn không có quyền thực hiện thao tác này.", 403);
    public static readonly AppError NotFound = new(
        "NOT_FOUND", "Không tìm thấy dữ liệu yêu cầu.", 404);
    public static readonly AppError InternalError = new(
        "INTERNAL_ERROR", "Đã xảy ra lỗi, vui lòng thử lại.", 500);

    public static readonly AppError BookingNotFound = new(
        "BOOKING_NOT_FOUND", "Không tìm thấy đơn đặt lịch này.", 404);
    public static readonly AppError BookingStatusUpdateFailed = new(
        "BOOKING_STATUS_UPDATE_FAILED", "Không tìm thấy đơn đặt lịch này hoặc có lỗi xảy ra.", 404);
    public static readonly AppError BookingAcceptFailed = new(
        "BOOKING_ACCEPT_FAILED", "Đơn hàng này không hợp lệ, hoặc đã có thợ khác nhanh tay nhận mất rồi.", 409);

    public static readonly AppError AuthRegisterFailed = new(
        "AUTH_REGISTER_FAILED", "Email hoặc số điện thoại đã tồn tại, hoặc có lỗi hệ thống xảy ra.", 400);
    public static readonly AppError AuthInvalidCredentials = new(
        "AUTH_INVALID_CREDENTIALS", "Sai tài khoản hoặc mật khẩu.", 401);
    public static readonly AppError AuthInvalidRefreshToken = new(
        "AUTH_INVALID_REFRESH_TOKEN", "Refresh token không hợp lệ hoặc đã hết hạn.", 401);
    public static readonly AppError AuthEmailNotFound = new(
        "AUTH_EMAIL_NOT_FOUND", "Không tìm thấy email trong hệ thống.", 404);
    public static readonly AppError AuthResetPasswordFailed = new(
        "AUTH_RESET_PASSWORD_FAILED", "OTP sai, đã hết hạn hoặc đã được sử dụng.", 400);
    public static readonly AppError AuthVerifyAccountFailed = new(
        "AUTH_VERIFY_ACCOUNT_FAILED", "Mã OTP không hợp lệ, đã hết hạn hoặc tài khoản không tồn tại.", 400);
    public static readonly AppError AuthChangePasswordFailed = new(
        "AUTH_CHANGE_PASSWORD_FAILED", "Mật khẩu cũ không đúng hoặc tài khoản không tồn tại.", 400);
    public static readonly AppError AuthSendPhoneOtpFailed = new(
        "AUTH_SEND_PHONE_OTP_FAILED", "Không thể gửi OTP. Hãy kiểm tra lại số điện thoại trong hồ sơ.", 400);
    public static readonly AppError AuthVerifyPhoneFailed = new(
        "AUTH_VERIFY_PHONE_FAILED", "Mã OTP sai, hết hạn hoặc số điện thoại không tồn tại.", 400);

    public static readonly AppError AiMessageRequired = new(
        "AI_MESSAGE_REQUIRED", "Tin nhắn không được để trống.", 400);
    public static readonly AppError AiMatchingFailed = new(
        "AI_MATCHING_FAILED", "Không thể phân tích hoặc không tìm thấy thợ phù hợp.", 400);

    public static readonly AppError PaymentNotFound = new(
        "PAYMENT_NOT_FOUND", "Không tìm thấy thông tin thanh toán cho đơn này.", 404);
    public static readonly AppError PaymentCallbackFailed = new(
        "PAYMENT_CALLBACK_FAILED", "Không tìm thấy giao dịch hoặc lỗi hệ thống.", 404);
    public static readonly AppError VnpayNotLinked = new(
        "VNPAY_NOT_LINKED", "Bạn chưa liên kết tài khoản VNPay. Vui lòng liên kết trước khi chọn thanh toán VNPay.", 400);
    public static readonly AppError VnpayAccountInvalid = new(
        "VNPAY_ACCOUNT_INVALID", "Tài khoản VNPay không hợp lệ.", 400);

    public static readonly AppError ProfileNotFound = new(
        "PROFILE_NOT_FOUND", "Profile not found.", 404);
    public static readonly AppError ProfileUpdateFailed = new(
        "PROFILE_UPDATE_FAILED", "Profile not found or could not be updated.", 404);
    public static readonly AppError AvatarFileRequired = new(
        "AVATAR_FILE_REQUIRED", "Vui lòng chọn một file ảnh hợp lệ.", 400);
    public static readonly AppError AvatarFileTypeInvalid = new(
        "AVATAR_FILE_TYPE_INVALID", "Chỉ chấp nhận các định dạng file .jpg, .jpeg, .png", 400);
    public static readonly AppError AvatarUploadFailed = new(
        "AVATAR_UPLOAD_FAILED", "Lỗi hệ thống khi lưu file.", 500);

    public static readonly AppError ServiceNotFound = new(
        "SERVICE_NOT_FOUND", "Không tìm thấy dịch vụ hoặc dịch vụ đang tạm ngưng.", 404);

    public static readonly AppError AddressNotFound = new(
        "ADDRESS_NOT_FOUND", "Address not found.", 404);

    public static readonly AppError WorkerProfileNotFound = new(
        "WORKER_PROFILE_NOT_FOUND", "Worker profile not found.", 404);
    public static readonly AppError WorkerProfileExists = new(
        "WORKER_PROFILE_EXISTS", "Worker profile already exists.", 400);
}

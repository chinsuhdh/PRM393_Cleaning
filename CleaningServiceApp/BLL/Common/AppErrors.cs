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
    public static readonly AppError OptionAnswersInvalid = new(
        "BOOKING_OPTION_ANSWERS_INVALID", "Thông tin trả lời cho dịch vụ không hợp lệ.");
    public static readonly AppError StartRequired = new(
        "BOOKING_START_REQUIRED", "Vui lòng chọn thời gian đặt dịch vụ.");
    public static readonly AppError StartTooSoon = new(
        "BOOKING_START_TOO_SOON", "Đặt lịch cần được thực hiện trước ít nhất 2 giờ.");
    public static readonly AppError OutsideOperatingHours = new(
        "BOOKING_OUTSIDE_OPERATING_HOURS", "Thời gian đã chọn nằm ngoài giờ hoạt động của dịch vụ.");
    public static readonly AppError NoAvailableWorker = new(
        "BOOKING_NO_AVAILABLE_WORKER", "Không có nhân viên phù hợp trong thời gian đã chọn.", 409);
    public static readonly AppError SlotUnavailable = new(
        "BOOKING_SLOT_UNAVAILABLE", "Khung giờ đã chọn không còn khả dụng.", 409);
    public static readonly AppError BookingConflict = new(
        "BOOKING_CONFLICT", "Dữ liệu đặt dịch vụ đã thay đổi. Vui lòng thử lại.", 409);
    public static readonly AppError BookingCreateFailed = new(
        "BOOKING_CREATE_FAILED", "Không thể tạo đơn đặt dịch vụ. Vui lòng thử lại sau.", 500);
}

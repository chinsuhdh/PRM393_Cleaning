namespace Cleaning.BLL.Constants;

public static class AiConstants
{
    public const int MaxRelevantDocuments = 3;
    public const int MaxHistoryMessages = 10;
    public const int MaxToolIterations = 4;
    public const int GroqTimeoutSeconds = 30;
    public const double Temperature = 0.3;
    public const int MaxTokens = 1024;
    public const string FallbackReply = "Xin lỗi, hiện tại hệ thống AI đang quá tải. Quý khách vui lòng thử lại sau.";
    public const string InvalidToolError = "{\"error\":\"Công cụ không hợp lệ hoặc tham số sai.\"}";
    public const string QueryFailedError = "{\"error\":\"Không truy xuất được dữ liệu.\"}";
}

public static class MyBookingsToolLimits
{
    public const int Default = 5;
    public const int Min = 1;
    public const int Max = 10;
}

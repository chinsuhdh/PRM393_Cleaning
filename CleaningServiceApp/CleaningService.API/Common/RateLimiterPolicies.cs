namespace CleaningService.API.Common;

public static class RateLimiterPolicies
{
    public const string AiChat = "ai-chat";
}

public static class AiChatRateLimiterSettings
{
    public const int PermitLimit = 10;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    public const int QueueLimit = 0;
}

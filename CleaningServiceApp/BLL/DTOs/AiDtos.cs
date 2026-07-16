namespace Cleaning.BLL.DTOs
{
    public class ChatRequestDto
    {
        public string SessionId { get; set; } = null!;
        public string Message { get; set; } = null!;
    }

    public class ChatResponseDto
    {
        public string SessionId { get; set; } = null!;
        public string Reply { get; set; } = null!;
        public int LatencyMs { get; set; }

        /// <summary>False when this is the canned overload/fallback message, not a real model reply.</summary>
        public bool Success { get; set; }
    }

    public class AiChatMessageDto
    {
        public string SenderType { get; set; } = null!;
        public string Message { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
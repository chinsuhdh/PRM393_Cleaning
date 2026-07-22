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

        public bool Success { get; set; }

        public List<ChatSuggestionDto> Suggestions { get; set; } = [];
    }

    public class ChatSuggestionDto
    {
        public string Label { get; set; } = null!;
        public string Route { get; set; } = null!;
    }

    public class AiChatMessageDto
    {
        public string SenderType { get; set; } = null!;
        public string Message { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
namespace Cleaning.BLL.DTOs
{
    public class ChatRequestDto
    {
        public string SessionId { get; set; } = null!;
        public string Message { get; set; } = null!;
    }

    public class ChatResponseDto
    {
        public string Reply { get; set; } = null!;
        public int LatencyMs { get; set; }
    }

    // Class dùng nội bộ để đọc JSON từ Ollama trả về
    public class OllamaResponse
    {
        public string response { get; set; } = string.Empty;
    }
}
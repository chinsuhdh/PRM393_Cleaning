using System.Text.Json.Serialization;

namespace Cleaning.BLL.Features.Ai
{
    public class GroqChatRequest
    {
        public string Model { get; set; } = null!;
        public List<GroqMessage> Messages { get; set; } = [];
        public IReadOnlyList<GroqTool>? Tools { get; set; }
        [JsonPropertyName("tool_choice")]
        public string? ToolChoice { get; set; }
        public double Temperature { get; set; }
        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }
    }

    public class GroqMessage
    {
        public string Role { get; set; } = null!;
        public string? Content { get; set; }
        [JsonPropertyName("tool_calls")]
        public List<GroqToolCall>? ToolCalls { get; set; }
        [JsonPropertyName("tool_call_id")]
        public string? ToolCallId { get; set; }
    }

    public class GroqToolCall
    {
        public string Id { get; set; } = null!;
        public string Type { get; set; } = "function";
        public GroqFunctionCall Function { get; set; } = null!;
    }

    public class GroqFunctionCall
    {
        public string Name { get; set; } = null!;
        public string Arguments { get; set; } = "{}";
    }

    public class GroqTool
    {
        public string Type { get; set; } = "function";
        public GroqFunction Function { get; set; } = null!;
    }

    public class GroqFunction
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public object Parameters { get; set; } = null!;
    }

    public class GroqChatResponse
    {
        public List<GroqChoice> Choices { get; set; } = [];
    }

    public class GroqChoice
    {
        public GroqMessage Message { get; set; } = null!;
        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }
}

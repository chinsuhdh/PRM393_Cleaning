using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Cleaning.BLL.DTOs;
using Cleaning.BLL.Interfaces;
using Cleaning.DAL.Data;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cleaning.BLL.Services
{
    public class AiService : IAiService
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<AiService> _logger;

        public AiService(AppDbContext context, HttpClient httpClient, IConfiguration config, ILogger<AiService> logger)
        {
            _context = context;
            _httpClient = httpClient;
            _config = config;
            _logger = logger;

            var ollamaUrl = _config["AiConfig:OllamaUrl"] ?? "http://localhost:11434";
            _httpClient.BaseAddress = new Uri(ollamaUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<ChatResponseDto> ChatWithRagAsync(Guid userId, ChatRequestDto request)
        {
            var stopwatch = Stopwatch.StartNew();
            var modelName = _config["AiConfig:DefaultModel"] ?? "qwen2.5:1.5b";

            // Fix: Khắc phục trường hợp dữ liệu đầu vào bị null
            var sessionId = request.SessionId ?? Guid.NewGuid().ToString();
            var userMessage = request.Message ?? string.Empty;

            var conversation = await _context.AiConversations
                .FirstOrDefaultAsync(c => c.SessionId == sessionId && c.UserId == userId);

            if (conversation == null)
            {
                conversation = new AiConversation { UserId = userId, SessionId = sessionId, CreatedAt = DateTime.UtcNow };
                _context.AiConversations.Add(conversation);
                await _context.SaveChangesAsync();
            }

            _context.AiMessages.Add(new AiMessage
            {
                ConversationId = conversation.Id,
                SenderType = AiSenderType.User,
                Message = userMessage,
                CreatedAt = DateTime.UtcNow
            });

            var relevantDocs = await _context.KnowledgeDocuments
                .Where(d => d.IsActive)
                .Take(3)
                .Select(d => d.Content)
                .ToListAsync();

            string contextData = relevantDocs.Any() ? string.Join("\n- ", relevantDocs) : "Không có thông tin nội bộ.";

            string prompt = $@"Bạn là trợ lý ảo hỗ trợ khách hàng của ứng dụng dọn dẹp CleanAI. 
Chính sách nội bộ: 
{contextData}

Khách hỏi: {userMessage}
Yêu cầu: Trả lời ngắn gọn, lịch sự, chuyên nghiệp bằng tiếng Việt.";

            var ollamaPayload = new { model = modelName, prompt = prompt, stream = false };
            var content = new StringContent(JsonSerializer.Serialize(ollamaPayload), Encoding.UTF8, "application/json");

            string aiReplyText = "Xin lỗi, hiện tại hệ thống AI đang quá tải. Quý khách vui lòng thử lại sau.";

            try
            {
                var response = await _httpClient.PostAsync("/api/generate", content);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();
                    if (result != null && !string.IsNullOrWhiteSpace(result.response))
                    {
                        aiReplyText = result.response;
                    }
                }
                else
                {
                    _logger.LogWarning("Ollama trả về mã lỗi: {StatusCode}", response.StatusCode);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("Ollama API timeout sau 30s.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kết nối Ollama AI");
            }

            stopwatch.Stop();

            _context.AiMessages.Add(new AiMessage
            {
                ConversationId = conversation.Id,
                SenderType = AiSenderType.Ai,
                Message = aiReplyText,
                CreatedAt = DateTime.UtcNow
            });

            _context.AiInferenceLogs.Add(new AiInferenceLog
            {
                UserId = userId,
                Prompt = prompt,
                Response = aiReplyText,
                LatencyMs = (int)stopwatch.ElapsedMilliseconds,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return new ChatResponseDto { Reply = aiReplyText, LatencyMs = (int)stopwatch.ElapsedMilliseconds };
        }

    }

    public class OllamaResponse
    {
        public string model { get; set; } = string.Empty;
        public string response { get; set; } = string.Empty;
        public bool done { get; set; }
    }
}

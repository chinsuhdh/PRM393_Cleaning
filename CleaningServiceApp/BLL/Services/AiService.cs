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

namespace Cleaning.BLL.Services
{
    public class AiService : IAiService
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public AiService(AppDbContext context, HttpClient httpClient, IConfiguration config)
        {
            _context = context;
            _httpClient = httpClient;
            _config = config;

            // Lấy URL Ollama từ appsettings.json
            var ollamaUrl = _config["AiConfig:OllamaUrl"] ?? "http://localhost:11434";
            _httpClient.BaseAddress = new Uri(ollamaUrl);
        }

        // =================================================================
        // CHỨC NĂNG 1: CHATBOT RAG (Tìm tài liệu -> Nạp Context -> Gọi LLM)
        // =================================================================
        public async Task<ChatResponseDto> ChatWithRagAsync(Guid userId, ChatRequestDto request)
        {
            var stopwatch = Stopwatch.StartNew();
            var modelName = _config["AiConfig:DefaultModel"] ?? "qwen2.5:1.5b";

            // 1. Quản lý Session & Ghi nhận câu hỏi của User
            var conversation = await _context.AiConversations
                .FirstOrDefaultAsync(c => c.SessionId == request.SessionId && c.UserId == userId);

            if (conversation == null)
            {
                conversation = new AiConversation { UserId = userId, SessionId = request.SessionId, CreatedAt = DateTime.UtcNow };
                _context.AiConversations.Add(conversation);
                await _context.SaveChangesAsync();
            }

            _context.AiMessages.Add(new AiMessage
            {
                ConversationId = conversation.Id,
                SenderType = AiSenderType.User,
                Message = request.Message,
                CreatedAt = DateTime.UtcNow
            });

            // 2. RETRIEVAL (Giả lập tìm kiếm tài liệu từ Knowledge Base)
            // Lưu ý: Nếu database dùng VECTOR, sẽ dùng hàm tính Cosine Similarity. 
            // Ở đây dùng text search cơ bản làm mẫu.
            var relevantDocs = await _context.KnowledgeDocuments
                .Take(2) // Lấy top 2 tài liệu liên quan
                .Select(d => d.Content)
                .ToListAsync();

            string contextData = string.Join("\n- ", relevantDocs);

            // 3. AUGMENTED GENERATION (Tạo Prompt)
            string prompt = $@"Bạn là trợ lý ảo của ứng dụng đặt lịch dọn dẹp CleaningApp. 
Dựa vào các chính sách sau đây:
- {contextData}

Hãy trả lời câu hỏi của khách hàng một cách ngắn gọn, lịch sự:
Khách hàng hỏi: {request.Message}";

            // 4. GỌI OLLAMA API
            var ollamaPayload = new { model = modelName, prompt = prompt, stream = false };
            var content = new StringContent(JsonSerializer.Serialize(ollamaPayload), Encoding.UTF8, "application/json");

            string aiReplyText = "Xin lỗi, hiện tại hệ thống AI đang bảo trì.";
            try
            {
                var response = await _httpClient.PostAsync("/api/generate", content);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();
                    if (result != null) aiReplyText = result.response;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OLLAMA ERROR] {ex.Message}");
            }

            stopwatch.Stop();

            // 5. GHI NHẬN CÂU TRẢ LỜI CỦA AI VÀ LOG GIÁM SÁT
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

            return new ChatResponseDto
            {
                Reply = aiReplyText,
                LatencyMs = (int)stopwatch.ElapsedMilliseconds
            };
        }

        // =================================================================
        // CHỨC NĂNG 2: AI MATCHING WORKER (Dùng AI để đánh giá thợ)
        // =================================================================
        public async Task<bool> RecommendWorkerAsync(Guid bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Address)
                .Include(b => b.Service)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return false;

            // Lấy danh sách thợ có kỹ năng làm dịch vụ này (Mẫu lấy top 1 thợ tốt nhất)
            var potentialWorker = await _context.WorkerProfiles
                .Include(w => w.WorkerSkills)
                .Where(w => w.WorkerSkills.Any(ws => ws.ServiceId == booking.ServiceId))
                .OrderByDescending(w => w.AverageRating)
                .FirstOrDefaultAsync();

            if (potentialWorker == null) return false;

            // Xây dựng Prompt cho AI tự chấm điểm dựa trên Profile
            string prompt = $@"Phân tích mức độ phù hợp của thợ này cho đơn đặt lịch:
- Đơn hàng: Cần {booking.Service.Name}, thời gian {booking.DurationHours} giờ. Tọa độ khách: {booking.Address?.Latitude},{booking.Address?.Longitude}
- Thợ: Điểm đánh giá {potentialWorker.AverageRating}/5.0, Đã làm {potentialWorker.CompletedJobs} công việc. Tọa độ thợ: {potentialWorker.CurrentLat},{potentialWorker.CurrentLng}

Đánh giá và cho điểm từ 0.000 đến 1.000 (Chỉ trả về 1 con số và 1 câu giải thích ngắn).";

            var modelName = _config["AiConfig:DefaultModel"] ?? "qwen2.5:1.5b";
            var ollamaPayload = new { model = modelName, prompt = prompt, stream = false };
            var content = new StringContent(JsonSerializer.Serialize(ollamaPayload), Encoding.UTF8, "application/json");

            string reason = "Được đề xuất tự động dựa trên đánh giá sao cao nhất.";
            decimal score = 0.8500m; // Default score

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var response = await _httpClient.PostAsync("/api/generate", content);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();
                    if (result != null) reason = "AI Nhận định: " + result.response;
                    // Logic Regex bóc tách số điểm (score) từ text của AI có thể thêm tại đây
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OLLAMA MATCHING ERROR] {ex.Message}");
            }
            stopwatch.Stop();

            // Lưu kết quả vào bảng ai_recommendations
            _context.AiRecommendations.Add(new AiRecommendation
            {
                BookingId = booking.Id,
                WorkerId = potentialWorker.UserId,
                Score = score,
                Reason = reason,
                CreatedAt = DateTime.UtcNow
            });

            // Monitor log cho Matching
            _context.AiInferenceLogs.Add(new AiInferenceLog
            {
                Prompt = prompt,
                Response = reason,
                LatencyMs = (int)stopwatch.ElapsedMilliseconds,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
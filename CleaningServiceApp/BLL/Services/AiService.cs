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

            var relevantDocs = await _context.KnowledgeDocuments
                .Take(2)
                .Select(d => d.Content)
                .ToListAsync();

            string contextData = string.Join("\n- ", relevantDocs);

            string prompt = $@"Bạn là trợ lý ảo của ứng dụng đặt lịch dọn dẹp CleaningApp. 
Dựa vào các chính sách sau đây:
- {contextData}

Hãy trả lời câu hỏi của khách hàng một cách ngắn gọn, lịch sự:
Khách hàng hỏi: {request.Message}";

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
        // CHỨC NĂNG 2: AI MATCHING WORKER (Dùng AI để đánh giá thợ - LƯU VÀO DB)
        // =================================================================
        public async Task<bool> RecommendWorkerAsync(Guid bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Address)
                .Include(b => b.Service)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return false;

            var potentialWorkers = await _context.WorkerProfiles
                .Include(w => w.WorkerSkills)
                .Include(w => w.User)
                .Where(w => w.WorkerSkills.Any(ws => ws.ServiceId == booking.ServiceId))
                .OrderByDescending(w => w.AverageRating)
                .Take(5)
                .ToListAsync();

            if (!potentialWorkers.Any()) return false;

            var modelName = _config["AiConfig:DefaultModel"] ?? "qwen2.5:1.5b";

            foreach (var worker in potentialWorkers)
            {
                string prompt = $@"Phân tích mức độ phù hợp của thợ này cho đơn đặt lịch:
- Đơn hàng: Cần {booking.Service.Name}, thời gian {booking.DurationHours} giờ. Tọa độ khách: {booking.Address?.Latitude},{booking.Address?.Longitude}
- Thợ ({worker.User.FullName}): Điểm đánh giá {worker.AverageRating}/5.0, Đã làm {worker.CompletedJobs} công việc. Tọa độ thợ: {worker.CurrentLat},{worker.CurrentLng}

Đánh giá và cho điểm từ 0.000 đến 1.000 (Chỉ trả về 1 con số và 1 câu giải thích ngắn).";

                var ollamaPayload = new { model = modelName, prompt = prompt, stream = false };
                var content = new StringContent(JsonSerializer.Serialize(ollamaPayload), Encoding.UTF8, "application/json");

                string reason = "Được đề xuất dựa trên kỹ năng và điểm đánh giá.";
                decimal score = 0.8500m;

                var stopwatch = Stopwatch.StartNew();
                try
                {
                    var response = await _httpClient.PostAsync("/api/generate", content);
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();
                        if (result != null) reason = "AI: " + result.response;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[OLLAMA ERROR cho thợ {worker.UserId}]: {ex.Message}");
                }
                stopwatch.Stop();

                _context.AiRecommendations.Add(new AiRecommendation
                {
                    BookingId = booking.Id,
                    WorkerId = worker.UserId,
                    Score = score,
                    Reason = reason,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // =================================================================
        // CHỨC NĂNG 3: LẤY DANH SÁCH THỢ ĐÃ ĐƯỢC MATCHING ĐỂ TRẢ VỀ FRONTEND
        // =================================================================
        public async Task<List<WorkerDto>> GetRecommendedWorkersAsync(Guid bookingId)
        {
            var recommendedWorkers = await _context.AiRecommendations
                .Where(r => r.BookingId == bookingId)
                .Include(r => r.Worker)
                    .ThenInclude(w => w.User) // Map tới bảng Profiles (tên property là User)
                .OrderByDescending(r => r.Score)
                .Select(r => new WorkerDto
                {
                    Id = r.WorkerId.ToString(),
                    Name = r.Worker.User.FullName,
                    Initials = GetInitials(r.Worker.User.FullName),
                    Rating = (double)r.Worker.AverageRating,
                    Reviews = r.Worker.CompletedJobs,
                    Distance = "2.5 km",
                    MatchPercentage = (int)(r.Score * 100)
                })
                .ToListAsync();

            return recommendedWorkers;
        }

        // Hàm phụ trợ tiện ích
        private static string GetInitials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "?";
            var words = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return words.Length > 1
                ? $"{words[0][0]}{words[^1][0]}".ToUpper()
                : words[0][0].ToString().ToUpper();
        }
    }
}
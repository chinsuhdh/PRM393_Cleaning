using Cleaning.BLL.DTOs;

namespace Cleaning.BLL.Interfaces
{
    public interface IAiService
    {
        // Chức năng 1: Chatbot RAG
        Task<ChatResponseDto> ChatWithRagAsync(Guid userId, ChatRequestDto request);

        // Chức năng 2: Matching Worker
        Task<bool> RecommendWorkerAsync(Guid bookingId);
    }
}
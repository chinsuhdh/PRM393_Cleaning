using Cleaning.BLL.DTOs;

namespace Cleaning.BLL.Interfaces
{
    public interface IAiService
    {
        // Chatbot RAG (AI = chatbot only; worker matching is broadcast dispatch, not AI)
        Task<ChatResponseDto> ChatWithRagAsync(Guid userId, ChatRequestDto request);
    }
}
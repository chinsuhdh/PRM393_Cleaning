using Cleaning.BLL.DTOs;

namespace Cleaning.BLL.Interfaces
{
    public interface IAiService
    {
        Task<ChatResponseDto> ChatWithRagAsync(Guid userId, ChatRequestDto request);
        Task<IReadOnlyList<AiChatMessageDto>> GetHistoryAsync(Guid userId, string sessionId);
        Task ClearHistoryAsync(Guid userId, string sessionId);
    }
}
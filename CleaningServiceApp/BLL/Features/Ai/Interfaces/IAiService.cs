
namespace Cleaning.BLL.Features.Ai
{
    public interface IAiService
    {
        Task<ChatResponseDto> ChatWithRagAsync(Guid userId, ChatRequestDto request);
        Task<IReadOnlyList<AiChatMessageDto>> GetHistoryAsync(Guid userId, string sessionId);
        Task ClearHistoryAsync(Guid userId, string sessionId);
    }
}
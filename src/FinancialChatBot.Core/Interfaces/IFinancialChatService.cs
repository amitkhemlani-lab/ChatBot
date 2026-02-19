using FinancialChatBot.Core.Models;

namespace FinancialChatBot.Core.Interfaces;

public interface IFinancialChatService
{
    Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken = default);
    Task<ChatSession> CreateSessionAsync(string userId, string? title = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatSession>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatMessageDto>> GetSessionHistoryAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

public record ChatSession(Guid Id, string UserId, string Title, DateTime CreatedAt, DateTime LastActivityAt);
public record ChatMessageDto(Guid Id, string Role, string Content, DateTime CreatedAt, string? SqlQueryExecuted);

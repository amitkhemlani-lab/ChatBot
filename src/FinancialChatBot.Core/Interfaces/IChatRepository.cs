using FinancialChatBot.Core.Entities;

namespace FinancialChatBot.Core.Interfaces;

public interface IChatRepository
{
    Task<ChatSession> CreateSessionAsync(ChatSession session, CancellationToken cancellationToken = default);
    Task<ChatSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatSession>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default);
    Task UpdateSessionAsync(ChatSession session, CancellationToken cancellationToken = default);
    Task<ChatMessage> AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatMessage>> GetSessionMessagesAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

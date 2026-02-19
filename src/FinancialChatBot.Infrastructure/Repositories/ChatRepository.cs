using FinancialChatBot.Core.Entities;
using FinancialChatBot.Core.Interfaces;
using FinancialChatBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinancialChatBot.Infrastructure.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly FinancialDbContext _context;

    public ChatRepository(FinancialDbContext context)
    {
        _context = context;
    }

    public async Task<ChatSession> CreateSessionAsync(ChatSession session, CancellationToken cancellationToken = default)
    {
        _context.ChatSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<ChatSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.ChatSessions
            .AsNoTracking()
            .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
    }

    public async Task<IEnumerable<ChatSession>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.ChatSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateSessionAsync(ChatSession session, CancellationToken cancellationToken = default)
    {
        _context.ChatSessions.Update(session);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ChatMessage> AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task<IEnumerable<ChatMessage>> GetSessionMessagesAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.ChatMessages
            .AsNoTracking()
            .Where(m => m.ChatSessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}

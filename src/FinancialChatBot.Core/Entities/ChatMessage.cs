namespace FinancialChatBot.Core.Entities;

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChatSessionId { get; set; }
    public string Role { get; set; } = string.Empty; // user, assistant, system
    public string Content { get; set; } = string.Empty;
    public string? FoundryMessageId { get; set; }
    public int? TokensUsed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? SqlQueryExecuted { get; set; }
    public string? DataContext { get; set; }

    public ChatSession Session { get; set; } = null!;
}

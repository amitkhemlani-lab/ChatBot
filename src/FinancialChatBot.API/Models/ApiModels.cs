namespace FinancialChatBot.API.Models;

public class ChatMessageRequest
{
    /// <summary>Existing session ID. Omit to start a new session.</summary>
    public Guid? SessionId { get; set; }

    /// <summary>User identifier (e.g., email or user ID).</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>The user's natural language question or command.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Optional context to scope the query to specific accounts or companies.</summary>
    public ChatContextRequest? Context { get; set; }
}

public class ChatContextRequest
{
    /// <summary>Filter to a specific bank account number.</summary>
    public string? AccountNumber { get; set; }

    /// <summary>Filter to a specific company code for capital structure queries.</summary>
    public string? CompanyCode { get; set; }

    /// <summary>Start date for date-range filtering.</summary>
    public DateTime? DateFrom { get; set; }

    /// <summary>End date for date-range filtering.</summary>
    public DateTime? DateTo { get; set; }

    /// <summary>Analysis focus: BankStatement, CapitalStructure, or Both.</summary>
    public string? AnalysisType { get; set; }
}

public class CreateSessionRequest
{
    public string UserId { get; set; } = string.Empty;
    public string? Title { get; set; }
}

public record ChatSessionResponse(
    Guid Id,
    string UserId,
    string Title,
    DateTime CreatedAt,
    DateTime LastActivityAt
);

public record ChatMessageHistoryResponse(
    Guid Id,
    string Role,
    string Content,
    DateTime CreatedAt,
    string? SqlQueryExecuted
);

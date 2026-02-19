namespace FinancialChatBot.Core.Models;

public class ChatRequest
{
    public Guid? SessionId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ChatContext? Context { get; set; }
}

public class ChatContext
{
    public string? AccountNumber { get; set; }
    public string? CompanyCode { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? AnalysisType { get; set; } // BankStatement, CapitalStructure, Both
}

public class ChatResponse
{
    public Guid SessionId { get; set; }
    public Guid MessageId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? SqlQueryExecuted { get; set; }
    public List<DataInsight> Insights { get; set; } = new();
    public int TokensUsed { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class DataInsight
{
    public string Type { get; set; } = string.Empty; // Chart, Table, Summary, Alert
    public string Title { get; set; } = string.Empty;
    public object? Data { get; set; }
}

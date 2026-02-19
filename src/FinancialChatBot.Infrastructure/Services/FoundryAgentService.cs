using Azure.AI.Projects;
using Azure.Identity;
using FinancialChatBot.Core.Entities;
using FinancialChatBot.Core.Interfaces;
using FinancialChatBot.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace FinancialChatBot.Infrastructure.Services;

/// <summary>
/// Integrates with Azure AI Foundry Agents API to provide intelligent
/// financial data analysis via natural language conversations.
/// </summary>
public class FoundryAgentService : IFinancialChatService
{
    private readonly AIProjectClient _projectClient;
    private readonly IChatRepository _chatRepository;
    private readonly IBankStatementRepository _bankRepo;
    private readonly ICapitalStructureRepository _capitalRepo;
    private readonly ILogger<FoundryAgentService> _logger;
    private readonly IConfiguration _config;
    private readonly string _agentId;

    private static readonly string SystemPrompt = """
        You are a sophisticated financial analyst assistant with deep expertise in banking and capital markets.
        You have access to a financial database containing:

        1. BANK STATEMENTS DATA:
           - BankAccounts: AccountNumber, AccountHolderName, AccountType, Currency, Balance, OpenedDate
           - BankStatements: StatementPeriod (YYYY-MM), PeriodStartDate, PeriodEndDate, OpeningBalance, ClosingBalance, TotalCredits, TotalDebits, TransactionCount
           - Transactions: TransactionDate, Description, TransactionType (Credit/Debit), Category, Amount, RunningBalance, Channel, CounterpartyName

        2. CAPITAL STRUCTURE DATA:
           - CapitalStructures: CompanyName, CompanyCode, FiscalYear, Quarter, TotalEquity, TotalDebt, TotalCapital, DebtToEquityRatio, WACC, MarketCapitalization, EnterpriseValue
           - Components: CommonStock, PreferredStock, RetainedEarnings, LongTermDebt, ShortTermDebt, Bonds, Debentures

        Your capabilities:
        - Analyze bank statement transactions (spending patterns, cash flow, category breakdowns)
        - Assess capital structure health (leverage ratios, WACC, debt composition)
        - Compare companies by capital efficiency, leverage, and financing costs
        - Identify financial trends, anomalies, and risks
        - Generate SQL queries to answer specific data questions
        - Provide actionable financial insights and recommendations

        When generating SQL queries for data retrieval:
        - Use SELECT-only queries (never INSERT, UPDATE, DELETE, DROP, ALTER)
        - Reference table names exactly: BankAccounts, BankStatements, Transactions, CapitalStructures
        - Format monetary values clearly with currency symbols
        - Always contextualize numbers with industry benchmarks where relevant

        Respond in a clear, professional manner suitable for financial professionals.
        When presenting data, use structured formats with clear labels and units.
        """;

    public FoundryAgentService(
        IConfiguration config,
        IChatRepository chatRepository,
        IBankStatementRepository bankRepo,
        ICapitalStructureRepository capitalRepo,
        ILogger<FoundryAgentService> logger)
    {
        _config = config;
        _chatRepository = chatRepository;
        _bankRepo = bankRepo;
        _capitalRepo = capitalRepo;
        _logger = logger;

        var connectionString = config["AzureAIFoundry:ConnectionString"]
            ?? throw new InvalidOperationException("AzureAIFoundry:ConnectionString is required.");
        _agentId = config["AzureAIFoundry:AgentId"] ?? string.Empty;

        _projectClient = new AIProjectClient(
            connectionString,
            new DefaultAzureCredential());
    }

    public async Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        // Resolve or create session
        Core.Entities.ChatSession? session = null;
        if (request.SessionId.HasValue)
        {
            session = await _chatRepository.GetSessionAsync(request.SessionId.Value, cancellationToken);
        }

        if (session == null)
        {
            var title = TruncateTitle(request.Message, 80);
            session = await _chatRepository.CreateSessionAsync(new Core.Entities.ChatSession
            {
                UserId = request.UserId,
                Title = title
            }, cancellationToken);
        }

        // Save the user message
        await _chatRepository.AddMessageAsync(new ChatMessage
        {
            ChatSessionId = session.Id,
            Role = "user",
            Content = request.Message
        }, cancellationToken);

        // Enrich with relevant financial context
        var contextData = await GatherFinancialContextAsync(request, cancellationToken);
        var enrichedMessage = BuildEnrichedMessage(request.Message, contextData, request.Context);

        // Interact with Azure AI Foundry
        string assistantReply;
        string? sqlExecuted = null;
        int tokensUsed = 0;

        try
        {
            (assistantReply, sqlExecuted, tokensUsed) = await CallFoundryAgentAsync(
                session, enrichedMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Foundry agent call failed for session {SessionId}", session.Id);
            assistantReply = "I encountered an error while processing your financial query. Please try again or rephrase your question.";
        }

        // Persist assistant reply
        var assistantMessage = await _chatRepository.AddMessageAsync(new ChatMessage
        {
            ChatSessionId = session.Id,
            Role = "assistant",
            Content = assistantReply,
            TokensUsed = tokensUsed,
            SqlQueryExecuted = sqlExecuted,
            DataContext = contextData
        }, cancellationToken);

        // Update session activity timestamp
        session.LastActivityAt = DateTime.UtcNow;
        await _chatRepository.UpdateSessionAsync(session, cancellationToken);

        return new ChatResponse
        {
            SessionId = session.Id,
            MessageId = assistantMessage.Id,
            Content = assistantReply,
            SqlQueryExecuted = sqlExecuted,
            TokensUsed = tokensUsed,
            Insights = ExtractInsights(assistantReply)
        };
    }

    public async Task<Core.Interfaces.ChatSession> CreateSessionAsync(
        string userId, string? title = null, CancellationToken cancellationToken = default)
    {
        var session = await _chatRepository.CreateSessionAsync(new Core.Entities.ChatSession
        {
            UserId = userId,
            Title = title ?? "New Financial Analysis"
        }, cancellationToken);

        return new Core.Interfaces.ChatSession(session.Id, session.UserId, session.Title, session.CreatedAt, session.LastActivityAt);
    }

    public async Task<IEnumerable<Core.Interfaces.ChatSession>> GetUserSessionsAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var sessions = await _chatRepository.GetUserSessionsAsync(userId, cancellationToken);
        return sessions.Select(s => new Core.Interfaces.ChatSession(s.Id, s.UserId, s.Title, s.CreatedAt, s.LastActivityAt));
    }

    public async Task<IEnumerable<Core.Interfaces.ChatMessageDto>> GetSessionHistoryAsync(
        Guid sessionId, CancellationToken cancellationToken = default)
    {
        var messages = await _chatRepository.GetSessionMessagesAsync(sessionId, cancellationToken);
        return messages.Select(m => new Core.Interfaces.ChatMessageDto(m.Id, m.Role, m.Content, m.CreatedAt, m.SqlQueryExecuted));
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task<(string reply, string? sqlQuery, int tokens)> CallFoundryAgentAsync(
        Core.Entities.ChatSession session, string message, CancellationToken cancellationToken)
    {
        var agentsClient = _projectClient.GetAgentsClient();

        // Create or reuse Foundry thread
        AgentThread thread;
        if (!string.IsNullOrEmpty(session.FoundryThreadId))
        {
            thread = await agentsClient.GetThreadAsync(session.FoundryThreadId, cancellationToken);
        }
        else
        {
            thread = await agentsClient.CreateThreadAsync(cancellationToken: cancellationToken);
            session.FoundryThreadId = thread.Id;
        }

        // Add user message to thread
        await agentsClient.CreateMessageAsync(thread.Id, MessageRole.User, message, cancellationToken: cancellationToken);

        // Determine which agent to use
        Agent agent;
        if (!string.IsNullOrEmpty(_agentId))
        {
            agent = await agentsClient.GetAgentAsync(_agentId, cancellationToken);
        }
        else
        {
            agent = await agentsClient.CreateAgentAsync(
                model: _config["AzureAIFoundry:ModelDeploymentName"] ?? "gpt-4o",
                name: "FinancialAnalystBot",
                instructions: SystemPrompt,
                cancellationToken: cancellationToken);
        }

        // Run the agent
        var run = await agentsClient.CreateRunAsync(thread.Id, agent.Id, cancellationToken: cancellationToken);

        // Poll until complete
        int maxWaitMs = 60_000;
        int elapsed = 0;
        int pollInterval = 1_000;

        while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress)
        {
            if (elapsed >= maxWaitMs)
                throw new TimeoutException("Foundry agent run timed out.");

            await Task.Delay(pollInterval, cancellationToken);
            elapsed += pollInterval;
            run = await agentsClient.GetRunAsync(thread.Id, run.Id, cancellationToken);
        }

        if (run.Status == RunStatus.Failed)
            throw new InvalidOperationException($"Foundry run failed: {run.LastError?.Message}");

        // Retrieve the latest assistant messages
        var messagesPage = agentsClient.GetMessagesAsync(thread.Id, cancellationToken: cancellationToken);
        string replyContent = string.Empty;
        await foreach (var msg in messagesPage)
        {
            if (msg.Role == MessageRole.Agent)
            {
                replyContent = string.Join("\n", msg.ContentItems
                    .OfType<MessageTextContent>()
                    .Select(c => c.Text));
                break;
            }
        }

        int tokens = run.Usage?.TotalTokens ?? 0;
        return (replyContent, null, tokens);
    }

    private async Task<string?> GatherFinancialContextAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        if (request.Context == null) return null;

        var sb = new StringBuilder();
        sb.AppendLine("=== FINANCIAL DATA CONTEXT ===");

        // Bank account context
        if (!string.IsNullOrEmpty(request.Context.AccountNumber))
        {
            var account = await _bankRepo.GetAccountByNumberAsync(request.Context.AccountNumber, cancellationToken);
            if (account != null)
            {
                sb.AppendLine($"Account: {account.AccountNumber} | {account.AccountHolderName} | {account.AccountType}");
                sb.AppendLine($"Current Balance: {account.Currency} {account.Balance:N2}");

                if (request.Context.DateFrom.HasValue && request.Context.DateTo.HasValue)
                {
                    var credits = await _bankRepo.GetTotalCreditsAsync(account.Id, request.Context.DateFrom.Value, request.Context.DateTo.Value, cancellationToken);
                    var debits = await _bankRepo.GetTotalDebitsAsync(account.Id, request.Context.DateFrom.Value, request.Context.DateTo.Value, cancellationToken);
                    var categories = await _bankRepo.GetSpendingByCategoryAsync(account.Id, request.Context.DateFrom.Value, request.Context.DateTo.Value, cancellationToken);

                    sb.AppendLine($"Period: {request.Context.DateFrom:yyyy-MM-dd} to {request.Context.DateTo:yyyy-MM-dd}");
                    sb.AppendLine($"Total Credits: {account.Currency} {credits:N2}");
                    sb.AppendLine($"Total Debits: {account.Currency} {debits:N2}");
                    sb.AppendLine("Spending by Category:");
                    foreach (var (cat, amt, cnt) in categories)
                        sb.AppendLine($"  - {cat}: {account.Currency} {amt:N2} ({cnt} transactions)");
                }
            }
        }

        // Capital structure context
        if (!string.IsNullOrEmpty(request.Context.CompanyCode))
        {
            var capital = await _capitalRepo.GetLatestByCompanyAsync(request.Context.CompanyCode, cancellationToken);
            if (capital != null)
            {
                sb.AppendLine($"\nCompany: {capital.CompanyName} ({capital.CompanyCode})");
                sb.AppendLine($"Period: FY{capital.FiscalYear} {capital.Quarter}");
                sb.AppendLine($"Total Equity: ${capital.TotalEquity:N2}M | Total Debt: ${capital.TotalDebt:N2}M");
                sb.AppendLine($"D/E Ratio: {capital.DebtToEquityRatio:F2} | WACC: {capital.WeightedAverageCostOfCapital:P2}");
                sb.AppendLine($"Market Cap: ${capital.MarketCapitalization:N2}M | EV: ${capital.EnterpriseValue:N2}M");
            }
        }

        return sb.Length > 50 ? sb.ToString() : null;
    }

    private static string BuildEnrichedMessage(string userMessage, string? contextData, ChatContext? context)
    {
        if (string.IsNullOrEmpty(contextData)) return userMessage;

        return $"""
            {contextData}

            === USER QUESTION ===
            {userMessage}
            """;
    }

    private static List<DataInsight> ExtractInsights(string reply)
    {
        // Simple heuristic: if response contains key financial terms, tag as insight
        var insights = new List<DataInsight>();

        if (reply.Contains("ratio", StringComparison.OrdinalIgnoreCase) ||
            reply.Contains("trend", StringComparison.OrdinalIgnoreCase))
        {
            insights.Add(new DataInsight { Type = "Summary", Title = "Financial Analysis", Data = reply });
        }

        return insights;
    }

    private static string TruncateTitle(string message, int maxLength)
    {
        return message.Length <= maxLength ? message : message[..maxLength] + "...";
    }
}

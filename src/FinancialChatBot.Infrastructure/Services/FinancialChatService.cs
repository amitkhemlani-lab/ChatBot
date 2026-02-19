using Azure.Identity;
using FinancialChatBot.Core.Entities;
using FinancialChatBot.Core.Interfaces;
using FinancialChatBot.Core.Models;
using FinancialChatBot.Infrastructure.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace FinancialChatBot.Infrastructure.Services;

/// <summary>
/// Financial chatbot service powered by Microsoft Semantic Kernel.
///
/// How it works:
///   1. User sends a natural language question.
///   2. SK sends it to the GPT-4o deployment on Azure AI Foundry.
///   3. The model decides whether to call a plugin function (e.g. get_transactions,
///      get_capital_structure) and passes back the arguments.
///   4. SK automatically invokes the matching KernelFunction, which queries Azure SQL.
///   5. The function result (JSON) is fed back to the model.
///   6. The model generates a final, grounded, human-readable response.
///   Steps 3-5 repeat (auto-iterate) until the model is satisfied.
/// </summary>
public sealed class FinancialChatService : IFinancialChatService
{
    private readonly Kernel _kernel;
    private readonly IChatRepository _chatRepository;
    private readonly ILogger<FinancialChatService> _logger;

    private const string SystemPrompt = """
        You are a sophisticated financial analyst assistant with expertise in banking and capital markets.

        You have access to tools that query a live financial database containing:
        - Bank accounts, monthly statements, and individual transactions
        - Capital structure records (equity, debt, ratios, WACC, market data) for multiple companies

        Guidelines:
        - ALWAYS use the available tools to retrieve live data before answering data-specific questions.
        - Never guess or fabricate numbers. If a tool returns no data, say so clearly.
        - When presenting financial data, format monetary values with currency symbols and commas.
        - Express ratios to 2 decimal places, percentages to 1 decimal place.
        - Provide concise interpretation alongside the raw numbers (e.g., "a D/E ratio of 0.71 indicates moderate leverage").
        - For trend questions, call get_capital_structure_trend rather than making multiple single-year calls.
        - For comparisons, use compare_companies to fetch both companies in one call.
        - Highlight notable findings, risks, or recommendations where appropriate.
        - Keep responses professional and suitable for financial analysts and executives.
        """;

    public FinancialChatService(
        IConfiguration config,
        IChatRepository chatRepository,
        BankStatementPlugin bankPlugin,
        CapitalStructurePlugin capitalPlugin,
        ILogger<FinancialChatService> logger)
    {
        _chatRepository = chatRepository;
        _logger = logger;

        var endpoint = config["AzureAIFoundry:Endpoint"]
            ?? throw new InvalidOperationException("AzureAIFoundry:Endpoint is required.");
        var deployment = config["AzureAIFoundry:ModelDeploymentName"] ?? "gpt-4o";

        // Build the Semantic Kernel with Azure OpenAI (Azure AI Foundry exposes an Azure OpenAI-compatible endpoint)
        var kernelBuilder = Kernel.CreateBuilder();

        kernelBuilder.AddAzureOpenAIChatCompletion(
            deploymentName: deployment,
            endpoint: endpoint,
            credentials: new DefaultAzureCredential());

        // Register plugins — SK discovers all [KernelFunction] methods automatically
        kernelBuilder.Plugins.AddFromObject(bankPlugin, pluginName: "BankStatements");
        kernelBuilder.Plugins.AddFromObject(capitalPlugin, pluginName: "CapitalStructure");

        _kernel = kernelBuilder.Build();
    }

    public async Task<ChatResponse> SendMessageAsync(
        ChatRequest request, CancellationToken cancellationToken = default)
    {
        // Resolve or create the session
        ChatSession? session = null;
        if (request.SessionId.HasValue)
            session = await _chatRepository.GetSessionAsync(request.SessionId.Value, cancellationToken);

        if (session is null)
        {
            session = await _chatRepository.CreateSessionAsync(new ChatSession
            {
                UserId = request.UserId,
                Title = Truncate(request.Message, 80)
            }, cancellationToken);
        }

        // Persist user message
        await _chatRepository.AddMessageAsync(new ChatMessage
        {
            ChatSessionId = session.Id,
            Role = "user",
            Content = request.Message
        }, cancellationToken);

        // Rebuild SK ChatHistory from persisted messages so the model has full context
        var history = await BuildChatHistoryAsync(session.Id, cancellationToken);

        // Add the new user message to the live history
        history.AddUserMessage(request.Message);

        // Configure auto function calling — SK will loop automatically until the model stops invoking tools
        var executionSettings = new AzureOpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            Temperature = 0.2,          // Low temperature for factual financial data
            MaxTokens = 2048
        };

        _logger.LogInformation("Invoking SK chat for session {SessionId}, user: {UserId}", session.Id, request.UserId);

        var chatService = _kernel.GetRequiredService<IChatCompletionService>();
        var result = await chatService.GetChatMessageContentAsync(
            history,
            executionSettings,
            _kernel,
            cancellationToken);

        var assistantContent = result.Content ?? string.Empty;
        var tokensUsed = result.Metadata?.TryGetValue("Usage", out var usage) == true
            ? ExtractTotalTokens(usage)
            : 0;

        // Collect the names of any plugin functions that were called this turn
        // (tracked via the kernel's FunctionInvocationContext in full SK telemetry;
        // here we do a lightweight check via InnerContent)
        var functionsInvoked = ExtractFunctionNames(result);

        // Persist assistant reply
        var assistantMsg = await _chatRepository.AddMessageAsync(new ChatMessage
        {
            ChatSessionId = session.Id,
            Role = "assistant",
            Content = assistantContent,
            TokensUsed = tokensUsed,
            DataContext = functionsInvoked.Any()
                ? string.Join(", ", functionsInvoked)
                : null
        }, cancellationToken);

        // Bump last-activity timestamp
        session.LastActivityAt = DateTime.UtcNow;
        await _chatRepository.UpdateSessionAsync(session, cancellationToken);

        return new ChatResponse
        {
            SessionId = session.Id,
            MessageId = assistantMsg.Id,
            Content = assistantContent,
            TokensUsed = tokensUsed
        };
    }

    public async Task<Core.Interfaces.ChatSession> CreateSessionAsync(
        string userId, string? title = null, CancellationToken cancellationToken = default)
    {
        var session = await _chatRepository.CreateSessionAsync(new ChatSession
        {
            UserId = userId,
            Title = title ?? "New Financial Analysis"
        }, cancellationToken);

        return Map(session);
    }

    public async Task<IEnumerable<Core.Interfaces.ChatSession>> GetUserSessionsAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var sessions = await _chatRepository.GetUserSessionsAsync(userId, cancellationToken);
        return sessions.Select(Map);
    }

    public async Task<IEnumerable<Core.Interfaces.ChatMessageDto>> GetSessionHistoryAsync(
        Guid sessionId, CancellationToken cancellationToken = default)
    {
        var messages = await _chatRepository.GetSessionMessagesAsync(sessionId, cancellationToken);
        return messages.Select(m => new Core.Interfaces.ChatMessageDto(
            m.Id, m.Role, m.Content, m.CreatedAt, m.SqlQueryExecuted));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reconstruct a <see cref="ChatHistory"/> from persisted messages so the
    /// model retains full conversation context across API calls.
    /// </summary>
    private async Task<ChatHistory> BuildChatHistoryAsync(
        Guid sessionId, CancellationToken cancellationToken)
    {
        var history = new ChatHistory(SystemPrompt);

        var messages = await _chatRepository.GetSessionMessagesAsync(sessionId, cancellationToken);
        foreach (var msg in messages)
        {
            switch (msg.Role)
            {
                case "user":      history.AddUserMessage(msg.Content);      break;
                case "assistant": history.AddAssistantMessage(msg.Content); break;
            }
        }

        return history;
    }

    private static Core.Interfaces.ChatSession Map(ChatSession s) =>
        new(s.Id, s.UserId, s.Title, s.CreatedAt, s.LastActivityAt);

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";

    private static int ExtractTotalTokens(object? usage)
    {
        // SK wraps usage in a CompletionsUsage object; extract via reflection-safe ToString
        if (usage is null) return 0;
        var text = usage.ToString() ?? string.Empty;
        // Heuristic: look for "TotalTokens" in the string representation
        var match = System.Text.RegularExpressions.Regex.Match(text, @"TotalTokens[^\d]*(\d+)");
        return match.Success && int.TryParse(match.Groups[1].Value, out var t) ? t : 0;
    }

    private static List<string> ExtractFunctionNames(ChatMessageContent result)
    {
        var names = new List<string>();
        if (result.Metadata?.TryGetValue("FunctionInvocations", out var invocations) == true
            && invocations is IEnumerable<object> list)
        {
            names.AddRange(list.Select(i => i.ToString() ?? string.Empty));
        }
        return names;
    }
}

using FinancialChatBot.API.Models;
using FinancialChatBot.Core.Interfaces;
using FinancialChatBot.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace FinancialChatBot.API.Controllers;

/// <summary>
/// Financial AI Chatbot - conversational interface powered by Azure AI Foundry.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IFinancialChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IFinancialChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Send a message to the financial AI assistant.
    /// </summary>
    [HttpPost("message")]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ChatResponse>> SendMessage(
        [FromBody] ChatMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Message cannot be empty." });

        if (string.IsNullOrWhiteSpace(request.UserId))
            return BadRequest(new { error = "UserId is required." });

        _logger.LogInformation("Chat message from user {UserId}, session {SessionId}", request.UserId, request.SessionId);

        var chatRequest = new ChatRequest
        {
            SessionId = request.SessionId,
            UserId = request.UserId,
            Message = request.Message,
            Context = request.Context == null ? null : new ChatContext
            {
                AccountNumber = request.Context.AccountNumber,
                CompanyCode = request.Context.CompanyCode,
                DateFrom = request.Context.DateFrom,
                DateTo = request.Context.DateTo,
                AnalysisType = request.Context.AnalysisType
            }
        };

        var response = await _chatService.SendMessageAsync(chatRequest, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Create a new chat session.
    /// </summary>
    [HttpPost("sessions")]
    [ProducesResponseType(typeof(ChatSessionResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<ChatSessionResponse>> CreateSession(
        [FromBody] CreateSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
            return BadRequest(new { error = "UserId is required." });

        var session = await _chatService.CreateSessionAsync(request.UserId, request.Title, cancellationToken);
        var response = new ChatSessionResponse(session.Id, session.UserId, session.Title, session.CreatedAt, session.LastActivityAt);
        return CreatedAtAction(nameof(GetSession), new { sessionId = session.Id }, response);
    }

    /// <summary>
    /// Get all chat sessions for a user.
    /// </summary>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(IEnumerable<ChatSessionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ChatSessionResponse>>> GetUserSessions(
        [FromQuery] string userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest(new { error = "userId query parameter is required." });

        var sessions = await _chatService.GetUserSessionsAsync(userId, cancellationToken);
        var response = sessions.Select(s => new ChatSessionResponse(s.Id, s.UserId, s.Title, s.CreatedAt, s.LastActivityAt));
        return Ok(response);
    }

    /// <summary>
    /// Get a specific session by ID (returns metadata only).
    /// </summary>
    [HttpGet("sessions/{sessionId:guid}")]
    [ProducesResponseType(typeof(ChatSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatSessionResponse>> GetSession(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var sessions = await _chatService.GetUserSessionsAsync(string.Empty, cancellationToken);
        var session = sessions.FirstOrDefault(s => s.Id == sessionId);

        if (session == null) return NotFound();

        return Ok(new ChatSessionResponse(session.Id, session.UserId, session.Title, session.CreatedAt, session.LastActivityAt));
    }

    /// <summary>
    /// Get full message history for a session.
    /// </summary>
    [HttpGet("sessions/{sessionId:guid}/history")]
    [ProducesResponseType(typeof(IEnumerable<ChatMessageHistoryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ChatMessageHistoryResponse>>> GetSessionHistory(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var messages = await _chatService.GetSessionHistoryAsync(sessionId, cancellationToken);
        var response = messages.Select(m => new ChatMessageHistoryResponse(
            m.Id, m.Role, m.Content, m.CreatedAt, m.SqlQueryExecuted));
        return Ok(response);
    }
}

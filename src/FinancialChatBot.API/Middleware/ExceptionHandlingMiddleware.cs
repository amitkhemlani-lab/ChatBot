using System.Net;
using System.Text.Json;

namespace FinancialChatBot.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteErrorResponseAsync(context, ex);
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = ex switch
        {
            ArgumentNullException => (HttpStatusCode.BadRequest, ex.Message),
            ArgumentException => (HttpStatusCode.BadRequest, ex.Message),
            InvalidOperationException => (HttpStatusCode.UnprocessableEntity, ex.Message),
            TimeoutException => (HttpStatusCode.GatewayTimeout, "The request timed out. Please try again."),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again.")
        };

        context.Response.StatusCode = (int)statusCode;

        var response = JsonSerializer.Serialize(new
        {
            error = message,
            timestamp = DateTime.UtcNow,
            traceId = context.TraceIdentifier
        });

        await context.Response.WriteAsync(response);
    }
}

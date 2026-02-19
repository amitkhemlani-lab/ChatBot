using FinancialChatBot.API.Middleware;
using FinancialChatBot.Infrastructure.Data;
using FinancialChatBot.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ─── Logging ─────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// ─── Services ─────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Financial AI ChatBot API",
        Version = "v1",
        Description = """
            AI-powered financial chatbot API backed by Azure AI Foundry and Azure SQL.

            Provides conversational analysis of:
            - **Bank Statements**: Transaction history, spending patterns, cash flow analysis
            - **Capital Structure**: Equity/debt composition, WACC, leverage ratios, market metrics

            The `/api/chat/message` endpoint accepts natural language queries and returns
            AI-generated financial insights powered by Azure AI Foundry Agents.
            """,
        Contact = new OpenApiContact { Name = "Financial ChatBot Team" }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// Infrastructure (EF Core + Repositories + Foundry Service)
builder.Services.AddInfrastructure(builder.Configuration);

// CORS for web clients
builder.Services.AddCors(options =>
{
    options.AddPolicy("FinancialChatBotPolicy", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3000", "http://localhost:5173"];

        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<FinancialDbContext>("database");

var app = builder.Build();

// ─── Middleware Pipeline ──────────────────────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Financial AI ChatBot API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger at root
    });
}

app.UseSerilogRequestLogging();
app.UseCors("FinancialChatBotPolicy");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

// ─── Database Migration on Startup ───────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FinancialDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying database migrations...");
        await db.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to apply database migrations. Ensure Azure SQL is reachable.");
    }
}

app.Run();

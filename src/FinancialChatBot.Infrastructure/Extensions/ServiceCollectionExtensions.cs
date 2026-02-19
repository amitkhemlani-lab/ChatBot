using FinancialChatBot.Core.Interfaces;
using FinancialChatBot.Infrastructure.Data;
using FinancialChatBot.Infrastructure.Plugins;
using FinancialChatBot.Infrastructure.Repositories;
using FinancialChatBot.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialChatBot.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // ── Azure SQL via Entity Framework Core ──────────────────────────────
        services.AddDbContext<FinancialDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("AzureSql"),
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                    sqlOptions.CommandTimeout(30);
                }));

        // ── Repositories ─────────────────────────────────────────────────────
        services.AddScoped<IBankStatementRepository, BankStatementRepository>();
        services.AddScoped<ICapitalStructureRepository, CapitalStructureRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();

        // ── Semantic Kernel Plugins ───────────────────────────────────────────
        // Scoped so each request gets plugins sharing the same repository instances.
        services.AddScoped<BankStatementPlugin>();
        services.AddScoped<CapitalStructurePlugin>();

        // ── AI Chat Service (Semantic Kernel) ─────────────────────────────────
        services.AddScoped<IFinancialChatService, FinancialChatService>();

        return services;
    }
}

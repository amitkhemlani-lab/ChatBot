using FinancialChatBot.Core.Entities;
using FinancialChatBot.Core.Interfaces;
using FinancialChatBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FinancialChatBot.Infrastructure.Repositories;

public class BankStatementRepository : IBankStatementRepository
{
    private readonly FinancialDbContext _context;
    private readonly ILogger<BankStatementRepository> _logger;

    public BankStatementRepository(FinancialDbContext context, ILogger<BankStatementRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BankAccount?> GetAccountByNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        return await _context.BankAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber, cancellationToken);
    }

    public async Task<IEnumerable<BankAccount>> GetAllAccountsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.BankAccounts
            .AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.AccountHolderName)
            .ToListAsync(cancellationToken);
    }

    public async Task<BankStatement?> GetStatementByPeriodAsync(int accountId, string period, CancellationToken cancellationToken = default)
    {
        return await _context.BankStatements
            .AsNoTracking()
            .Include(s => s.Transactions)
            .FirstOrDefaultAsync(s => s.BankAccountId == accountId && s.StatementPeriod == period, cancellationToken);
    }

    public async Task<IEnumerable<BankStatement>> GetStatementsByAccountAsync(
        int accountId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var query = _context.BankStatements
            .AsNoTracking()
            .Where(s => s.BankAccountId == accountId);

        if (from.HasValue)
            query = query.Where(s => s.PeriodEndDate >= from.Value);

        if (to.HasValue)
            query = query.Where(s => s.PeriodStartDate <= to.Value);

        return await query
            .OrderByDescending(s => s.StatementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByAccountAsync(
        int accountId, DateTime? from = null, DateTime? to = null, string? category = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Transactions
            .AsNoTracking()
            .Where(t => t.BankAccountId == accountId);

        if (from.HasValue)
            query = query.Where(t => t.TransactionDate >= from.Value);

        if (to.HasValue)
            query = query.Where(t => t.TransactionDate <= to.Value);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(t => t.Category == category);

        return await query
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByStatementAsync(
        int statementId, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .AsNoTracking()
            .Where(t => t.BankStatementId == statementId)
            .OrderBy(t => t.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalCreditsAsync(int accountId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .Where(t => t.BankAccountId == accountId
                && t.TransactionType == "Credit"
                && t.TransactionDate >= from
                && t.TransactionDate <= to)
            .SumAsync(t => t.Amount, cancellationToken);
    }

    public async Task<decimal> GetTotalDebitsAsync(int accountId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .Where(t => t.BankAccountId == accountId
                && t.TransactionType == "Debit"
                && t.TransactionDate >= from
                && t.TransactionDate <= to)
            .SumAsync(t => t.Amount, cancellationToken);
    }

    public async Task<IEnumerable<(string Category, decimal Amount, int Count)>> GetSpendingByCategoryAsync(
        int accountId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var results = await _context.Transactions
            .Where(t => t.BankAccountId == accountId
                && t.TransactionType == "Debit"
                && t.TransactionDate >= from
                && t.TransactionDate <= to)
            .GroupBy(t => t.Category)
            .Select(g => new
            {
                Category = g.Key,
                Amount = g.Sum(t => t.Amount),
                Count = g.Count()
            })
            .OrderByDescending(x => x.Amount)
            .ToListAsync(cancellationToken);

        return results.Select(r => (r.Category, r.Amount, r.Count));
    }

    public async Task<string> ExecuteNaturalLanguageQueryAsync(string sqlQuery, CancellationToken cancellationToken = default)
    {
        // Execute read-only parameterized queries for NL-to-SQL results
        // This is called only with AI-generated, validated SQL
        try
        {
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = sqlQuery;
            command.CommandTimeout = 30;

            var results = new List<Dictionary<string, object?>>();
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                results.Add(row);
            }

            return JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query: {Query}", sqlQuery);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}

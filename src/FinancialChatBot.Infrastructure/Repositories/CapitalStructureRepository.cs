using FinancialChatBot.Core.Entities;
using FinancialChatBot.Core.Interfaces;
using FinancialChatBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FinancialChatBot.Infrastructure.Repositories;

public class CapitalStructureRepository : ICapitalStructureRepository
{
    private readonly FinancialDbContext _context;
    private readonly ILogger<CapitalStructureRepository> _logger;

    public CapitalStructureRepository(FinancialDbContext context, ILogger<CapitalStructureRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CapitalStructure?> GetLatestByCompanyAsync(string companyCode, CancellationToken cancellationToken = default)
    {
        return await _context.CapitalStructures
            .AsNoTracking()
            .Include(c => c.Notes)
            .Where(c => c.CompanyCode == companyCode)
            .OrderByDescending(c => c.FiscalYear)
            .ThenByDescending(c => c.Quarter)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<CapitalStructure>> GetByCompanyAndYearAsync(
        string companyCode, int fiscalYear, CancellationToken cancellationToken = default)
    {
        return await _context.CapitalStructures
            .AsNoTracking()
            .Include(c => c.Notes)
            .Where(c => c.CompanyCode == companyCode && c.FiscalYear == fiscalYear)
            .OrderBy(c => c.Quarter)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CapitalStructure>> GetAllCompaniesLatestAsync(CancellationToken cancellationToken = default)
    {
        // Get the latest record per company using a subquery approach
        var latestDates = await _context.CapitalStructures
            .GroupBy(c => c.CompanyCode)
            .Select(g => new { CompanyCode = g.Key, MaxYear = g.Max(c => c.FiscalYear) })
            .ToListAsync(cancellationToken);

        var results = new List<CapitalStructure>();
        foreach (var item in latestDates)
        {
            var latest = await _context.CapitalStructures
                .AsNoTracking()
                .Where(c => c.CompanyCode == item.CompanyCode && c.FiscalYear == item.MaxYear)
                .OrderByDescending(c => c.Quarter)
                .FirstOrDefaultAsync(cancellationToken);

            if (latest != null)
                results.Add(latest);
        }

        return results.OrderBy(c => c.CompanyName);
    }

    public async Task<IEnumerable<string>> GetAllCompanyCodesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CapitalStructures
            .AsNoTracking()
            .Select(c => c.CompanyCode)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }

    public async Task<CapitalStructure?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.CapitalStructures
            .AsNoTracking()
            .Include(c => c.Notes)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<CapitalStructure>> GetHistoricalTrendAsync(
        string companyCode, int years = 5, CancellationToken cancellationToken = default)
    {
        var cutoffYear = DateTime.UtcNow.Year - years;
        return await _context.CapitalStructures
            .AsNoTracking()
            .Where(c => c.CompanyCode == companyCode && c.FiscalYear >= cutoffYear)
            .OrderBy(c => c.FiscalYear)
            .ThenBy(c => c.Quarter)
            .ToListAsync(cancellationToken);
    }

    public async Task<string> ExecuteNaturalLanguageQueryAsync(string sqlQuery, CancellationToken cancellationToken = default)
    {
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
            _logger.LogError(ex, "Error executing capital structure query: {Query}", sqlQuery);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}

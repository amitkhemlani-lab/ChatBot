using FinancialChatBot.Core.Entities;

namespace FinancialChatBot.Core.Interfaces;

public interface ICapitalStructureRepository
{
    Task<CapitalStructure?> GetLatestByCompanyAsync(string companyCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<CapitalStructure>> GetByCompanyAndYearAsync(string companyCode, int fiscalYear, CancellationToken cancellationToken = default);
    Task<IEnumerable<CapitalStructure>> GetAllCompaniesLatestAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetAllCompanyCodesAsync(CancellationToken cancellationToken = default);
    Task<CapitalStructure?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<CapitalStructure>> GetHistoricalTrendAsync(string companyCode, int years = 5, CancellationToken cancellationToken = default);
    Task<string> ExecuteNaturalLanguageQueryAsync(string sqlQuery, CancellationToken cancellationToken = default);
}

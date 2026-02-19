using FinancialChatBot.Core.Entities;

namespace FinancialChatBot.Core.Interfaces;

public interface IBankStatementRepository
{
    Task<BankAccount?> GetAccountByNumberAsync(string accountNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<BankAccount>> GetAllAccountsAsync(CancellationToken cancellationToken = default);
    Task<BankStatement?> GetStatementByPeriodAsync(int accountId, string period, CancellationToken cancellationToken = default);
    Task<IEnumerable<BankStatement>> GetStatementsByAccountAsync(int accountId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetTransactionsByAccountAsync(int accountId, DateTime? from = null, DateTime? to = null, string? category = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetTransactionsByStatementAsync(int statementId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalCreditsAsync(int accountId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalDebitsAsync(int accountId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<IEnumerable<(string Category, decimal Amount, int Count)>> GetSpendingByCategoryAsync(int accountId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<string> ExecuteNaturalLanguageQueryAsync(string sqlQuery, CancellationToken cancellationToken = default);
}

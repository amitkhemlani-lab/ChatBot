using System.ComponentModel;
using System.Text.Json;
using FinancialChatBot.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace FinancialChatBot.Infrastructure.Plugins;

/// <summary>
/// Semantic Kernel plugin exposing bank statement data as callable tools.
/// The AI model automatically decides when and how to invoke these functions
/// based on the user's natural language query.
/// </summary>
public sealed class BankStatementPlugin
{
    private readonly IBankStatementRepository _repo;
    private readonly ILogger<BankStatementPlugin> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public BankStatementPlugin(IBankStatementRepository repo, ILogger<BankStatementPlugin> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    [KernelFunction("get_account_summary")]
    [Description("Returns account holder name, account type, currency, and current balance for a given account number.")]
    public async Task<string> GetAccountSummaryAsync(
        [Description("The bank account number, e.g. ACC-001-2024")] string accountNumber,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Plugin] get_account_summary: {Account}", accountNumber);

        var account = await _repo.GetAccountByNumberAsync(accountNumber, cancellationToken);
        if (account is null)
            return $"No account found with number '{accountNumber}'.";

        return JsonSerializer.Serialize(new
        {
            accountNumber = account.AccountNumber,
            accountHolder = account.AccountHolderName,
            accountType = account.AccountType,
            currency = account.Currency,
            currentBalance = account.Balance,
            openedDate = account.OpenedDate.ToString("yyyy-MM-dd"),
            isActive = account.IsActive
        }, JsonOpts);
    }

    [KernelFunction("list_all_accounts")]
    [Description("Returns a list of all active bank accounts with their balances and types.")]
    public async Task<string> ListAllAccountsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Plugin] list_all_accounts");

        var accounts = await _repo.GetAllAccountsAsync(cancellationToken);
        var result = accounts.Select(a => new
        {
            accountNumber = a.AccountNumber,
            accountHolder = a.AccountHolderName,
            accountType = a.AccountType,
            currency = a.Currency,
            balance = a.Balance
        });

        return JsonSerializer.Serialize(result, JsonOpts);
    }

    [KernelFunction("get_transactions")]
    [Description("""
        Returns transactions for a bank account within an optional date range and/or category filter.
        Use this to answer questions about specific payments, receipts, or spending history.
        Categories include: Revenue, Payroll, Rent, Technology, Marketing, Insurance, Utilities, Operations, Professional.
        """)]
    public async Task<string> GetTransactionsAsync(
        [Description("The bank account number")] string accountNumber,
        [Description("Start date in yyyy-MM-dd format (optional)")] string? fromDate = null,
        [Description("End date in yyyy-MM-dd format (optional)")] string? toDate = null,
        [Description("Category to filter by (optional)")] string? category = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Plugin] get_transactions: {Account} {From}-{To} category={Category}",
            accountNumber, fromDate, toDate, category);

        var account = await _repo.GetAccountByNumberAsync(accountNumber, cancellationToken);
        if (account is null)
            return $"No account found with number '{accountNumber}'.";

        DateTime? from = TryParseDate(fromDate);
        DateTime? to = TryParseDate(toDate);

        var transactions = await _repo.GetTransactionsByAccountAsync(
            account.Id, from, to, category, cancellationToken);

        var list = transactions.Select(t => new
        {
            date = t.TransactionDate.ToString("yyyy-MM-dd"),
            type = t.TransactionType,
            category = t.Category,
            amount = t.Amount,
            currency = account.Currency,
            description = t.Description,
            counterparty = t.CounterpartyName,
            channel = t.Channel,
            runningBalance = t.RunningBalance
        }).ToList();

        return JsonSerializer.Serialize(new
        {
            account = accountNumber,
            transactionCount = list.Count,
            transactions = list
        }, JsonOpts);
    }

    [KernelFunction("get_spending_by_category")]
    [Description("""
        Returns a breakdown of debit spending grouped by category for a given account and date range.
        Use this when asked about spending patterns, top expenses, or where money was spent.
        """)]
    public async Task<string> GetSpendingByCategoryAsync(
        [Description("The bank account number")] string accountNumber,
        [Description("Start date in yyyy-MM-dd format")] string fromDate,
        [Description("End date in yyyy-MM-dd format")] string toDate,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Plugin] get_spending_by_category: {Account} {From}-{To}",
            accountNumber, fromDate, toDate);

        var account = await _repo.GetAccountByNumberAsync(accountNumber, cancellationToken);
        if (account is null)
            return $"No account found with number '{accountNumber}'.";

        var from = DateTime.Parse(fromDate);
        var to = DateTime.Parse(toDate);

        var categories = (await _repo.GetSpendingByCategoryAsync(account.Id, from, to, cancellationToken)).ToList();
        var totalSpend = categories.Sum(c => c.Amount);

        var result = categories.Select(c => new
        {
            category = c.Category,
            amount = c.Amount,
            currency = account.Currency,
            transactionCount = c.Count,
            percentageOfTotal = totalSpend > 0 ? Math.Round(c.Amount / totalSpend * 100, 1) : 0
        });

        return JsonSerializer.Serialize(new
        {
            account = accountNumber,
            period = $"{fromDate} to {toDate}",
            totalSpend,
            currency = account.Currency,
            breakdown = result
        }, JsonOpts);
    }

    [KernelFunction("get_statement_summary")]
    [Description("""
        Returns summary of bank statements (opening/closing balance, total credits/debits, transaction count)
        for a given account. Use this for monthly or period-level cash flow questions.
        """)]
    public async Task<string> GetStatementSummaryAsync(
        [Description("The bank account number")] string accountNumber,
        [Description("Start date in yyyy-MM-dd format (optional)")] string? fromDate = null,
        [Description("End date in yyyy-MM-dd format (optional)")] string? toDate = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Plugin] get_statement_summary: {Account}", accountNumber);

        var account = await _repo.GetAccountByNumberAsync(accountNumber, cancellationToken);
        if (account is null)
            return $"No account found with number '{accountNumber}'.";

        var from = TryParseDate(fromDate);
        var to = TryParseDate(toDate);

        var statements = await _repo.GetStatementsByAccountAsync(account.Id, from, to, cancellationToken);

        var result = statements.Select(s => new
        {
            period = s.StatementPeriod,
            periodStart = s.PeriodStartDate.ToString("yyyy-MM-dd"),
            periodEnd = s.PeriodEndDate.ToString("yyyy-MM-dd"),
            openingBalance = s.OpeningBalance,
            closingBalance = s.ClosingBalance,
            totalCredits = s.TotalCredits,
            totalDebits = s.TotalDebits,
            netCashFlow = s.TotalCredits - s.TotalDebits,
            transactionCount = s.TransactionCount,
            currency = account.Currency
        });

        return JsonSerializer.Serialize(new
        {
            account = accountNumber,
            statements = result
        }, JsonOpts);
    }

    [KernelFunction("get_cash_flow_totals")]
    [Description("Returns total credits (inflows) and total debits (outflows) for an account within a date range. Use for cash flow analysis.")]
    public async Task<string> GetCashFlowTotalsAsync(
        [Description("The bank account number")] string accountNumber,
        [Description("Start date in yyyy-MM-dd format")] string fromDate,
        [Description("End date in yyyy-MM-dd format")] string toDate,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Plugin] get_cash_flow_totals: {Account} {From}-{To}", accountNumber, fromDate, toDate);

        var account = await _repo.GetAccountByNumberAsync(accountNumber, cancellationToken);
        if (account is null)
            return $"No account found with number '{accountNumber}'.";

        var from = DateTime.Parse(fromDate);
        var to = DateTime.Parse(toDate);

        var credits = await _repo.GetTotalCreditsAsync(account.Id, from, to, cancellationToken);
        var debits = await _repo.GetTotalDebitsAsync(account.Id, from, to, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            account = accountNumber,
            currency = account.Currency,
            period = $"{fromDate} to {toDate}",
            totalCredits = credits,
            totalDebits = debits,
            netCashFlow = credits - debits,
            cashFlowPositive = credits > debits
        }, JsonOpts);
    }

    private static DateTime? TryParseDate(string? value) =>
        DateTime.TryParse(value, out var d) ? d : null;
}

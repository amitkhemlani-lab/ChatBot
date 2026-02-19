using FinancialChatBot.Core.DTOs;
using FinancialChatBot.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinancialChatBot.API.Controllers;

/// <summary>
/// Bank statement data management and querying endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BankStatementController : ControllerBase
{
    private readonly IBankStatementRepository _repository;
    private readonly ILogger<BankStatementController> _logger;

    public BankStatementController(IBankStatementRepository repository, ILogger<BankStatementController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Get all active bank accounts.
    /// </summary>
    [HttpGet("accounts")]
    [ProducesResponseType(typeof(IEnumerable<AccountSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AccountSummaryDto>>> GetAllAccounts(CancellationToken cancellationToken)
    {
        var accounts = await _repository.GetAllAccountsAsync(cancellationToken);
        var dtos = accounts.Select(a => new AccountSummaryDto(
            a.AccountNumber, a.AccountHolderName, a.AccountType, a.Currency, a.Balance, a.OpenedDate));
        return Ok(dtos);
    }

    /// <summary>
    /// Get a specific account by account number.
    /// </summary>
    [HttpGet("accounts/{accountNumber}")]
    [ProducesResponseType(typeof(AccountSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountSummaryDto>> GetAccount(string accountNumber, CancellationToken cancellationToken)
    {
        var account = await _repository.GetAccountByNumberAsync(accountNumber, cancellationToken);
        if (account == null) return NotFound();

        return Ok(new AccountSummaryDto(
            account.AccountNumber, account.AccountHolderName, account.AccountType,
            account.Currency, account.Balance, account.OpenedDate));
    }

    /// <summary>
    /// Get statements for an account within a date range.
    /// </summary>
    [HttpGet("accounts/{accountNumber}/statements")]
    [ProducesResponseType(typeof(IEnumerable<StatementSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<StatementSummaryDto>>> GetStatements(
        string accountNumber,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var account = await _repository.GetAccountByNumberAsync(accountNumber, cancellationToken);
        if (account == null) return NotFound(new { error = "Account not found." });

        var statements = await _repository.GetStatementsByAccountAsync(account.Id, from, to, cancellationToken);
        var dtos = statements.Select(s => new StatementSummaryDto(
            s.StatementPeriod, s.PeriodStartDate, s.PeriodEndDate,
            s.OpeningBalance, s.ClosingBalance, s.TotalCredits, s.TotalDebits, s.TransactionCount));
        return Ok(dtos);
    }

    /// <summary>
    /// Get transactions for an account with optional filters.
    /// </summary>
    [HttpGet("accounts/{accountNumber}/transactions")]
    [ProducesResponseType(typeof(IEnumerable<TransactionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions(
        string accountNumber,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? category,
        CancellationToken cancellationToken)
    {
        var account = await _repository.GetAccountByNumberAsync(accountNumber, cancellationToken);
        if (account == null) return NotFound(new { error = "Account not found." });

        var transactions = await _repository.GetTransactionsByAccountAsync(account.Id, from, to, category, cancellationToken);
        var dtos = transactions.Select(t => new TransactionDto(
            t.Id, t.TransactionDate, t.Description, t.TransactionType,
            t.Category, t.Amount, t.RunningBalance, t.Channel, t.CounterpartyName));
        return Ok(dtos);
    }

    /// <summary>
    /// Get spending breakdown by category for an account.
    /// </summary>
    [HttpGet("accounts/{accountNumber}/spending-summary")]
    [ProducesResponseType(typeof(IEnumerable<SpendingCategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SpendingCategoryDto>>> GetSpendingSummary(
        string accountNumber,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken)
    {
        var account = await _repository.GetAccountByNumberAsync(accountNumber, cancellationToken);
        if (account == null) return NotFound(new { error = "Account not found." });

        var categories = await _repository.GetSpendingByCategoryAsync(account.Id, from, to, cancellationToken);
        var totalSpend = categories.Sum(c => c.Amount);

        var dtos = categories.Select(c => new SpendingCategoryDto(
            c.Category, c.Amount, c.Count,
            totalSpend > 0 ? Math.Round(c.Amount / totalSpend * 100, 2) : 0));
        return Ok(dtos);
    }
}

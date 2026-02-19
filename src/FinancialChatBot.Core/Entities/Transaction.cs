namespace FinancialChatBot.Core.Entities;

public class Transaction
{
    public int Id { get; set; }
    public int BankStatementId { get; set; }
    public int BankAccountId { get; set; }
    public string TransactionReference { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public DateTime ValueDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty; // Credit, Debit
    public string Category { get; set; } = string.Empty; // Salary, Transfer, Payment, Withdrawal, etc.
    public decimal Amount { get; set; }
    public decimal RunningBalance { get; set; }
    public string? Narration { get; set; }
    public string? CounterpartyName { get; set; }
    public string? CounterpartyAccount { get; set; }
    public string Channel { get; set; } = string.Empty; // ATM, Online, Branch, POS, etc.
    public string Status { get; set; } = "Completed";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public BankStatement Statement { get; set; } = null!;
    public BankAccount Account { get; set; } = null!;
}

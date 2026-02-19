namespace FinancialChatBot.Core.Entities;

public class BankStatement
{
    public int Id { get; set; }
    public int BankAccountId { get; set; }
    public string StatementPeriod { get; set; } = string.Empty; // e.g., "2024-01"
    public DateTime StatementDate { get; set; }
    public DateTime PeriodStartDate { get; set; }
    public DateTime PeriodEndDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal TotalDebits { get; set; }
    public int TransactionCount { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public BankAccount Account { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

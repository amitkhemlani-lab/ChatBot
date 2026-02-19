namespace FinancialChatBot.Core.Entities;

public class BankAccount
{
    public int Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty; // Checking, Savings, Business
    public string Currency { get; set; } = "USD";
    public decimal Balance { get; set; }
    public DateTime OpenedDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? BranchCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BankStatement> Statements { get; set; } = new List<BankStatement>();
}

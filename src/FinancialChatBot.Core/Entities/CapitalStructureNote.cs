namespace FinancialChatBot.Core.Entities;

public class CapitalStructureNote
{
    public int Id { get; set; }
    public int CapitalStructureId { get; set; }
    public string NoteType { get; set; } = string.Empty; // Debt, Equity, Risk, Covenant
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CapitalStructure CapitalStructure { get; set; } = null!;
}

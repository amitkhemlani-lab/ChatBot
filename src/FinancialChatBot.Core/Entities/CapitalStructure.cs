namespace FinancialChatBot.Core.Entities;

public class CapitalStructure
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyCode { get; set; } = string.Empty;
    public int FiscalYear { get; set; }
    public string Quarter { get; set; } = string.Empty; // Q1, Q2, Q3, Q4, Annual

    // Equity Components
    public decimal CommonStock { get; set; }
    public decimal PreferredStock { get; set; }
    public decimal RetainedEarnings { get; set; }
    public decimal AdditionalPaidInCapital { get; set; }
    public decimal TreasuryStock { get; set; }
    public decimal TotalEquity { get; set; }

    // Debt Components
    public decimal ShortTermDebt { get; set; }
    public decimal LongTermDebt { get; set; }
    public decimal CurrentPortionLongTermDebt { get; set; }
    public decimal Bonds { get; set; }
    public decimal Debentures { get; set; }
    public decimal TotalDebt { get; set; }

    // Capital Metrics
    public decimal TotalCapital { get; set; }
    public decimal DebtToEquityRatio { get; set; }
    public decimal DebtRatio { get; set; }
    public decimal EquityRatio { get; set; }
    public decimal WeightedAverageCostOfCapital { get; set; } // WACC
    public decimal CostOfDebt { get; set; }
    public decimal CostOfEquity { get; set; }

    // Market Data
    public decimal MarketCapitalization { get; set; }
    public decimal EnterpriseValue { get; set; }
    public decimal SharesOutstanding { get; set; }
    public decimal BookValuePerShare { get; set; }
    public decimal MarketValuePerShare { get; set; }

    public DateTime ReportDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CapitalStructureNote> Notes { get; set; } = new List<CapitalStructureNote>();
}

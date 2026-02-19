namespace FinancialChatBot.Core.DTOs;

public record AccountSummaryDto(
    string AccountNumber,
    string AccountHolderName,
    string AccountType,
    string Currency,
    decimal Balance,
    DateTime OpenedDate
);

public record TransactionDto(
    int Id,
    DateTime TransactionDate,
    string Description,
    string TransactionType,
    string Category,
    decimal Amount,
    decimal RunningBalance,
    string Channel,
    string? CounterpartyName
);

public record StatementSummaryDto(
    string StatementPeriod,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal OpeningBalance,
    decimal ClosingBalance,
    decimal TotalCredits,
    decimal TotalDebits,
    int TransactionCount
);

public record CapitalStructureSummaryDto(
    string CompanyName,
    string CompanyCode,
    int FiscalYear,
    string Quarter,
    decimal TotalEquity,
    decimal TotalDebt,
    decimal TotalCapital,
    decimal DebtToEquityRatio,
    decimal WeightedAverageCostOfCapital,
    decimal MarketCapitalization,
    DateTime ReportDate
);

public record SpendingCategoryDto(
    string Category,
    decimal Amount,
    int TransactionCount,
    decimal Percentage
);

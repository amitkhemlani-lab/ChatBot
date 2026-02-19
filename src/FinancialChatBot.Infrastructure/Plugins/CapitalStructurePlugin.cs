using System.ComponentModel;
using System.Text.Json;
using FinancialChatBot.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace FinancialChatBot.Infrastructure.Plugins;

/// <summary>
/// Semantic Kernel plugin exposing capital structure data as callable tools.
/// The AI model automatically decides when and how to invoke these functions
/// based on the user's natural language query about company finances.
/// </summary>
public sealed class CapitalStructurePlugin
{
    private readonly ICapitalStructureRepository _repo;
    private readonly ILogger<CapitalStructurePlugin> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public CapitalStructurePlugin(ICapitalStructureRepository repo, ILogger<CapitalStructurePlugin> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    [KernelFunction("list_companies")]
    [Description("Returns a list of all company codes available in the capital structure database.")]
    public async Task<string> ListCompaniesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Plugin] list_companies");

        var codes = await _repo.GetAllCompanyCodesAsync(cancellationToken);
        return JsonSerializer.Serialize(new { companyCodes = codes }, JsonOpts);
    }

    [KernelFunction("get_capital_structure")]
    [Description("""
        Returns the latest capital structure for a company, including:
        - Equity breakdown (common stock, preferred stock, retained earnings, APIC, treasury stock)
        - Debt breakdown (short-term, long-term, bonds, debentures)
        - Key ratios: debt-to-equity, debt ratio, equity ratio, WACC, cost of debt, cost of equity
        - Market data: market cap, enterprise value, shares outstanding, book/market value per share
        Use this for questions about a company's financing mix, leverage, or capital efficiency.
        """)]
    public async Task<string> GetCapitalStructureAsync(
        [Description("The company code, e.g. TGC, FBL, GEI")] string companyCode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Plugin] get_capital_structure: {Company}", companyCode);

        var cs = await _repo.GetLatestByCompanyAsync(companyCode, cancellationToken);
        if (cs is null)
            return $"No capital structure data found for company code '{companyCode}'. Use list_companies to see available codes.";

        return JsonSerializer.Serialize(new
        {
            company = cs.CompanyName,
            companyCode = cs.CompanyCode,
            fiscalYear = cs.FiscalYear,
            quarter = cs.Quarter,
            reportDate = cs.ReportDate.ToString("yyyy-MM-dd"),
            equity = new
            {
                commonStock = cs.CommonStock,
                preferredStock = cs.PreferredStock,
                retainedEarnings = cs.RetainedEarnings,
                additionalPaidInCapital = cs.AdditionalPaidInCapital,
                treasuryStock = cs.TreasuryStock,
                totalEquity = cs.TotalEquity
            },
            debt = new
            {
                shortTermDebt = cs.ShortTermDebt,
                longTermDebt = cs.LongTermDebt,
                currentPortionOfLongTermDebt = cs.CurrentPortionLongTermDebt,
                bonds = cs.Bonds,
                debentures = cs.Debentures,
                totalDebt = cs.TotalDebt
            },
            capitalMetrics = new
            {
                totalCapital = cs.TotalCapital,
                debtToEquityRatio = cs.DebtToEquityRatio,
                debtRatio = cs.DebtRatio,
                equityRatio = cs.EquityRatio,
                waccPercent = Math.Round(cs.WeightedAverageCostOfCapital * 100, 2),
                costOfDebtPercent = Math.Round(cs.CostOfDebt * 100, 2),
                costOfEquityPercent = Math.Round(cs.CostOfEquity * 100, 2)
            },
            marketData = new
            {
                marketCapitalizationMillions = cs.MarketCapitalization,
                enterpriseValueMillions = cs.EnterpriseValue,
                sharesOutstandingMillions = cs.SharesOutstanding,
                bookValuePerShare = cs.BookValuePerShare,
                marketValuePerShare = cs.MarketValuePerShare,
                priceToBookRatio = cs.BookValuePerShare > 0
                    ? Math.Round(cs.MarketValuePerShare / cs.BookValuePerShare, 2) : 0
            },
            notes = cs.Notes.Select(n => new
            {
                type = n.NoteType,
                title = n.Title,
                content = n.Content
            })
        }, JsonOpts);
    }

    [KernelFunction("get_capital_structure_by_year")]
    [Description("Returns capital structure records for a specific company and fiscal year (all quarters).")]
    public async Task<string> GetCapitalStructureByYearAsync(
        [Description("The company code")] string companyCode,
        [Description("The fiscal year as a 4-digit integer, e.g. 2024")] int fiscalYear,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Plugin] get_capital_structure_by_year: {Company} FY{Year}", companyCode, fiscalYear);

        var records = await _repo.GetByCompanyAndYearAsync(companyCode, fiscalYear, cancellationToken);
        var list = records.Select(cs => new
        {
            quarter = cs.Quarter,
            totalEquity = cs.TotalEquity,
            totalDebt = cs.TotalDebt,
            totalCapital = cs.TotalCapital,
            debtToEquityRatio = cs.DebtToEquityRatio,
            waccPercent = Math.Round(cs.WeightedAverageCostOfCapital * 100, 2),
            marketCapMillions = cs.MarketCapitalization
        });

        return JsonSerializer.Serialize(new
        {
            company = companyCode,
            fiscalYear,
            quarters = list
        }, JsonOpts);
    }

    [KernelFunction("get_capital_structure_trend")]
    [Description("""
        Returns historical capital structure data for a company over multiple years.
        Use this when asked about trends in leverage, WACC, debt reduction, equity growth,
        or any year-over-year comparison of capital structure metrics.
        """)]
    public async Task<string> GetCapitalStructureTrendAsync(
        [Description("The company code")] string companyCode,
        [Description("Number of years of history to return (1-10, default 5)")] int years = 5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Plugin] get_capital_structure_trend: {Company} {Years}yr", companyCode, years);

        var trend = await _repo.GetHistoricalTrendAsync(companyCode, Math.Clamp(years, 1, 10), cancellationToken);
        var list = trend.Select(cs => new
        {
            fiscalYear = cs.FiscalYear,
            quarter = cs.Quarter,
            totalEquity = cs.TotalEquity,
            totalDebt = cs.TotalDebt,
            totalCapital = cs.TotalCapital,
            debtToEquityRatio = cs.DebtToEquityRatio,
            debtRatioPercent = Math.Round(cs.DebtRatio * 100, 1),
            equityRatioPercent = Math.Round(cs.EquityRatio * 100, 1),
            waccPercent = Math.Round(cs.WeightedAverageCostOfCapital * 100, 2),
            marketCapMillions = cs.MarketCapitalization
        });

        return JsonSerializer.Serialize(new
        {
            company = companyCode,
            yearsRequested = years,
            trend = list
        }, JsonOpts);
    }

    [KernelFunction("compare_companies")]
    [Description("""
        Compares the latest capital structure metrics of two companies side by side.
        Use this when asked to compare leverage, WACC, debt levels, or capital efficiency between companies.
        """)]
    public async Task<string> CompareCompaniesAsync(
        [Description("First company code")] string companyCode1,
        [Description("Second company code")] string companyCode2,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Plugin] compare_companies: {A} vs {B}", companyCode1, companyCode2);

        var cs1 = await _repo.GetLatestByCompanyAsync(companyCode1, cancellationToken);
        var cs2 = await _repo.GetLatestByCompanyAsync(companyCode2, cancellationToken);

        if (cs1 is null && cs2 is null)
            return $"No data found for either '{companyCode1}' or '{companyCode2}'.";

        static object? Summarise(Core.Entities.CapitalStructure? cs) => cs is null ? null : new
        {
            company = cs.CompanyName,
            companyCode = cs.CompanyCode,
            fiscalYear = cs.FiscalYear,
            quarter = cs.Quarter,
            totalEquity = cs.TotalEquity,
            totalDebt = cs.TotalDebt,
            totalCapital = cs.TotalCapital,
            debtToEquityRatio = cs.DebtToEquityRatio,
            debtRatioPercent = Math.Round(cs.DebtRatio * 100, 1),
            waccPercent = Math.Round(cs.WeightedAverageCostOfCapital * 100, 2),
            costOfDebtPercent = Math.Round(cs.CostOfDebt * 100, 2),
            costOfEquityPercent = Math.Round(cs.CostOfEquity * 100, 2),
            marketCapMillions = cs.MarketCapitalization,
            enterpriseValueMillions = cs.EnterpriseValue
        };

        return JsonSerializer.Serialize(new
        {
            comparison = new[] { Summarise(cs1), Summarise(cs2) }
        }, JsonOpts);
    }

    [KernelFunction("get_all_companies_summary")]
    [Description("Returns the latest capital structure summary for all companies. Use to get a market-wide overview.")]
    public async Task<string> GetAllCompaniesSummaryAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Plugin] get_all_companies_summary");

        var companies = await _repo.GetAllCompaniesLatestAsync(cancellationToken);
        var result = companies.Select(cs => new
        {
            company = cs.CompanyName,
            companyCode = cs.CompanyCode,
            fiscalYear = cs.FiscalYear,
            quarter = cs.Quarter,
            totalEquity = cs.TotalEquity,
            totalDebt = cs.TotalDebt,
            debtToEquityRatio = cs.DebtToEquityRatio,
            waccPercent = Math.Round(cs.WeightedAverageCostOfCapital * 100, 2),
            marketCapMillions = cs.MarketCapitalization
        });

        return JsonSerializer.Serialize(new { companies = result }, JsonOpts);
    }
}

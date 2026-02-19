using FinancialChatBot.Core.DTOs;
using FinancialChatBot.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinancialChatBot.API.Controllers;

/// <summary>
/// Capital structure data management and analysis endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CapitalStructureController : ControllerBase
{
    private readonly ICapitalStructureRepository _repository;
    private readonly ILogger<CapitalStructureController> _logger;

    public CapitalStructureController(ICapitalStructureRepository repository, ILogger<CapitalStructureController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Get the latest capital structure for all companies.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CapitalStructureSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CapitalStructureSummaryDto>>> GetAllCompanies(CancellationToken cancellationToken)
    {
        var companies = await _repository.GetAllCompaniesLatestAsync(cancellationToken);
        return Ok(companies.Select(MapToDto));
    }

    /// <summary>
    /// Get all tracked company codes.
    /// </summary>
    [HttpGet("companies")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<string>>> GetCompanyCodes(CancellationToken cancellationToken)
    {
        var codes = await _repository.GetAllCompanyCodesAsync(cancellationToken);
        return Ok(codes);
    }

    /// <summary>
    /// Get the latest capital structure for a specific company.
    /// </summary>
    [HttpGet("{companyCode}/latest")]
    [ProducesResponseType(typeof(CapitalStructureSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CapitalStructureSummaryDto>> GetLatest(
        string companyCode, CancellationToken cancellationToken)
    {
        var capital = await _repository.GetLatestByCompanyAsync(companyCode, cancellationToken);
        if (capital == null) return NotFound(new { error = $"No capital structure found for {companyCode}." });
        return Ok(MapToDto(capital));
    }

    /// <summary>
    /// Get capital structure for a specific fiscal year.
    /// </summary>
    [HttpGet("{companyCode}/{fiscalYear:int}")]
    [ProducesResponseType(typeof(IEnumerable<CapitalStructureSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CapitalStructureSummaryDto>>> GetByYear(
        string companyCode, int fiscalYear, CancellationToken cancellationToken)
    {
        var records = await _repository.GetByCompanyAndYearAsync(companyCode, fiscalYear, cancellationToken);
        return Ok(records.Select(MapToDto));
    }

    /// <summary>
    /// Get historical capital structure trend for a company.
    /// </summary>
    [HttpGet("{companyCode}/trend")]
    [ProducesResponseType(typeof(IEnumerable<CapitalStructureSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CapitalStructureSummaryDto>>> GetTrend(
        string companyCode,
        [FromQuery] int years = 5,
        CancellationToken cancellationToken = default)
    {
        if (years < 1 || years > 20)
            return BadRequest(new { error = "years must be between 1 and 20." });

        var trend = await _repository.GetHistoricalTrendAsync(companyCode, years, cancellationToken);
        return Ok(trend.Select(MapToDto));
    }

    private static CapitalStructureSummaryDto MapToDto(Core.Entities.CapitalStructure c) =>
        new(c.CompanyName, c.CompanyCode, c.FiscalYear, c.Quarter,
            c.TotalEquity, c.TotalDebt, c.TotalCapital,
            c.DebtToEquityRatio, c.WeightedAverageCostOfCapital,
            c.MarketCapitalization, c.ReportDate);
}

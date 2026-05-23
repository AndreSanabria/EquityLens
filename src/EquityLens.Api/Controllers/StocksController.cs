using EquityLens.Api.DTOs;
using EquityLens.Api.Services;
using EquityLens.Api.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace EquityLens.Api.Controllers;

[ApiController]
[Route("api/stocks")]
public class StocksController(
    IStockDashboardService stockDashboardService,
    IPerformanceService performanceService,
    IResearchDataProvider researchDataProvider,
    INewsRankingService newsRankingService,
    IRiskAnalysisService riskAnalysisService,
    IResearchSnapshotService researchSnapshotService) : ControllerBase
{
    [HttpGet("supported")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<string>> GetSupportedTickers() =>
        Ok(researchDataProvider.GetSupportedTickers());

    [HttpGet("{ticker}/dashboard")]
    [ProducesResponseType(typeof(StockDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockDashboardDto>> GetDashboard(string ticker, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await stockDashboardService.GetDashboardAsync(ticker, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (TickerNotSupportedException exception)
        {
            return NotFound(new { error = exception.Message });
        }
    }

    [HttpGet("{ticker}/performance")]
    [ProducesResponseType(typeof(PerformanceOverviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PerformanceOverviewDto>> GetPerformance(string ticker, CancellationToken cancellationToken)
    {
        try
        {
            var prices = await researchDataProvider.GetHistoricalPricesAsync(ticker, cancellationToken);
            return Ok(performanceService.BuildOverview(prices));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (TickerNotSupportedException exception)
        {
            return NotFound(new { error = exception.Message });
        }
    }

    [HttpGet("{ticker}/risk")]
    [ProducesResponseType(typeof(RiskScoreDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RiskScoreDto>> GetRisk(string ticker, CancellationToken cancellationToken)
    {
        try
        {
            var prices = await researchDataProvider.GetHistoricalPricesAsync(ticker, cancellationToken);
            var facts = await researchDataProvider.GetFinancialFactsAsync(ticker, cancellationToken);
            var news = await researchDataProvider.GetNewsAsync(ticker, cancellationToken);
            var performance = performanceService.BuildOverview(prices);
            var rankedNews = newsRankingService.Rank(news);
            var risk = riskAnalysisService.CalculateRiskScore(performance, facts, rankedNews);

            return Ok(risk);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (TickerNotSupportedException exception)
        {
            return NotFound(new { error = exception.Message });
        }
    }

    [HttpGet("{ticker}/news")]
    [ProducesResponseType(typeof(IReadOnlyList<RankedNewsItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RankedNewsItemDto>>> GetNews(string ticker, CancellationToken cancellationToken)
    {
        try
        {
            var news = await researchDataProvider.GetNewsAsync(ticker, cancellationToken);
            return Ok(newsRankingService.Rank(news));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (TickerNotSupportedException exception)
        {
            return NotFound(new { error = exception.Message });
        }
    }

    [HttpGet("{ticker}/filings")]
    [ProducesResponseType(typeof(IReadOnlyList<LatestFilingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LatestFilingDto>>> GetFilings(string ticker, CancellationToken cancellationToken)
    {
        try
        {
            var filings = await researchDataProvider.GetRecentFilingsAsync(ticker, cancellationToken);
            return Ok(filings
                .OrderByDescending(filing => filing.FiledAt)
                .Select(filing => new LatestFilingDto(
                    filing.FormType,
                    filing.FiledAt,
                    filing.Description,
                    filing.FilingUrl))
                .ToList());
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (TickerNotSupportedException exception)
        {
            return NotFound(new { error = exception.Message });
        }
    }

    [HttpPost("{ticker}/snapshot")]
    [ProducesResponseType(typeof(ResearchSnapshotDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ResearchSnapshotDto>> CreateSnapshot(string ticker, CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await researchSnapshotService.CreateSnapshotAsync(ticker, cancellationToken);
            return CreatedAtAction(nameof(GetSnapshots), new { ticker }, snapshot);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (TickerNotSupportedException exception)
        {
            return NotFound(new { error = exception.Message });
        }
    }

    [HttpGet("{ticker}/snapshots")]
    [ProducesResponseType(typeof(IReadOnlyList<ResearchSnapshotDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ResearchSnapshotDto>>> GetSnapshots(string ticker, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await researchSnapshotService.GetSnapshotsAsync(ticker, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}

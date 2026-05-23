using EquityLens.Api.Data;
using EquityLens.Api.DTOs;
using EquityLens.Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace EquityLens.Api.Services;

public class StockDashboardService(
    IResearchDataProvider researchDataProvider,
    IPerformanceService performanceService,
    INewsRankingService newsRankingService,
    IRiskAnalysisService riskAnalysisService,
    IFinancialDirectionService financialDirectionService,
    IResearchSummaryService researchSummaryService,
    EquityLensDbContext dbContext) : IStockDashboardService
{
    public async Task<StockDashboardDto> GetDashboardAsync(string ticker, CancellationToken cancellationToken)
    {
        var normalizedTicker = TickerNormalizer.Normalize(ticker);

        var prices = await researchDataProvider.GetHistoricalPricesAsync(normalizedTicker, cancellationToken);
        var profile = await researchDataProvider.GetCompanyProfileAsync(normalizedTicker, cancellationToken);

        var financialFactsTask = researchDataProvider.GetFinancialFactsAsync(normalizedTicker, cancellationToken);
        var newsTask = researchDataProvider.GetNewsAsync(normalizedTicker, cancellationToken);
        var filingsTask = researchDataProvider.GetRecentFilingsAsync(normalizedTicker, cancellationToken);

        await Task.WhenAll(financialFactsTask, newsTask, filingsTask);

        var financialFacts = await financialFactsTask;
        var rawNews = await newsTask;
        var filings = await filingsTask;

        var performance = performanceService.BuildOverview(prices);
        var rankedNews = newsRankingService.Rank(rawNews);
        var risk = riskAnalysisService.CalculateRiskScore(performance, financialFacts, rankedNews);
        var financialDirection = financialDirectionService.BuildDirection(financialFacts);
        var summary = researchSummaryService.BuildSummary(normalizedTicker, performance, risk, financialDirection);

        await UpdateWatchlistMetadataAsync(normalizedTicker, risk.FinalScore, cancellationToken);

        return new StockDashboardDto(
            Ticker: normalizedTicker,
            GeneratedAt: DateTime.UtcNow,
            NarrativeSummary: summary,
            CompanyProfile: new CompanyProfileDto(
                profile.Ticker,
                profile.CompanyName,
                profile.Sector,
                profile.Industry,
                profile.Exchange,
                profile.Cik,
                profile.MarketCap,
                profile.CurrentPrice,
                profile.FiftyTwoWeekHigh,
                profile.FiftyTwoWeekLow,
                profile.LatestFilingForm,
                profile.LastUpdated),
            Performance: performance,
            RiskAnalysis: risk,
            FinancialDirection: financialDirection,
            RelevantNews: rankedNews,
            LatestFilings: filings
                .OrderByDescending(filing => filing.FiledAt)
                .Select(filing => new LatestFilingDto(
                    filing.FormType,
                    filing.FiledAt,
                    filing.Description,
                    filing.FilingUrl))
                .ToList(),
            DataFreshness: new DataFreshnessDto(
                ProviderMode: researchDataProvider.ProviderName,
                PriceDataThrough: prices.MaxBy(price => price.Date)?.Date,
                FinancialDataFiledAt: financialFacts.MaxBy(fact => fact.FiledAt)?.FiledAt,
                NewsUpdatedAt: rankedNews.MaxBy(news => news.PublishedAt)?.PublishedAt,
                FilingsUpdatedAt: filings.MaxBy(filing => filing.FiledAt)?.FiledAt,
                Limitations: BuildLimitations()));
    }

    private IReadOnlyList<string> BuildLimitations()
    {
        if (researchDataProvider.ProviderName.Contains("Alpha Vantage", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                "Price history and news use Alpha Vantage when ApiProviderOptions:MarketDataProvider is set to AlphaVantage.",
                "Financial facts and filings come from SEC EDGAR and can lag company events.",
                "Market data availability depends on the configured Alpha Vantage API plan and rate limits."
            ];
        }

        if (researchDataProvider.ProviderName.StartsWith("Live:", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                "Price history uses Yahoo Finance chart data. For production, replace this with a contracted market-data provider.",
                "Financial facts and filings come from SEC EDGAR and can lag company events.",
                "News comes from Yahoo Finance RSS and may include broad market articles that mention the ticker."
            ];
        }

        return
        [
            "Demo mode uses generated sample prices, financials, and headlines. It is useful for UI review, not investment research."
        ];
    }

    private async Task UpdateWatchlistMetadataAsync(string ticker, int riskScore, CancellationToken cancellationToken)
    {
        var watchlistItem = await dbContext.WatchlistItems
            .SingleOrDefaultAsync(item => item.Ticker == ticker, cancellationToken);

        if (watchlistItem is null)
        {
            return;
        }

        watchlistItem.LastViewedAt = DateTime.UtcNow;
        watchlistItem.LastKnownRiskScore = riskScore;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

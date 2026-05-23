using EquityLens.Api.DTOs;
using EquityLens.Api.Models;

namespace EquityLens.Api.Services;

public interface IResearchDataProvider
{
    string ProviderName { get; }
    Task<CompanyProfile> GetCompanyProfileAsync(string ticker, CancellationToken cancellationToken);
    Task<IReadOnlyList<HistoricalPrice>> GetHistoricalPricesAsync(string ticker, CancellationToken cancellationToken);
    Task<IReadOnlyList<FinancialFact>> GetFinancialFactsAsync(string ticker, CancellationToken cancellationToken);
    Task<IReadOnlyList<NewsArticle>> GetNewsAsync(string ticker, CancellationToken cancellationToken);
    Task<IReadOnlyList<SecFiling>> GetRecentFilingsAsync(string ticker, CancellationToken cancellationToken);
    IReadOnlyList<string> GetSupportedTickers();
}

public interface IApiRequestLogService
{
    Task LogAsync(
        string provider,
        string endpointName,
        string ticker,
        int statusCode,
        bool success,
        string? errorMessage,
        CancellationToken cancellationToken);
}

public interface IPerformanceService
{
    PerformanceOverviewDto BuildOverview(IReadOnlyList<HistoricalPrice> prices);
}

public interface INewsRankingService
{
    IReadOnlyList<RankedNewsItemDto> Rank(IReadOnlyList<NewsArticle> articles);
    int CalculateNewsRiskScore(IReadOnlyList<RankedNewsItemDto> rankedNews);
}

public interface IFinancialDirectionService
{
    FinancialDirectionDto BuildDirection(IReadOnlyList<FinancialFact> facts);
}

public interface IRiskAnalysisService
{
    RiskScoreDto CalculateRiskScore(
        PerformanceOverviewDto performance,
        IReadOnlyList<FinancialFact> facts,
        IReadOnlyList<RankedNewsItemDto> rankedNews);
}

public interface IResearchSummaryService
{
    string BuildSummary(
        string ticker,
        PerformanceOverviewDto performance,
        RiskScoreDto risk,
        FinancialDirectionDto financialDirection);
}

public interface IStockDashboardService
{
    Task<StockDashboardDto> GetDashboardAsync(string ticker, CancellationToken cancellationToken);
}

public interface IResearchSnapshotService
{
    Task<ResearchSnapshotDto> CreateSnapshotAsync(string ticker, CancellationToken cancellationToken);
    Task<IReadOnlyList<ResearchSnapshotDto>> GetSnapshotsAsync(string ticker, CancellationToken cancellationToken);
}

public interface IWatchlistService
{
    Task<IReadOnlyList<WatchlistItemDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<WatchlistItemDto> AddOrUpdateAsync(CreateWatchlistItemRequest request, CancellationToken cancellationToken);
    Task<WatchlistItemDto> UpdateNotesAsync(string ticker, UpdateWatchlistNotesRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string ticker, CancellationToken cancellationToken);
}

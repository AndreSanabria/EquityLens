namespace EquityLens.Api.DTOs;

public record StockDashboardDto(
    string Ticker,
    DateTime GeneratedAt,
    string NarrativeSummary,
    CompanyProfileDto CompanyProfile,
    PerformanceOverviewDto Performance,
    RiskScoreDto RiskAnalysis,
    FinancialDirectionDto FinancialDirection,
    IReadOnlyList<RankedNewsItemDto> RelevantNews,
    IReadOnlyList<LatestFilingDto> LatestFilings,
    DataFreshnessDto DataFreshness);

public record CompanyProfileDto(
    string Ticker,
    string CompanyName,
    string Sector,
    string Industry,
    string Exchange,
    string Cik,
    decimal MarketCap,
    decimal CurrentPrice,
    decimal FiftyTwoWeekHigh,
    decimal FiftyTwoWeekLow,
    string LatestFilingForm,
    DateTime LastUpdated);

public record PerformanceOverviewDto(
    decimal CurrentPrice,
    IReadOnlyList<ReturnMetricDto> Returns,
    IReadOnlyList<PricePointDto> ChartPoints,
    decimal AnnualizedVolatility,
    decimal MaxDrawdown);

public record ReturnMetricDto(string Period, decimal PercentReturn);

public record PricePointDto(DateTime Date, decimal Close, long Volume);

public record RiskScoreDto(
    int FinalScore,
    string RiskLevel,
    int VolatilityScore,
    int MaxDrawdownScore,
    int RevenueInstabilityScore,
    int EarningsInstabilityScore,
    int DebtPressureScore,
    int NewsRiskScore,
    IReadOnlyList<string> MainDrivers,
    IReadOnlyList<RiskComponentDetailDto> Components);

public record RiskComponentDetailDto(
    string Name,
    int Score,
    decimal Weight,
    string MetricValue,
    string Explanation);

public record FinancialDirectionDto(
    IReadOnlyList<FinancialMetricDirectionDto> Metrics,
    string OverallDirection);

public record FinancialMetricDirectionDto(
    string MetricName,
    decimal PreviousValue,
    decimal CurrentValue,
    string DirectionLabel);

public record RankedNewsItemDto(
    string Title,
    string Source,
    string Url,
    bool IsDirectArticleUrl,
    DateTime PublishedAt,
    string Category,
    int RelevanceScore);

public record LatestFilingDto(
    string FormType,
    DateTime FiledAt,
    string Description,
    string FilingUrl);

public record DataFreshnessDto(
    string ProviderMode,
    DateTime? PriceDataThrough,
    DateTime? FinancialDataFiledAt,
    DateTime? NewsUpdatedAt,
    DateTime? FilingsUpdatedAt,
    IReadOnlyList<string> Limitations);

public record WatchlistItemDto(
    int Id,
    string Ticker,
    string Notes,
    DateTime AddedAt,
    DateTime LastViewedAt,
    int? LastKnownRiskScore);

public record CreateWatchlistItemRequest(string Ticker, string? Notes);

public record UpdateWatchlistNotesRequest(string Notes);

public record ResearchSnapshotDto(
    int Id,
    string Ticker,
    DateTime CreatedAt,
    int RiskScore,
    decimal OneYearReturn,
    string Summary);

public record MethodologyDto(
    string Summary,
    string RiskFormula,
    IReadOnlyList<MethodologyComponentDto> Components);

public record MethodologyComponentDto(
    string Name,
    decimal Weight,
    string Description);

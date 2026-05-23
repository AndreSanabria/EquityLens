namespace EquityLens.Api.Models;

public class CompanyProfile
{
    public string Ticker { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public string Cik { get; set; } = string.Empty;
    public decimal MarketCap { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal FiftyTwoWeekHigh { get; set; }
    public decimal FiftyTwoWeekLow { get; set; }
    public string LatestFilingForm { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}

public class HistoricalPrice
{
    public int Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal AdjustedClose { get; set; }
    public long Volume { get; set; }
}

public class FinancialFact
{
    public int Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public int FiscalYear { get; set; }
    public string FiscalPeriod { get; set; } = "FY";
    public decimal Value { get; set; }
    public string Source { get; set; } = "SEC Demo";
    public DateTime FiledAt { get; set; }
}

public class NewsArticle
{
    public int Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool IsDirectArticleUrl { get; set; }
    public DateTime PublishedAt { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class SecFiling
{
    public string Ticker { get; set; } = string.Empty;
    public string FormType { get; set; } = string.Empty;
    public DateTime FiledAt { get; set; }
    public string Description { get; set; } = string.Empty;
    public string FilingUrl { get; set; } = string.Empty;
}

public class WatchlistItem
{
    public int Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
    public DateTime LastViewedAt { get; set; }
    public int? LastKnownRiskScore { get; set; }
}

public class ResearchSnapshot
{
    public int Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int RiskScore { get; set; }
    public decimal OneYearReturn { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string DashboardJson { get; set; } = string.Empty;
}

public class ApiRequestLog
{
    public int Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string EndpointName { get; set; } = string.Empty;
    public string Ticker { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
}

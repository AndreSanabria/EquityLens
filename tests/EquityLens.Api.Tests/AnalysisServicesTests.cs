using EquityLens.Api.DTOs;
using EquityLens.Api.Models;
using EquityLens.Api.Services;
using EquityLens.Api.Utilities;

namespace EquityLens.Api.Tests;

public class AnalysisServicesTests
{
    [Fact]
    public void BuildOverview_CalculatesExpectedNamedReturns()
    {
        var today = DateTime.UtcNow.Date;
        var prices = new List<HistoricalPrice>
        {
            CreatePrice(today.AddYears(-5), 100m),
            CreatePrice(today.AddYears(-1), 150m),
            CreatePrice(today.AddMonths(-6), 170m),
            CreatePrice(today.AddMonths(-3), 180m),
            CreatePrice(today.AddMonths(-1), 190m),
            CreatePrice(today, 210m)
        };

        var service = new PerformanceService();
        var overview = service.BuildOverview(prices);

        Assert.Equal(210m, overview.CurrentPrice);
        Assert.Equal(10.53m, overview.Returns.Single(metric => metric.Period == "1M").PercentReturn);
        Assert.Equal(40.00m, overview.Returns.Single(metric => metric.Period == "1Y").PercentReturn);
        Assert.Equal(110.00m, overview.Returns.Single(metric => metric.Period == "5Y").PercentReturn);
        Assert.True(overview.AnnualizedVolatility > 0m);
        Assert.True(overview.MaxDrawdown >= 0m);
    }

    [Fact]
    public void Rank_PrioritizesLegalHeadlineOverGenericProductHeadline()
    {
        var service = new NewsRankingService();
        var now = DateTime.UtcNow;
        var articles = new List<NewsArticle>
        {
            new()
            {
                Ticker = "MSFT",
                Title = "Microsoft faces SEC investigation over cloud contracting disclosures",
                Source = "Reuters",
                Url = "https://example.test/legal",
                PublishedAt = now.AddDays(-1)
            },
            new()
            {
                Ticker = "MSFT",
                Title = "Microsoft expands platform roadmap for enterprise users",
                Source = "Blog",
                Url = "https://example.test/product",
                PublishedAt = now.AddDays(-20)
            }
        };

        var ranked = service.Rank(articles);

        Assert.Equal("Legal", ranked[0].Category);
        Assert.True(ranked[0].RelevanceScore > ranked[1].RelevanceScore);
    }

    [Fact]
    public void CalculateRiskScore_HighlightsDrawdownAndDebtPressure()
    {
        var newsRankingService = new NewsRankingService();
        var riskService = new RiskAnalysisService(newsRankingService);
        var performance = new PerformanceOverviewDto(
            CurrentPrice: 210m,
            Returns:
            [
                new ReturnMetricDto("1M", 3.2m),
                new ReturnMetricDto("1Y", -8.4m)
            ],
            ChartPoints:
            [
                new PricePointDto(DateTime.UtcNow.Date.AddDays(-1), 205m, 10_000_000),
                new PricePointDto(DateTime.UtcNow.Date, 210m, 12_000_000)
            ],
            AnnualizedVolatility: 58m,
            MaxDrawdown: 44m);

        var facts = new List<FinancialFact>
        {
            new() { MetricName = "Revenue", FiscalYear = 2022, Value = 140m },
            new() { MetricName = "Revenue", FiscalYear = 2023, Value = 136m },
            new() { MetricName = "Revenue", FiscalYear = 2024, Value = 130m },
            new() { MetricName = "NetIncome", FiscalYear = 2022, Value = 18m },
            new() { MetricName = "NetIncome", FiscalYear = 2023, Value = 12m },
            new() { MetricName = "NetIncome", FiscalYear = 2024, Value = 7m },
            new() { MetricName = "Cash", FiscalYear = 2023, Value = 10m },
            new() { MetricName = "Cash", FiscalYear = 2024, Value = 9m },
            new() { MetricName = "Debt", FiscalYear = 2023, Value = 17m },
            new() { MetricName = "Debt", FiscalYear = 2024, Value = 24m },
            new() { MetricName = "Liabilities", FiscalYear = 2023, Value = 48m },
            new() { MetricName = "Liabilities", FiscalYear = 2024, Value = 57m }
        };

        var rankedNews = newsRankingService.Rank(
        [
            new NewsArticle
            {
                Ticker = "TSLA",
                Title = "Tesla faces new investigation after guidance cut",
                Source = "Reuters",
                Url = "https://example.test/risk",
                PublishedAt = DateTime.UtcNow.AddDays(-2)
            }
        ]);

        var risk = riskService.CalculateRiskScore(performance, facts, rankedNews);

        Assert.True(risk.FinalScore >= 70);
        Assert.Contains("Significant recent drawdown", risk.MainDrivers);
        Assert.True(risk.DebtPressureScore >= 70);
        Assert.True(risk.RiskLevel is "High" or "Very high");
        Assert.Equal(6, risk.Components.Count);
        Assert.Contains(risk.Components, component =>
            component.Name == "Debt pressure" &&
            component.Explanation.Contains("Debt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildDirection_LabelsOperatingAndBalanceSheetChanges()
    {
        var service = new FinancialDirectionService();
        var facts = new List<FinancialFact>
        {
            new() { MetricName = "Revenue", FiscalYear = 2023, Value = 100m },
            new() { MetricName = "Revenue", FiscalYear = 2024, Value = 108m },
            new() { MetricName = "NetIncome", FiscalYear = 2023, Value = 20m },
            new() { MetricName = "NetIncome", FiscalYear = 2024, Value = 18m },
            new() { MetricName = "Debt", FiscalYear = 2023, Value = 30m },
            new() { MetricName = "Debt", FiscalYear = 2024, Value = 25m },
            new() { MetricName = "Liabilities", FiscalYear = 2023, Value = 50m },
            new() { MetricName = "Liabilities", FiscalYear = 2024, Value = 56m }
        };

        var direction = service.BuildDirection(facts);

        Assert.Contains(direction.Metrics, metric =>
            metric.MetricName == "Revenue" &&
            metric.DirectionLabel == "Improving");
        Assert.Contains(direction.Metrics, metric =>
            metric.MetricName == "NetIncome" &&
            metric.DirectionLabel == "Weakening");
        Assert.Contains(direction.Metrics, metric =>
            metric.MetricName == "Debt" &&
            metric.DirectionLabel == "Lower Risk");
        Assert.Contains(direction.Metrics, metric =>
            metric.MetricName == "Liabilities" &&
            metric.DirectionLabel == "Higher Risk");
    }

    [Fact]
    public void Normalize_AllowsCommonShareClassTickerFormat()
    {
        Assert.Equal("BRK-B", TickerNormalizer.Normalize("brk.b"));
    }

    private static HistoricalPrice CreatePrice(DateTime date, decimal close) =>
        new()
        {
            Ticker = "MSFT",
            Date = date,
            Open = close,
            High = close,
            Low = close,
            Close = close,
            AdjustedClose = close,
            Volume = 10_000_000
        };
}

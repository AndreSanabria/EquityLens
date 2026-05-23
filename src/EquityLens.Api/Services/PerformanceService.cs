using EquityLens.Api.DTOs;
using EquityLens.Api.Models;

namespace EquityLens.Api.Services;

public class PerformanceService : IPerformanceService
{
    public PerformanceOverviewDto BuildOverview(IReadOnlyList<HistoricalPrice> prices)
    {
        if (prices.Count < 2)
        {
            throw new ArgumentException("At least two price points are required to calculate performance.");
        }

        var orderedPrices = prices.OrderBy(price => price.Date).ToList();
        var latest = orderedPrices[^1];
        var asOfDate = latest.Date.Date;
        var returns = new List<ReturnMetricDto>
        {
            BuildReturnMetric("1M", asOfDate.AddMonths(-1), latest.Close, orderedPrices),
            BuildReturnMetric("3M", asOfDate.AddMonths(-3), latest.Close, orderedPrices),
            BuildReturnMetric("6M", asOfDate.AddMonths(-6), latest.Close, orderedPrices),
            BuildReturnMetric("1Y", asOfDate.AddYears(-1), latest.Close, orderedPrices),
            BuildReturnMetric("5Y", asOfDate.AddYears(-5), latest.Close, orderedPrices)
        };

        var chartPoints = orderedPrices
            .Select(price => new PricePointDto(price.Date, price.Close, price.Volume))
            .ToList();

        return new PerformanceOverviewDto(
            CurrentPrice: latest.Close,
            Returns: returns,
            ChartPoints: chartPoints,
            AnnualizedVolatility: decimal.Round(CalculateAnnualizedVolatilityPercent(orderedPrices), 2),
            MaxDrawdown: decimal.Round(CalculateMaxDrawdownPercent(orderedPrices), 2));
    }

    internal static decimal CalculateAnnualizedVolatilityPercent(IReadOnlyList<HistoricalPrice> prices)
    {
        var returns = new List<double>();

        for (var index = 1; index < prices.Count; index++)
        {
            var previousClose = prices[index - 1].Close;
            var currentClose = prices[index].Close;

            if (previousClose <= 0)
            {
                continue;
            }

            returns.Add((double)((currentClose - previousClose) / previousClose));
        }

        if (returns.Count == 0)
        {
            return 0m;
        }

        var mean = returns.Average();
        var variance = returns.Select(value => Math.Pow(value - mean, 2)).Average();
        var annualizedVolatility = Math.Sqrt(variance) * Math.Sqrt(252d) * 100d;
        return (decimal)annualizedVolatility;
    }

    internal static decimal CalculateMaxDrawdownPercent(IReadOnlyList<HistoricalPrice> prices)
    {
        var peak = prices[0].Close;
        var maxDrawdown = 0m;

        foreach (var price in prices)
        {
            if (price.Close > peak)
            {
                peak = price.Close;
            }

            if (peak <= 0)
            {
                continue;
            }

            var drawdown = (peak - price.Close) / peak;
            if (drawdown > maxDrawdown)
            {
                maxDrawdown = drawdown;
            }
        }

        return maxDrawdown * 100m;
    }

    private static ReturnMetricDto BuildReturnMetric(
        string label,
        DateTime targetDate,
        decimal currentPrice,
        IReadOnlyList<HistoricalPrice> prices)
    {
        var anchor = prices
            .Where(price => price.Date <= targetDate)
            .MaxBy(price => price.Date) ?? prices[0];

        var percentReturn = anchor.Close == 0m
            ? 0m
            : ((currentPrice - anchor.Close) / anchor.Close) * 100m;

        return new ReturnMetricDto(label, decimal.Round(percentReturn, 2));
    }
}

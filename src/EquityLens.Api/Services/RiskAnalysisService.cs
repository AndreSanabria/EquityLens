using EquityLens.Api.DTOs;
using EquityLens.Api.Models;
using System.Globalization;

namespace EquityLens.Api.Services;

public class RiskAnalysisService(INewsRankingService newsRankingService) : IRiskAnalysisService
{
    public RiskScoreDto CalculateRiskScore(
        PerformanceOverviewDto performance,
        IReadOnlyList<FinancialFact> facts,
        IReadOnlyList<RankedNewsItemDto> rankedNews)
    {
        var volatilityScore = ScoreBetween(performance.AnnualizedVolatility, 20m, 60m);
        var maxDrawdownScore = ScoreBetween(performance.MaxDrawdown, 10m, 50m);
        var revenueInstability = CalculateTrendInstability(facts, "Revenue", 1.15m);
        var earningsInstability = CalculateTrendInstability(facts, "NetIncome", 1.45m);
        var debtPressure = CalculateDebtPressure(facts);
        var revenueInstabilityScore = revenueInstability.Score;
        var earningsInstabilityScore = earningsInstability.Score;
        var debtPressureScore = debtPressure.Score;
        var newsRiskScore = newsRankingService.CalculateNewsRiskScore(rankedNews);

        var finalScore = (int)Math.Round(
            (volatilityScore * 0.30m) +
            (maxDrawdownScore * 0.25m) +
            (revenueInstabilityScore * 0.15m) +
            (earningsInstabilityScore * 0.15m) +
            (debtPressureScore * 0.10m) +
            (newsRiskScore * 0.05m));

        var componentScores = new Dictionary<string, int>
        {
            ["Elevated volatility"] = volatilityScore,
            ["Significant recent drawdown"] = maxDrawdownScore,
            ["Revenue trend instability"] = revenueInstabilityScore,
            ["Earnings pressure"] = earningsInstabilityScore,
            ["Debt pressure"] = debtPressureScore,
            ["Headline risk"] = newsRiskScore
        };

        var mainDrivers = componentScores
            .Where(pair => pair.Value >= 50)
            .OrderByDescending(pair => pair.Value)
            .Select(pair => pair.Key)
            .Take(3)
            .ToList();

        if (mainDrivers.Count == 0)
        {
            mainDrivers = componentScores
                .OrderByDescending(pair => pair.Value)
                .Select(pair => pair.Key)
                .Take(2)
            .ToList();
        }

        var components = BuildComponentDetails(
            performance,
            rankedNews,
            volatilityScore,
            maxDrawdownScore,
            revenueInstability,
            earningsInstability,
            debtPressure,
            newsRiskScore);

        return new RiskScoreDto(
            FinalScore: finalScore,
            RiskLevel: LabelRiskLevel(finalScore),
            VolatilityScore: volatilityScore,
            MaxDrawdownScore: maxDrawdownScore,
            RevenueInstabilityScore: revenueInstabilityScore,
            EarningsInstabilityScore: earningsInstabilityScore,
            DebtPressureScore: debtPressureScore,
            NewsRiskScore: newsRiskScore,
            MainDrivers: mainDrivers,
            Components: components);
    }

    private static IReadOnlyList<RiskComponentDetailDto> BuildComponentDetails(
        PerformanceOverviewDto performance,
        IReadOnlyList<RankedNewsItemDto> rankedNews,
        int volatilityScore,
        int maxDrawdownScore,
        TrendInstabilityResult revenueInstability,
        TrendInstabilityResult earningsInstability,
        DebtPressureResult debtPressure,
        int newsRiskScore)
    {
        var topNewsCategories = rankedNews
            .Take(5)
            .GroupBy(news => news.Category)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key)
            .Take(2)
            .ToList();
        var newsCategoryText = topNewsCategories.Count == 0
            ? "No recent ranked headlines were available."
            : $"Top recent headline categories: {string.Join(", ", topNewsCategories)}.";

        return
        [
            new RiskComponentDetailDto(
                "Volatility",
                volatilityScore,
                0.30m,
                UnsignedPercent(performance.AnnualizedVolatility),
                $"Annualized volatility is {UnsignedPercent(performance.AnnualizedVolatility)}. The score is low near 20% and reaches the highest-risk band around 60%."),
            new RiskComponentDetailDto(
                "Max drawdown",
                maxDrawdownScore,
                0.25m,
                UnsignedPercent(performance.MaxDrawdown),
                $"The largest peak-to-trough decline is {UnsignedPercent(performance.MaxDrawdown)}. The score is low below 10% and reaches the highest-risk band around 50%."),
            new RiskComponentDetailDto(
                "Revenue instability",
                revenueInstability.Score,
                0.15m,
                revenueInstability.MetricValue,
                revenueInstability.Explanation),
            new RiskComponentDetailDto(
                "Earnings instability",
                earningsInstability.Score,
                0.15m,
                earningsInstability.MetricValue,
                earningsInstability.Explanation),
            new RiskComponentDetailDto(
                "Debt pressure",
                debtPressure.Score,
                0.10m,
                debtPressure.MetricValue,
                debtPressure.Explanation),
            new RiskComponentDetailDto(
                "News risk",
                newsRiskScore,
                0.05m,
                rankedNews.Count == 0 ? "No ranked headlines" : $"{rankedNews.Count} ranked headlines",
                $"{newsCategoryText} Legal, debt, layoff, and leadership headlines carry more risk weight than general product news.")
        ];
    }

    private static TrendInstabilityResult CalculateTrendInstability(
        IReadOnlyList<FinancialFact> facts,
        string metricName,
        decimal sensitivity)
    {
        var values = facts
            .Where(fact => fact.MetricName == metricName)
            .OrderBy(fact => fact.FiscalYear)
            .Select(fact => fact.Value)
            .ToList();

        if (values.Count < 2)
        {
            return new TrendInstabilityResult(
                50,
                "Insufficient SEC history",
                $"Not enough annual {DisplayMetric(metricName)} history was available, so the model uses a neutral score of 50.");
        }

        var changes = new List<decimal>();
        for (var index = 1; index < values.Count; index++)
        {
            if (values[index - 1] == 0m)
            {
                continue;
            }

            changes.Add((values[index] - values[index - 1]) / values[index - 1]);
        }

        if (changes.Count == 0)
        {
            return new TrendInstabilityResult(
                50,
                "Insufficient comparable history",
                $"Annual {DisplayMetric(metricName)} values could not be compared safely, so the model uses a neutral score of 50.");
        }

        var avgAbsoluteChange = changes.Average(change => Math.Abs(change));
        var latestChange = changes[^1];
        var signFlipPenalty = changes.Zip(changes.Skip(1), (left, right) => Math.Sign(left) != Math.Sign(right) ? 8m : 0m).Sum();
        var downsidePenalty = latestChange < 0m ? Math.Abs(latestChange) * 120m : 0m;
        var rawScore = (avgAbsoluteChange * 100m * sensitivity) + downsidePenalty + signFlipPenalty;
        var score = Math.Clamp((int)Math.Round(rawScore), 0, 100);
        var explanation = $"Latest annual {DisplayMetric(metricName)} change is {SignedPercent(latestChange * 100m)}. Average absolute annual change is {UnsignedPercent(avgAbsoluteChange * 100m)}, with extra penalty when the latest change is negative or the trend changes direction.";

        return new TrendInstabilityResult(
            score,
            $"Latest change {SignedPercent(latestChange * 100m)}",
            explanation);
    }

    private static DebtPressureResult CalculateDebtPressure(IReadOnlyList<FinancialFact> facts)
    {
        var hasDebt = facts.Any(fact => fact.MetricName == "Debt");
        var hasCash = facts.Any(fact => fact.MetricName == "Cash");
        var hasLiabilities = facts.Any(fact => fact.MetricName == "Liabilities");

        if (!hasDebt || !hasCash)
        {
            return new DebtPressureResult(
                50,
                "Insufficient debt/cash data",
                "SEC debt and cash facts were not both available, so the model uses a neutral debt pressure score of 50.");
        }

        var latestDebt = GetLatestValue(facts, "Debt");
        var latestCash = GetLatestValue(facts, "Cash");
        var latestLiabilities = GetLatestValue(facts, "Liabilities");
        var previousLiabilities = GetPreviousValue(facts, "Liabilities");
        var debtToCash = latestCash == 0m
            ? latestDebt == 0m ? 0m : 3m
            : latestDebt / latestCash;
        var liabilitiesGrowth = !hasLiabilities || previousLiabilities == 0m
            ? 0m
            : (latestLiabilities - previousLiabilities) / previousLiabilities;

        var baseScore = debtToCash switch
        {
            < 0.6m => 12,
            < 1.0m => 24,
            < 1.5m => 38,
            < 2.0m => 56,
            < 3.0m => 74,
            _ => 90
        };

        var growthPenalty = liabilitiesGrowth > 0.10m
            ? 10
            : liabilitiesGrowth > 0.04m
                ? 5
                : 0;

        var score = Math.Clamp(baseScore + growthPenalty, 0, 100);
        var metricValue = $"Debt/cash {debtToCash.ToString("0.0x", CultureInfo.InvariantCulture)}";
        var explanation = $"Latest debt is {debtToCash.ToString("0.0", CultureInfo.InvariantCulture)}x latest cash. Liability growth is {SignedPercent(liabilitiesGrowth * 100m)}. Rising liabilities add a penalty when growth exceeds 4% or 10%.";

        return new DebtPressureResult(score, metricValue, explanation);
    }

    private static decimal GetLatestValue(IReadOnlyList<FinancialFact> facts, string metricName) =>
        facts
            .Where(fact => fact.MetricName == metricName)
            .OrderBy(fact => fact.FiscalYear)
            .Select(fact => fact.Value)
            .LastOrDefault();

    private static decimal GetPreviousValue(IReadOnlyList<FinancialFact> facts, string metricName) =>
        facts
            .Where(fact => fact.MetricName == metricName)
            .OrderBy(fact => fact.FiscalYear)
            .Select(fact => fact.Value)
            .Reverse()
            .Skip(1)
            .FirstOrDefault();

    private static int ScoreBetween(decimal value, decimal lowRiskThreshold, decimal highRiskThreshold)
    {
        if (value <= lowRiskThreshold)
        {
            return 0;
        }

        if (value >= highRiskThreshold)
        {
            return 100;
        }

        var normalized = (value - lowRiskThreshold) / (highRiskThreshold - lowRiskThreshold);
        return Math.Clamp((int)Math.Round(normalized * 100m), 0, 100);
    }

    private static string LabelRiskLevel(int finalScore) =>
        finalScore switch
        {
            <= 25 => "Low",
            <= 50 => "Moderate",
            <= 75 => "High",
            _ => "Very high"
        };

    private static string SignedPercent(decimal value) =>
        value.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + "%";

    private static string UnsignedPercent(decimal value) =>
        Math.Abs(value).ToString("0.0", CultureInfo.InvariantCulture) + "%";

    private static string DisplayMetric(string metricName) =>
        metricName == "NetIncome" ? "net income" : metricName.ToLowerInvariant();

    private sealed record TrendInstabilityResult(int Score, string MetricValue, string Explanation);

    private sealed record DebtPressureResult(int Score, string MetricValue, string Explanation);
}

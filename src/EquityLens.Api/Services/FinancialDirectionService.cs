using EquityLens.Api.DTOs;
using EquityLens.Api.Models;

namespace EquityLens.Api.Services;

public class FinancialDirectionService : IFinancialDirectionService
{
    private static readonly HashSet<string> RiskWeightedMetrics =
    [
        "Liabilities",
        "Debt"
    ];

    public FinancialDirectionDto BuildDirection(IReadOnlyList<FinancialFact> facts)
    {
        var metrics = facts
            .GroupBy(fact => fact.MetricName)
            .Select(group =>
            {
                var ordered = group
                    .OrderBy(fact => fact.FiscalYear)
                    .ToList();

                if (ordered.Count < 2)
                {
                    return null;
                }

                var previous = ordered[^2];
                var current = ordered[^1];
                var label = BuildLabel(group.Key, previous.Value, current.Value);

                return new FinancialMetricDirectionDto(
                    group.Key,
                    previous.Value,
                    current.Value,
                    label);
            })
            .Where(metric => metric is not null)
            .Cast<FinancialMetricDirectionDto>()
            .OrderBy(metric => metric.MetricName)
            .ToList();

        var improvingCount = metrics.Count(metric => metric.DirectionLabel is "Improving" or "Lower Risk");
        var weakeningCount = metrics.Count(metric => metric.DirectionLabel is "Weakening" or "Higher Risk");

        var overallDirection = improvingCount > weakeningCount
            ? "Improving"
            : weakeningCount > improvingCount
                ? "Mixed to weakening"
                : "Mixed";

        return new FinancialDirectionDto(metrics, overallDirection);
    }

    private static string BuildLabel(string metricName, decimal previousValue, decimal currentValue)
    {
        if (previousValue == 0m)
        {
            return "Flat";
        }

        var changeRatio = (currentValue - previousValue) / previousValue;

        if (RiskWeightedMetrics.Contains(metricName))
        {
            if (changeRatio >= 0.05m)
            {
                return "Higher Risk";
            }

            if (changeRatio <= -0.05m)
            {
                return "Lower Risk";
            }

            return "Flat";
        }

        if (changeRatio >= 0.03m)
        {
            return "Improving";
        }

        if (changeRatio <= -0.03m)
        {
            return "Weakening";
        }

        return "Flat";
    }
}

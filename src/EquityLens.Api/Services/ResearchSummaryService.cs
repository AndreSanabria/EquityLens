using EquityLens.Api.DTOs;

namespace EquityLens.Api.Services;

public class ResearchSummaryService : IResearchSummaryService
{
    public string BuildSummary(
        string ticker,
        PerformanceOverviewDto performance,
        RiskScoreDto risk,
        FinancialDirectionDto financialDirection)
    {
        var oneYearReturn = performance.Returns.FirstOrDefault(metric => metric.Period == "1Y")?.PercentReturn ?? 0m;
        var sixMonthReturn = performance.Returns.FirstOrDefault(metric => metric.Period == "6M")?.PercentReturn ?? 0m;
        var threeMonthReturn = performance.Returns.FirstOrDefault(metric => metric.Period == "3M")?.PercentReturn ?? 0m;
        var topDrivers = risk.MainDrivers.Count == 0
            ? "no single dominant risk driver"
            : string.Join(", ", risk.MainDrivers.Select(driver => driver.ToLowerInvariant()));

        var openingClause = oneYearReturn switch
        {
            > 20m when risk.FinalScore < 50 => $"{ticker} has delivered strong 1-year performance at {oneYearReturn:+0.0;-0.0;0.0}% while staying in a {risk.RiskLevel.ToLowerInvariant()} risk band.",
            > 0m => $"{ticker} remains positive over the last year at {oneYearReturn:+0.0;-0.0;0.0}%, but the risk score is {risk.FinalScore}/100, which places it in the {risk.RiskLevel.ToLowerInvariant()} range.",
            _ => $"{ticker} is down {Math.Abs(oneYearReturn):0.0}% over the last year, so the dashboard treats downside risk as a primary review item."
        };

        var momentumClause = $"Recent momentum is {sixMonthReturn:+0.0;-0.0;0.0}% over 6 months and {threeMonthReturn:+0.0;-0.0;0.0}% over 3 months, which helps show whether the longer-term result is still strengthening or fading.";

        var drawdownClause = performance.MaxDrawdown > 30m
            ? $"The largest historical drawdown in the available series is {performance.MaxDrawdown:0.0}%, which is a major caution signal because it shows how far the stock fell from a prior peak."
            : $"The largest historical drawdown is {performance.MaxDrawdown:0.0}%, which is more manageable than the highest-risk range used by the model.";

        var volatilityClause = $"Annualized volatility is {performance.AnnualizedVolatility:0.0}%, and the largest score contributors are {topDrivers}.";

        var financialClause = financialDirection.OverallDirection switch
        {
            "Improving" => "Recent financial direction looks broadly constructive because more tracked financial metrics are improving or lowering risk than weakening.",
            "Mixed to weakening" => "Financial direction is mixed to weakening, so the app flags the business trend as a risk factor rather than relying only on stock price.",
            _ => "Financial direction is mixed, which means the tracked fundamentals do not point clearly toward either improvement or deterioration."
        };

        return string.Join(" ", openingClause, momentumClause, drawdownClause, volatilityClause, financialClause);
    }
}

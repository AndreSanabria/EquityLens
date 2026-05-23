using System.Text.Json;
using EquityLens.Api.Data;
using EquityLens.Api.DTOs;
using EquityLens.Api.Models;
using EquityLens.Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace EquityLens.Api.Services;

public class ResearchSnapshotService(
    EquityLensDbContext dbContext,
    IStockDashboardService stockDashboardService) : IResearchSnapshotService
{
    public async Task<ResearchSnapshotDto> CreateSnapshotAsync(string ticker, CancellationToken cancellationToken)
    {
        var normalizedTicker = TickerNormalizer.Normalize(ticker);
        var dashboard = await stockDashboardService.GetDashboardAsync(normalizedTicker, cancellationToken);
        var oneYearReturn = dashboard.Performance.Returns.FirstOrDefault(metric => metric.Period == "1Y")?.PercentReturn ?? 0m;

        var snapshot = new ResearchSnapshot
        {
            Ticker = normalizedTicker,
            CreatedAt = DateTime.UtcNow,
            RiskScore = dashboard.RiskAnalysis.FinalScore,
            OneYearReturn = oneYearReturn,
            Summary = dashboard.NarrativeSummary,
            DashboardJson = JsonSerializer.Serialize(dashboard)
        };

        dbContext.ResearchSnapshots.Add(snapshot);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(snapshot);
    }

    public async Task<IReadOnlyList<ResearchSnapshotDto>> GetSnapshotsAsync(string ticker, CancellationToken cancellationToken)
    {
        var normalizedTicker = TickerNormalizer.Normalize(ticker);

        return await dbContext.ResearchSnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.Ticker == normalizedTicker)
            .OrderByDescending(snapshot => snapshot.CreatedAt)
            .Select(snapshot => Map(snapshot))
            .ToListAsync(cancellationToken);
    }

    private static ResearchSnapshotDto Map(ResearchSnapshot snapshot) =>
        new(
            snapshot.Id,
            snapshot.Ticker,
            snapshot.CreatedAt,
            snapshot.RiskScore,
            snapshot.OneYearReturn,
            snapshot.Summary);
}

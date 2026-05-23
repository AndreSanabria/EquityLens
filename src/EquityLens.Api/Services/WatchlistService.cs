using EquityLens.Api.Data;
using EquityLens.Api.DTOs;
using EquityLens.Api.Models;
using EquityLens.Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace EquityLens.Api.Services;

public class WatchlistService(
    EquityLensDbContext dbContext,
    IStockDashboardService stockDashboardService) : IWatchlistService
{
    public async Task<IReadOnlyList<WatchlistItemDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.WatchlistItems
            .AsNoTracking()
            .OrderByDescending(item => item.LastViewedAt)
            .ThenByDescending(item => item.AddedAt)
            .Select(item => Map(item))
            .ToListAsync(cancellationToken);
    }

    public async Task<WatchlistItemDto> AddOrUpdateAsync(CreateWatchlistItemRequest request, CancellationToken cancellationToken)
    {
        var ticker = TickerNormalizer.Normalize(request.Ticker);
        var dashboard = await stockDashboardService.GetDashboardAsync(ticker, cancellationToken);
        var item = await dbContext.WatchlistItems.SingleOrDefaultAsync(existing => existing.Ticker == ticker, cancellationToken);
        var now = DateTime.UtcNow;

        if (item is null)
        {
            item = new WatchlistItem
            {
                Ticker = ticker,
                Notes = request.Notes?.Trim() ?? string.Empty,
                AddedAt = now,
                LastViewedAt = now,
                LastKnownRiskScore = dashboard.RiskAnalysis.FinalScore
            };

            dbContext.WatchlistItems.Add(item);
        }
        else
        {
            item.Notes = string.IsNullOrWhiteSpace(request.Notes) ? item.Notes : request.Notes.Trim();
            item.LastViewedAt = now;
            item.LastKnownRiskScore = dashboard.RiskAnalysis.FinalScore;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<WatchlistItemDto> UpdateNotesAsync(string ticker, UpdateWatchlistNotesRequest request, CancellationToken cancellationToken)
    {
        var normalizedTicker = TickerNormalizer.Normalize(ticker);
        var item = await dbContext.WatchlistItems.SingleOrDefaultAsync(existing => existing.Ticker == normalizedTicker, cancellationToken)
            ?? throw new KeyNotFoundException($"Watchlist item '{normalizedTicker}' was not found.");

        item.Notes = request.Notes.Trim();
        item.LastViewedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<bool> DeleteAsync(string ticker, CancellationToken cancellationToken)
    {
        var normalizedTicker = TickerNormalizer.Normalize(ticker);
        var item = await dbContext.WatchlistItems.SingleOrDefaultAsync(existing => existing.Ticker == normalizedTicker, cancellationToken);

        if (item is null)
        {
            return false;
        }

        dbContext.WatchlistItems.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static WatchlistItemDto Map(WatchlistItem item) =>
        new(
            item.Id,
            item.Ticker,
            item.Notes,
            item.AddedAt,
            item.LastViewedAt,
            item.LastKnownRiskScore);
}

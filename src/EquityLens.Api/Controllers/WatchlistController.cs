using EquityLens.Api.DTOs;
using EquityLens.Api.Services;
using EquityLens.Api.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace EquityLens.Api.Controllers;

[ApiController]
[Route("api/watchlist")]
public class WatchlistController(IWatchlistService watchlistService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WatchlistItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WatchlistItemDto>>> GetWatchlist(CancellationToken cancellationToken) =>
        Ok(await watchlistService.GetAllAsync(cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(WatchlistItemDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<WatchlistItemDto>> AddWatchlistItem(
        [FromBody] CreateWatchlistItemRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var item = await watchlistService.AddOrUpdateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetWatchlist), new { ticker = item.Ticker }, item);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (TickerNotSupportedException exception)
        {
            return NotFound(new { error = exception.Message });
        }
    }

    [HttpPut("{ticker}/notes")]
    [ProducesResponseType(typeof(WatchlistItemDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WatchlistItemDto>> UpdateNotes(
        string ticker,
        [FromBody] UpdateWatchlistNotesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await watchlistService.UpdateNotesAsync(ticker, request, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
    }

    [HttpDelete("{ticker}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string ticker, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await watchlistService.DeleteAsync(ticker, cancellationToken);
            return deleted ? NoContent() : NotFound(new { error = $"Watchlist item '{ticker}' was not found." });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}

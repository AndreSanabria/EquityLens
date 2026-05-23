namespace EquityLens.Api.Utilities;

public class TickerNotSupportedException(string ticker)
    : Exception($"Ticker '{ticker}' is not available in the current provider mode.")
{
    public string Ticker { get; } = ticker;
}

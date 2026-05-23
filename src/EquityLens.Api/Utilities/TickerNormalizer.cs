using System.Text.RegularExpressions;

namespace EquityLens.Api.Utilities;

public static partial class TickerNormalizer
{
    [GeneratedRegex("^[A-Z0-9][A-Z0-9-]{0,9}$", RegexOptions.Compiled)]
    private static partial Regex TickerPattern();

    public static string Normalize(string ticker)
    {
        if (string.IsNullOrWhiteSpace(ticker))
        {
            throw new ArgumentException("Ticker is required.");
        }

        var normalized = ticker
            .Trim()
            .ToUpperInvariant()
            .Replace('.', '-');

        if (!TickerPattern().IsMatch(normalized))
        {
            throw new ArgumentException("Ticker must be 1 to 10 letters, numbers, dots, or hyphens.");
        }

        return normalized;
    }
}

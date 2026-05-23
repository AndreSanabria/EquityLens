namespace EquityLens.Api.Configuration;

public class ApiProviderOptions
{
    public const string SectionName = "ApiProviderOptions";

    public string Mode { get; set; } = "Live";

    public string MarketDataProvider { get; set; } = "YahooFinance";

    public string AlphaVantageApiKey { get; set; } = string.Empty;

    public string SecUserAgent { get; set; } = "EquityLens Research Dashboard contact@example.com";

    public List<string> SupportedTickers { get; set; } = new();
}

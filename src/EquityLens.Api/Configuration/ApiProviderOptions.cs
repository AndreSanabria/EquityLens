namespace EquityLens.Api.Configuration;

public class ApiProviderOptions
{
    public const string SectionName = "ApiProviderOptions";

    public string Mode { get; set; } = "Live";

    public string SecUserAgent { get; set; } = "EquityLens Portfolio App contact@example.com";

    public List<string> SupportedTickers { get; set; } = new();
}

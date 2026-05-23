using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using EquityLens.Api.Configuration;
using EquityLens.Api.Models;
using EquityLens.Api.Utilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace EquityLens.Api.Services;

public class LiveResearchDataProvider(
    HttpClient httpClient,
    IMemoryCache cache,
    IOptions<ApiProviderOptions> options,
    IApiRequestLogService apiRequestLogService) : IResearchDataProvider
{
    private const string ProviderNameValue = "Live: Yahoo Finance chart/RSS + SEC EDGAR";
    private const string MarketUserAgent = "EquityLens/1.0 research-dashboard";

    private readonly HashSet<string> _supportedTickers = new(
        (options.Value.SupportedTickers.Count == 0 ? ["AAPL", "MSFT", "NVDA", "TSLA", "GOOG", "AMZN"] : options.Value.SupportedTickers)
            .Select(TickerNormalizer.Normalize),
        StringComparer.OrdinalIgnoreCase);

    private readonly string _secUserAgent = options.Value.SecUserAgent;

    public string ProviderName => ProviderNameValue;

    public IReadOnlyList<string> GetSupportedTickers() =>
        _supportedTickers.OrderBy(ticker => ticker).ToArray();

    public async Task<CompanyProfile> GetCompanyProfileAsync(string ticker, CancellationToken cancellationToken)
    {
        var normalized = TickerNormalizer.Normalize(ticker);
        var company = await GetCompanyAsync(normalized, cancellationToken);
        var prices = await GetHistoricalPricesAsync(normalized, cancellationToken);
        var submissions = await GetSubmissionsAsync(company.CikPadded, cancellationToken);
        var enrichment = CompanyEnrichment.GetValueOrDefault(normalized);
        var latest = prices[^1];
        var latestFilingForm = FindLatestImportantForm(submissions.RootElement);
        var trailingYear = prices
            .Where(price => price.Date >= latest.Date.AddYears(-1))
            .ToList();

        return new CompanyProfile
        {
            Ticker = normalized,
            CompanyName = company.Title,
            Sector = enrichment?.Sector ?? "N/A",
            Industry = enrichment?.Industry ?? "N/A",
            Exchange = enrichment?.Exchange ?? "N/A",
            Cik = company.CikPadded,
            MarketCap = 0m,
            CurrentPrice = latest.Close,
            FiftyTwoWeekHigh = decimal.Round(trailingYear.Max(price => price.High), 2),
            FiftyTwoWeekLow = decimal.Round(trailingYear.Min(price => price.Low), 2),
            LatestFilingForm = latestFilingForm,
            LastUpdated = DateTime.UtcNow
        };
    }

    public async Task<IReadOnlyList<HistoricalPrice>> GetHistoricalPricesAsync(string ticker, CancellationToken cancellationToken)
    {
        var normalized = TickerNormalizer.Normalize(ticker);

        if (cache.TryGetValue<IReadOnlyList<HistoricalPrice>>($"prices:{normalized}", out var cachedPrices) &&
            cachedPrices is not null)
        {
            return cachedPrices;
        }

        try
        {
            var uri = $"https://query1.finance.yahoo.com/v8/finance/chart/{normalized}?range=5y&interval=1d";
            using var request = CreateMarketRequest(uri);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            response.EnsureSuccessStatusCode();

            var prices = ParseYahooPrices(normalized, content);

            await apiRequestLogService.LogAsync(ProviderNameValue, "yahoo-chart", normalized, (int)response.StatusCode, true, null, cancellationToken);
            cache.Set($"prices:{normalized}", prices, TimeSpan.FromMinutes(20));

            return prices;
        }
        catch (Exception ex)
        {
            await apiRequestLogService.LogAsync(ProviderNameValue, "yahoo-chart", normalized, 500, false, ex.Message, cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<FinancialFact>> GetFinancialFactsAsync(string ticker, CancellationToken cancellationToken)
    {
        var normalized = TickerNormalizer.Normalize(ticker);
        var company = await GetCompanyAsync(normalized, cancellationToken);

        if (cache.TryGetValue<IReadOnlyList<FinancialFact>>($"facts:{company.CikPadded}", out var cachedFacts) &&
            cachedFacts is not null)
        {
            return cachedFacts;
        }

        try
        {
            using var request = CreateSecRequest($"https://data.sec.gov/api/xbrl/companyfacts/CIK{company.CikPadded}.json");
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            response.EnsureSuccessStatusCode();

            using var document = JsonDocument.Parse(content);
            var facts = ParseCompanyFacts(normalized, document.RootElement);

            await apiRequestLogService.LogAsync(ProviderNameValue, "sec-companyfacts", normalized, (int)response.StatusCode, true, null, cancellationToken);
            cache.Set($"facts:{company.CikPadded}", facts, TimeSpan.FromHours(12));

            return facts;
        }
        catch (Exception ex)
        {
            await apiRequestLogService.LogAsync(ProviderNameValue, "sec-companyfacts", normalized, 500, false, ex.Message, cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<NewsArticle>> GetNewsAsync(string ticker, CancellationToken cancellationToken)
    {
        var normalized = TickerNormalizer.Normalize(ticker);

        if (cache.TryGetValue<IReadOnlyList<NewsArticle>>($"news:{normalized}", out var cachedNews) &&
            cachedNews is not null)
        {
            return cachedNews;
        }

        try
        {
            var uri = $"https://feeds.finance.yahoo.com/rss/2.0/headline?s={normalized}&region=US&lang=en-US";
            using var request = CreateMarketRequest(uri);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            response.EnsureSuccessStatusCode();

            var news = ParseYahooNews(normalized, content);

            await apiRequestLogService.LogAsync(ProviderNameValue, "yahoo-rss-news", normalized, (int)response.StatusCode, true, null, cancellationToken);
            cache.Set($"news:{normalized}", news, TimeSpan.FromMinutes(20));

            return news;
        }
        catch (Exception ex)
        {
            await apiRequestLogService.LogAsync(ProviderNameValue, "yahoo-rss-news", normalized, 500, false, ex.Message, cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<SecFiling>> GetRecentFilingsAsync(string ticker, CancellationToken cancellationToken)
    {
        var normalized = TickerNormalizer.Normalize(ticker);
        var company = await GetCompanyAsync(normalized, cancellationToken);
        var submissions = await GetSubmissionsAsync(company.CikPadded, cancellationToken);
        var cikNoLeadingZeros = company.CikPadded.TrimStart('0');
        var root = submissions.RootElement;
        var recent = root.GetProperty("filings").GetProperty("recent");
        var forms = recent.GetProperty("form");
        var filingDates = recent.GetProperty("filingDate");
        var accessionNumbers = recent.GetProperty("accessionNumber");
        var primaryDocuments = recent.GetProperty("primaryDocument");
        var filings = new List<SecFiling>();

        for (var index = 0; index < forms.GetArrayLength() && filings.Count < 12; index++)
        {
            var formType = forms[index].GetString() ?? string.Empty;

            if (formType is not ("10-K" or "10-Q" or "8-K"))
            {
                continue;
            }

            var accessionNumber = accessionNumbers[index].GetString() ?? string.Empty;
            var accessionPath = accessionNumber.Replace("-", string.Empty, StringComparison.Ordinal);
            var primaryDocument = primaryDocuments[index].GetString() ?? string.Empty;

            filings.Add(new SecFiling
            {
                Ticker = normalized,
                FormType = formType,
                FiledAt = DateTime.SpecifyKind(DateTime.Parse(filingDates[index].GetString() ?? string.Empty, CultureInfo.InvariantCulture), DateTimeKind.Utc),
                Description = formType switch
                {
                    "10-K" => "Annual report covering full-year results, company risks, and business details",
                    "10-Q" => "Quarterly report covering recent financial performance and updates",
                    _ => "Current report for a material company event"
                },
                FilingUrl = $"https://www.sec.gov/Archives/edgar/data/{cikNoLeadingZeros}/{accessionPath}/{primaryDocument}"
            });
        }

        return filings;
    }

    private async Task<CompanyTicker> GetCompanyAsync(string ticker, CancellationToken cancellationToken)
    {
        var normalized = TickerNormalizer.Normalize(ticker);
        var companies = await cache.GetOrCreateAsync("sec:company-tickers", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);

            using var request = CreateSecRequest("https://www.sec.gov/files/company_tickers.json");
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            response.EnsureSuccessStatusCode();
            await apiRequestLogService.LogAsync(ProviderNameValue, "sec-company-tickers", normalized, (int)response.StatusCode, true, null, cancellationToken);

            using var document = JsonDocument.Parse(content);
            var results = new Dictionary<string, CompanyTicker>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in document.RootElement.EnumerateObject())
            {
                var item = property.Value;
                var symbol = item.GetProperty("ticker").GetString() ?? string.Empty;
                var cik = item.GetProperty("cik_str").GetInt32();
                var title = item.GetProperty("title").GetString() ?? symbol;

                results[symbol] = new CompanyTicker(symbol, cik.ToString("D10", CultureInfo.InvariantCulture), title);
            }

            return results;
        });

        if (companies is null || !companies.TryGetValue(normalized, out var company))
        {
            throw new TickerNotSupportedException(normalized);
        }

        return company;
    }

    private async Task<JsonDocument> GetSubmissionsAsync(string cikPadded, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue<JsonDocument>($"sec:submissions:{cikPadded}", out var cachedSubmissions) &&
            cachedSubmissions is not null)
        {
            return cachedSubmissions;
        }

        using var request = CreateSecRequest($"https://data.sec.gov/submissions/CIK{cikPadded}.json");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        var document = JsonDocument.Parse(content);
        cache.Set($"sec:submissions:{cikPadded}", document, TimeSpan.FromHours(4));

        return document;
    }

    private HttpRequestMessage CreateSecRequest(string uri)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.TryAddWithoutValidation("User-Agent", _secUserAgent);

        return request;
    }

    private static HttpRequestMessage CreateMarketRequest(string uri)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.TryAddWithoutValidation("User-Agent", MarketUserAgent);
        request.Headers.TryAddWithoutValidation("Accept", "application/json, application/xml, text/xml;q=0.9, */*;q=0.8");

        return request;
    }

    private static IReadOnlyList<HistoricalPrice> ParseYahooPrices(string ticker, string content)
    {
        using var document = JsonDocument.Parse(content);
        var result = document.RootElement.GetProperty("chart").GetProperty("result")[0];
        var timestamps = result.GetProperty("timestamp");
        var quote = result.GetProperty("indicators").GetProperty("quote")[0];
        var opens = quote.GetProperty("open");
        var highs = quote.GetProperty("high");
        var lows = quote.GetProperty("low");
        var closes = quote.GetProperty("close");
        var volumes = quote.GetProperty("volume");
        var adjustedCloses = result.GetProperty("indicators").TryGetProperty("adjclose", out var adjCloseRoot)
            ? adjCloseRoot[0].GetProperty("adjclose")
            : closes;
        var prices = new List<HistoricalPrice>();

        for (var index = 0; index < timestamps.GetArrayLength(); index++)
        {
            if (closes[index].ValueKind is JsonValueKind.Null)
            {
                continue;
            }

            var close = GetDecimal(closes[index]);

            prices.Add(new HistoricalPrice
            {
                Ticker = ticker,
                Date = DateTimeOffset.FromUnixTimeSeconds(timestamps[index].GetInt64()).UtcDateTime.Date,
                Open = opens[index].ValueKind is JsonValueKind.Null ? close : GetDecimal(opens[index]),
                High = highs[index].ValueKind is JsonValueKind.Null ? close : GetDecimal(highs[index]),
                Low = lows[index].ValueKind is JsonValueKind.Null ? close : GetDecimal(lows[index]),
                Close = close,
                AdjustedClose = adjustedCloses[index].ValueKind is JsonValueKind.Null ? close : GetDecimal(adjustedCloses[index]),
                Volume = volumes[index].ValueKind is JsonValueKind.Null ? 0 : volumes[index].GetInt64()
            });
        }

        if (prices.Count < 2)
        {
            throw new InvalidOperationException($"Not enough price history was returned for {ticker}.");
        }

        return prices.OrderBy(price => price.Date).ToList();
    }

    private static IReadOnlyList<FinancialFact> ParseCompanyFacts(string ticker, JsonElement root)
    {
        var factsRoot = root.GetProperty("facts").GetProperty("us-gaap");
        var facts = new List<FinancialFact>();

        AddMetric(facts, factsRoot, ticker, "Revenue", ["RevenueFromContractWithCustomerExcludingAssessedTax", "Revenues", "SalesRevenueNet"]);
        AddMetric(facts, factsRoot, ticker, "NetIncome", ["NetIncomeLoss"]);
        AddMetric(facts, factsRoot, ticker, "Assets", ["Assets"]);
        AddMetric(facts, factsRoot, ticker, "Liabilities", ["Liabilities"]);
        AddMetric(facts, factsRoot, ticker, "Cash", ["CashAndCashEquivalentsAtCarryingValue", "CashCashEquivalentsRestrictedCashAndRestrictedCashEquivalents"]);
        AddMetric(facts, factsRoot, ticker, "Debt",
        [
            "LongTermDebtAndFinanceLeaseObligations",
            "DebtAndFinanceLeaseObligations",
            "LongTermDebt",
            "LongTermDebtNoncurrent",
            "LongTermDebtAndFinanceLeaseObligationsNoncurrent",
            "LongTermDebtCurrent",
            "LongTermDebtAndFinanceLeaseObligationsCurrent"
        ]);

        return facts
            .GroupBy(fact => new { fact.MetricName, fact.FiscalYear })
            .Select(group => group.OrderByDescending(fact => fact.FiledAt).First())
            .OrderBy(fact => fact.MetricName)
            .ThenBy(fact => fact.FiscalYear)
            .ToList();
    }

    private static void AddMetric(
        List<FinancialFact> facts,
        JsonElement factsRoot,
        string ticker,
        string metricName,
        IReadOnlyList<string> candidateTags)
    {
        foreach (var tag in candidateTags)
        {
            if (!factsRoot.TryGetProperty(tag, out var factRoot) ||
                !factRoot.TryGetProperty("units", out var unitsRoot) ||
                !unitsRoot.TryGetProperty("USD", out var usdFacts))
            {
                continue;
            }

            var metricFacts = usdFacts
                .EnumerateArray()
                .Where(item =>
                    item.TryGetProperty("fy", out _) &&
                    item.TryGetProperty("val", out _) &&
                    item.TryGetProperty("filed", out _) &&
                    item.TryGetProperty("fp", out var fp) &&
                    string.Equals(fp.GetString(), "FY", StringComparison.OrdinalIgnoreCase) &&
                    item.TryGetProperty("form", out var form) &&
                    (form.GetString() is "10-K" or "10-K/A"))
                .Select(item => new FinancialFact
                {
                    Ticker = ticker,
                    MetricName = metricName,
                    FiscalYear = item.GetProperty("fy").GetInt32(),
                    FiscalPeriod = "FY",
                    Value = GetDecimal(item.GetProperty("val")),
                    Source = $"SEC us-gaap:{tag}",
                    FiledAt = DateTime.SpecifyKind(DateTime.Parse(item.GetProperty("filed").GetString() ?? string.Empty, CultureInfo.InvariantCulture), DateTimeKind.Utc)
                })
                .OrderByDescending(fact => fact.FiscalYear)
                .ThenByDescending(fact => fact.FiledAt)
                .Take(5)
                .ToList();

            if (metricFacts.Count > 0)
            {
                facts.AddRange(metricFacts);
                return;
            }
        }
    }

    private static IReadOnlyList<NewsArticle> ParseYahooNews(string ticker, string content)
    {
        var document = XDocument.Parse(content);
        var items = document.Descendants("item");

        return items
            .Select((item, index) => new NewsArticle
            {
                Id = index + 1,
                Ticker = ticker,
                Title = item.Element("title")?.Value ?? "Untitled article",
                Source = BuildNewsSource(item.Element("link")?.Value ?? string.Empty),
                Url = item.Element("link")?.Value ?? string.Empty,
                IsDirectArticleUrl = true,
                PublishedAt = DateTime.TryParse(item.Element("pubDate")?.Value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var published)
                    ? DateTime.SpecifyKind(published, DateTimeKind.Utc)
                    : DateTime.UtcNow,
                Category = "General"
            })
            .Where(article => !string.IsNullOrWhiteSpace(article.Url))
            .Take(20)
            .ToList();
    }

    private static string BuildNewsSource(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return "Yahoo Finance";
        }

        return uri.Host.Replace("www.", string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindLatestImportantForm(JsonElement root)
    {
        var forms = root.GetProperty("filings").GetProperty("recent").GetProperty("form");

        for (var index = 0; index < forms.GetArrayLength(); index++)
        {
            var form = forms[index].GetString();

            if (form is "10-K" or "10-Q" or "8-K")
            {
                return form;
            }
        }

        return "Not available";
    }

    private static decimal GetDecimal(JsonElement element) =>
        element.TryGetDecimal(out var value)
            ? decimal.Round(value, 4)
            : decimal.Round((decimal)element.GetDouble(), 4);

    private static readonly IReadOnlyDictionary<string, CompanyProfileEnrichment> CompanyEnrichment =
        new Dictionary<string, CompanyProfileEnrichment>(StringComparer.OrdinalIgnoreCase)
        {
            ["AAPL"] = new("Technology", "Consumer Electronics", "NASDAQ"),
            ["MSFT"] = new("Technology", "Software", "NASDAQ"),
            ["NVDA"] = new("Technology", "Semiconductors", "NASDAQ"),
            ["TSLA"] = new("Consumer Cyclical", "Auto Manufacturers", "NASDAQ"),
            ["GOOG"] = new("Communication Services", "Internet Content & Information", "NASDAQ"),
            ["AMZN"] = new("Consumer Cyclical", "Internet Retail", "NASDAQ")
        };

    private sealed record CompanyTicker(string Ticker, string CikPadded, string Title);

    private sealed record CompanyProfileEnrichment(string Sector, string Industry, string Exchange);
}

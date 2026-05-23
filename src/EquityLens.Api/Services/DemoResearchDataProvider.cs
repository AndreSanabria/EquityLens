using EquityLens.Api.Configuration;
using EquityLens.Api.Models;
using EquityLens.Api.Utilities;
using Microsoft.Extensions.Options;

namespace EquityLens.Api.Services;

public class DemoResearchDataProvider(
    IOptions<ApiProviderOptions> options,
    IApiRequestLogService apiRequestLogService) : IResearchDataProvider
{
    private readonly HashSet<string> _supportedTickers = new(
        (options.Value.SupportedTickers.Count == 0 ? Catalog.Keys : options.Value.SupportedTickers)
            .Select(TickerNormalizer.Normalize),
        StringComparer.OrdinalIgnoreCase);

    private const string ProviderName = "DemoResearchProvider";

    string IResearchDataProvider.ProviderName => ProviderName;

    public IReadOnlyList<string> GetSupportedTickers() =>
        _supportedTickers.OrderBy(ticker => ticker).ToArray();

    public async Task<CompanyProfile> GetCompanyProfileAsync(string ticker, CancellationToken cancellationToken)
    {
        var normalized = TickerNormalizer.Normalize(ticker);

        try
        {
            var seed = GetSeed(normalized);
            var prices = GenerateHistoricalPrices(seed);
            var latest = prices[^1];
            var trailingYear = prices
                .Where(price => price.Date >= DateTime.UtcNow.Date.AddYears(-1))
                .ToList();

            var profile = new CompanyProfile
            {
                Ticker = normalized,
                CompanyName = seed.CompanyName,
                Sector = seed.Sector,
                Industry = seed.Industry,
                Exchange = seed.Exchange,
                Cik = seed.Cik,
                MarketCap = decimal.Round(seed.MarketCapBase * (latest.Close / seed.BasePrice), 0),
                CurrentPrice = latest.Close,
                FiftyTwoWeekHigh = decimal.Round(trailingYear.Max(price => price.High), 2),
                FiftyTwoWeekLow = decimal.Round(trailingYear.Min(price => price.Low), 2),
                LatestFilingForm = "10-Q",
                LastUpdated = DateTime.UtcNow
            };

            await apiRequestLogService.LogAsync(ProviderName, "company-profile", normalized, 200, true, null, cancellationToken);
            return profile;
        }
        catch (Exception ex) when (ex is TickerNotSupportedException or ArgumentException)
        {
            await apiRequestLogService.LogAsync(ProviderName, "company-profile", normalized, 404, false, ex.Message, cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<HistoricalPrice>> GetHistoricalPricesAsync(string ticker, CancellationToken cancellationToken)
    {
        var normalized = TickerNormalizer.Normalize(ticker);

        try
        {
            var prices = GenerateHistoricalPrices(GetSeed(normalized));
            await apiRequestLogService.LogAsync(ProviderName, "historical-prices", normalized, 200, true, null, cancellationToken);
            return prices;
        }
        catch (Exception ex) when (ex is TickerNotSupportedException or ArgumentException)
        {
            await apiRequestLogService.LogAsync(ProviderName, "historical-prices", normalized, 404, false, ex.Message, cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<FinancialFact>> GetFinancialFactsAsync(string ticker, CancellationToken cancellationToken)
    {
        var normalized = TickerNormalizer.Normalize(ticker);

        try
        {
            var facts = GenerateFinancialFacts(GetSeed(normalized));
            await apiRequestLogService.LogAsync(ProviderName, "financial-facts", normalized, 200, true, null, cancellationToken);
            return facts;
        }
        catch (Exception ex) when (ex is TickerNotSupportedException or ArgumentException)
        {
            await apiRequestLogService.LogAsync(ProviderName, "financial-facts", normalized, 404, false, ex.Message, cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<NewsArticle>> GetNewsAsync(string ticker, CancellationToken cancellationToken)
    {
        var normalized = TickerNormalizer.Normalize(ticker);

        try
        {
            var news = GenerateNews(GetSeed(normalized));
            await apiRequestLogService.LogAsync(ProviderName, "news", normalized, 200, true, null, cancellationToken);
            return news;
        }
        catch (Exception ex) when (ex is TickerNotSupportedException or ArgumentException)
        {
            await apiRequestLogService.LogAsync(ProviderName, "news", normalized, 404, false, ex.Message, cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<SecFiling>> GetRecentFilingsAsync(string ticker, CancellationToken cancellationToken)
    {
        var normalized = TickerNormalizer.Normalize(ticker);

        try
        {
            var filings = GenerateFilings(GetSeed(normalized));
            await apiRequestLogService.LogAsync(ProviderName, "filings", normalized, 200, true, null, cancellationToken);
            return filings;
        }
        catch (Exception ex) when (ex is TickerNotSupportedException or ArgumentException)
        {
            await apiRequestLogService.LogAsync(ProviderName, "filings", normalized, 404, false, ex.Message, cancellationToken);
            throw;
        }
    }

    private static readonly IReadOnlyDictionary<string, ResearchSeed> Catalog =
        new Dictionary<string, ResearchSeed>(StringComparer.OrdinalIgnoreCase)
        {
            ["AAPL"] = new("AAPL", "Apple Inc.", "Technology", "Consumer Electronics", "NASDAQ", "0000320193",
                192m, 2_900_000_000_000m, 0.00028m, 0.014m, 0.0016m, 58_000_000L, 383_000_000_000m, 0.055m, 0.255m, 72_000_000_000m, 110_000_000_000m, 0.42m, 1, "iPhone", "services", "wearables"),
            ["MSFT"] = new("MSFT", "Microsoft Corporation", "Technology", "Software", "NASDAQ", "0000789019",
                420m, 3_100_000_000_000m, 0.00039m, 0.013m, 0.0017m, 31_000_000L, 245_000_000_000m, 0.092m, 0.345m, 80_000_000_000m, 68_000_000_000m, 0.34m, 3, "Azure", "copilot", "enterprise cloud"),
            ["NVDA"] = new("NVDA", "NVIDIA Corporation", "Technology", "Semiconductors", "NASDAQ", "0001045810",
                122m, 2_950_000_000_000m, 0.00062m, 0.021m, 0.0024m, 46_000_000L, 61_000_000_000m, 0.24m, 0.39m, 26_000_000_000m, 14_000_000_000m, 0.28m, 5, "accelerator chips", "data centers", "compute platforms"),
            ["TSLA"] = new("TSLA", "Tesla, Inc.", "Consumer Cyclical", "Auto Manufacturers", "NASDAQ", "0001318605",
                178m, 640_000_000_000m, 0.00018m, 0.028m, 0.0032m, 119_000_000L, 102_000_000_000m, 0.07m, 0.095m, 31_000_000_000m, 15_000_000_000m, 0.38m, 9, "EV demand", "autonomy", "factory scale"),
            ["GOOG"] = new("GOOG", "Alphabet Inc.", "Communication Services", "Internet Content & Information", "NASDAQ", "0001652044",
                171m, 2_250_000_000_000m, 0.00034m, 0.017m, 0.0019m, 27_000_000L, 328_000_000_000m, 0.10m, 0.235m, 112_000_000_000m, 27_000_000_000m, 0.29m, 4, "search ads", "cloud", "automation platform"),
            ["AMZN"] = new("AMZN", "Amazon.com, Inc.", "Consumer Cyclical", "Internet Retail", "NASDAQ", "0001018724",
                184m, 2_050_000_000_000m, 0.00031m, 0.019m, 0.0021m, 52_000_000L, 590_000_000_000m, 0.085m, 0.062m, 79_000_000_000m, 74_000_000_000m, 0.36m, 7, "AWS", "fulfillment", "advertising")
        };

    private ResearchSeed GetSeed(string ticker)
    {
        if (!_supportedTickers.Contains(ticker) || !Catalog.TryGetValue(ticker, out var seed))
        {
            throw new TickerNotSupportedException(ticker);
        }

        return seed;
    }

    private static IReadOnlyList<HistoricalPrice> GenerateHistoricalPrices(ResearchSeed seed)
    {
        var startDate = DateTime.UtcNow.Date.AddYears(-5);
        var endDate = DateTime.UtcNow.Date;
        var random = new Random(seed.Ticker.GetHashCode(StringComparison.Ordinal));
        var prices = new List<HistoricalPrice>();
        var price = seed.BasePrice;
        var businessDayIndex = 0;

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                continue;
            }

            var seasonal = (decimal)Math.Sin((businessDayIndex + seed.SeasonOffset) / 18.0) * seed.CycleAmplitude;
            var noise = ((decimal)random.NextDouble() - 0.5m) * seed.DailyNoiseRange;
            var dailyReturn = seed.DailyDrift + seasonal + noise;
            var open = price;
            var nextClose = price * (1m + dailyReturn);
            price = nextClose < 5m ? 5m : nextClose;
            var close = decimal.Round(price, 2);
            var intradaySpread = Math.Abs(((decimal)random.NextDouble() - 0.5m) * seed.DailyNoiseRange * 2.8m) + Math.Abs(dailyReturn) * 0.3m;
            var highBase = open > close ? open : close;
            var lowBase = open < close ? open : close;
            var high = decimal.Round(highBase * (1m + intradaySpread), 2);
            var low = decimal.Round(lowBase * (1m - intradaySpread), 2);
            var volumeMultiplier = 0.9 + random.NextDouble() * 0.45 + (double)(Math.Abs(dailyReturn) * 12m);
            var volume = (long)(seed.BaseVolume * volumeMultiplier);

            prices.Add(new HistoricalPrice
            {
                Ticker = seed.Ticker,
                Date = date,
                Open = decimal.Round(open, 2),
                High = high,
                Low = low,
                Close = close,
                AdjustedClose = close,
                Volume = volume
            });

            businessDayIndex++;
        }

        return prices;
    }

    private static IReadOnlyList<FinancialFact> GenerateFinancialFacts(ResearchSeed seed)
    {
        var facts = new List<FinancialFact>();
        var latestFiscalYear = DateTime.UtcNow.Year - 1;

        for (var offset = 0; offset < 4; offset++)
        {
            var year = latestFiscalYear - 3 + offset;
            var revenueScale = Pow(1m + seed.RevenueGrowth, offset) * (1m + Oscillate(seed, offset, 0.055m));
            var revenue = decimal.Round(seed.RevenueBase * revenueScale, 0);
            var margin = seed.NetMargin + Oscillate(seed, offset + 2, 0.025m);
            var netIncome = decimal.Round(revenue * margin, 0);
            var cash = decimal.Round(seed.CashBase * Pow(1m + seed.RevenueGrowth * 0.45m, offset) * (1m + Oscillate(seed, offset + 1, 0.045m)), 0);
            var debt = decimal.Round(seed.DebtBase * Pow(1m + 0.03m + Oscillate(seed, offset + 3, 0.035m), offset), 0);
            var liabilities = decimal.Round(revenue * seed.LiabilityRatio * (1m + Oscillate(seed, offset + 4, 0.05m)), 0);
            var assets = decimal.Round(liabilities + cash + (revenue * 0.42m), 0);
            var filedAt = new DateTime(year + 1, 2, 15, 0, 0, 0, DateTimeKind.Utc);

            facts.AddRange([
                CreateFact(seed.Ticker, "Revenue", year, revenue, filedAt),
                CreateFact(seed.Ticker, "NetIncome", year, netIncome, filedAt),
                CreateFact(seed.Ticker, "Assets", year, assets, filedAt),
                CreateFact(seed.Ticker, "Liabilities", year, liabilities, filedAt),
                CreateFact(seed.Ticker, "Cash", year, cash, filedAt),
                CreateFact(seed.Ticker, "Debt", year, debt, filedAt)
            ]);
        }

        return facts;
    }

    private static IReadOnlyList<NewsArticle> GenerateNews(ResearchSeed seed)
    {
        var titles = new (string Category, string Source, int DaysAgo, string Title, string SearchUrl)[]
        {
            ("Earnings", "Reuters", 2, $"{seed.CompanyName} updates revenue guidance as {seed.ThemeOne} demand stays active", BuildSearchUrl("Reuters", seed.CompanyName, seed.ThemeOne)),
            ("Legal", "Bloomberg", 6, $"{seed.CompanyName} faces new regulatory questions around {seed.ThemeTwo} strategy", BuildSearchUrl("Bloomberg", seed.CompanyName, seed.ThemeTwo)),
            ("Product", "CNBC", 11, $"{seed.CompanyName} expands investment in {seed.ThemeThree} platform roadmap", BuildSearchUrl("CNBC", seed.CompanyName, seed.ThemeThree)),
            ("Leadership", "The Wall Street Journal", 18, $"{seed.CompanyName} leadership emphasizes cost discipline ahead of the next filing", BuildSearchUrl("The Wall Street Journal", seed.CompanyName, "leadership cost discipline")),
            ("Debt", "MarketWatch", 27, $"{seed.CompanyName} debt outlook remains in focus as investors reassess growth durability", BuildSearchUrl("MarketWatch", seed.CompanyName, "debt outlook"))
        };

        return titles.Select((entry, index) => new NewsArticle
        {
            Id = index + 1,
            Ticker = seed.Ticker,
            Category = entry.Category,
            Source = entry.Source,
            PublishedAt = DateTime.UtcNow.Date.AddDays(-entry.DaysAgo),
            Title = entry.Title,
            Url = entry.SearchUrl,
            IsDirectArticleUrl = false
        }).ToList();
    }

    private static string BuildSearchUrl(string source, string companyName, string topic)
    {
        var query = Uri.EscapeDataString($"{companyName} {topic}");

        return source switch
        {
            "Reuters" => $"https://www.reuters.com/site-search/?query={query}",
            "Bloomberg" => $"https://www.bloomberg.com/search?query={query}",
            "CNBC" => $"https://www.cnbc.com/search/?query={query}",
            "The Wall Street Journal" => $"https://www.wsj.com/search?query={query}",
            "MarketWatch" => $"https://www.marketwatch.com/search?q={query}",
            _ => $"https://news.google.com/search?q={query}"
        };
    }

    private static IReadOnlyList<SecFiling> GenerateFilings(ResearchSeed seed)
    {
        var browseUrl = $"https://www.sec.gov/edgar/browse/?CIK={seed.Cik}&owner=exclude";

        return
        [
            new SecFiling
            {
                Ticker = seed.Ticker,
                FormType = "10-Q",
                FiledAt = DateTime.UtcNow.Date.AddDays(-38),
                Description = "Quarterly business and financial update",
                FilingUrl = browseUrl
            },
            new SecFiling
            {
                Ticker = seed.Ticker,
                FormType = "8-K",
                FiledAt = DateTime.UtcNow.Date.AddDays(-12),
                Description = "Current report for a material corporate event",
                FilingUrl = browseUrl
            },
            new SecFiling
            {
                Ticker = seed.Ticker,
                FormType = "10-K",
                FiledAt = DateTime.UtcNow.Date.AddDays(-88),
                Description = "Annual report covering strategy, risks, and full-year results",
                FilingUrl = browseUrl
            }
        ];
    }

    private static FinancialFact CreateFact(string ticker, string metricName, int year, decimal value, DateTime filedAt) =>
        new()
        {
            Ticker = ticker,
            MetricName = metricName,
            FiscalYear = year,
            FiscalPeriod = "FY",
            Value = value,
            FiledAt = filedAt
        };

    private static decimal Pow(decimal value, int power) =>
        (decimal)Math.Pow((double)value, power);

    private static decimal Oscillate(ResearchSeed seed, int offset, decimal amplitude) =>
        (decimal)Math.Sin((offset + seed.SeasonOffset) * 0.9d) * amplitude;

    private sealed record ResearchSeed(
        string Ticker,
        string CompanyName,
        string Sector,
        string Industry,
        string Exchange,
        string Cik,
        decimal BasePrice,
        decimal MarketCapBase,
        decimal DailyDrift,
        decimal DailyNoiseRange,
        decimal CycleAmplitude,
        long BaseVolume,
        decimal RevenueBase,
        decimal RevenueGrowth,
        decimal NetMargin,
        decimal CashBase,
        decimal DebtBase,
        decimal LiabilityRatio,
        int SeasonOffset,
        string ThemeOne,
        string ThemeTwo,
        string ThemeThree);
}

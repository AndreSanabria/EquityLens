using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EquityLens.Api.Data;
using EquityLens.Api.DTOs;
using EquityLens.Api.Models;
using EquityLens.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EquityLens.Api.Tests;

public class ApiIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task SupportedTickers_ReturnsConfiguredDemoTickers()
    {
        using var factory = new EquityLensApiFactory();
        using var client = factory.CreateClient();

        var tickers = await client.GetFromJsonAsync<IReadOnlyList<string>>("/api/stocks/supported", JsonOptions);

        Assert.NotNull(tickers);
        Assert.Contains("MSFT", tickers);
        Assert.Contains("NVDA", tickers);
    }

    [Fact]
    public async Task Dashboard_WithUnsupportedTicker_ReturnsNotFound()
    {
        using var factory = new EquityLensApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/stocks/ZZZZ/dashboard");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("ZZZZ", body);
    }

    [Fact]
    public async Task Watchlist_CanCreateUpdateAndDeleteItem()
    {
        using var factory = new EquityLensApiFactory();
        using var client = factory.CreateClient();

        using var createResponse = await client.PostAsJsonAsync(
            "/api/watchlist",
            new CreateWatchlistItemRequest("msft", "Watching after earnings"),
            JsonOptions);
        var created = await ReadJsonAsync<WatchlistItemDto>(createResponse);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal("MSFT", created.Ticker);
        Assert.Equal("Watching after earnings", created.Notes);
        Assert.NotNull(created.LastKnownRiskScore);

        using var updateResponse = await client.PutAsJsonAsync(
            "/api/watchlist/MSFT/notes",
            new UpdateWatchlistNotesRequest("Updated note"),
            JsonOptions);
        var updated = await ReadJsonAsync<WatchlistItemDto>(updateResponse);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal("Updated note", updated.Notes);

        var items = await client.GetFromJsonAsync<IReadOnlyList<WatchlistItemDto>>("/api/watchlist", JsonOptions);
        Assert.Single(items!);

        using var deleteResponse = await client.DeleteAsync("/api/watchlist/MSFT");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var remaining = await client.GetFromJsonAsync<IReadOnlyList<WatchlistItemDto>>("/api/watchlist", JsonOptions);
        Assert.Empty(remaining!);
    }

    [Fact]
    public async Task Snapshots_CanCreateAndReadResearchHistory()
    {
        using var factory = new EquityLensApiFactory();
        using var client = factory.CreateClient();

        using var createResponse = await client.PostAsync("/api/stocks/MSFT/snapshot", content: null);
        var created = await ReadJsonAsync<ResearchSnapshotDto>(createResponse);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal("MSFT", created.Ticker);
        Assert.NotEqual(0, created.RiskScore);
        Assert.False(string.IsNullOrWhiteSpace(created.Summary));

        var snapshots = await client.GetFromJsonAsync<IReadOnlyList<ResearchSnapshotDto>>("/api/stocks/MSFT/snapshots", JsonOptions);
        Assert.Single(snapshots!);
        Assert.Equal(created.Id, snapshots![0].Id);
    }

    [Fact]
    public async Task Dashboard_WhenProviderFails_ReturnsServiceUnavailable()
    {
        using var factory = new EquityLensApiFactory(services =>
        {
            services.RemoveAll<IResearchDataProvider>();
            services.AddScoped<IResearchDataProvider, FailingResearchDataProvider>();
        });
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/stocks/MSFT/dashboard");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("external data provider", body, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        Assert.NotNull(result);
        return result;
    }

    private sealed class EquityLensApiFactory(Action<IServiceCollection>? configureServices = null) : WebApplicationFactory<Program>
    {
        private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"equitylens-tests-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:EquityLens"] = $"Data Source={_databasePath}",
                    ["ApiProviderOptions:Mode"] = "Demo",
                    ["ApiProviderOptions:SecUserAgent"] = "EquityLens Integration Tests contact@example.com"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                configureServices?.Invoke(services);
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            GC.Collect();
            GC.WaitForPendingFinalizers();

            foreach (var path in new[] { _databasePath, $"{_databasePath}-wal", $"{_databasePath}-shm" })
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch (IOException)
                {
                    // SQLite can hold temp files briefly on Windows after the test host shuts down.
                }
            }
        }
    }

    private sealed class FailingResearchDataProvider : IResearchDataProvider
    {
        public string ProviderName => "Failing test provider";

        public Task<CompanyProfile> GetCompanyProfileAsync(string ticker, CancellationToken cancellationToken) =>
            throw BuildException();

        public Task<IReadOnlyList<HistoricalPrice>> GetHistoricalPricesAsync(string ticker, CancellationToken cancellationToken) =>
            throw BuildException();

        public Task<IReadOnlyList<FinancialFact>> GetFinancialFactsAsync(string ticker, CancellationToken cancellationToken) =>
            throw BuildException();

        public Task<IReadOnlyList<NewsArticle>> GetNewsAsync(string ticker, CancellationToken cancellationToken) =>
            throw BuildException();

        public Task<IReadOnlyList<SecFiling>> GetRecentFilingsAsync(string ticker, CancellationToken cancellationToken) =>
            throw BuildException();

        public IReadOnlyList<string> GetSupportedTickers() => ["MSFT"];

        private static HttpRequestException BuildException() =>
            new("Simulated provider outage", inner: null, statusCode: HttpStatusCode.ServiceUnavailable);
    }
}

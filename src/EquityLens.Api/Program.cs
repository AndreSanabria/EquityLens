using EquityLens.Api.Configuration;
using EquityLens.Api.Data;
using EquityLens.Api.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ApiProviderOptions>(
    builder.Configuration.GetSection(ApiProviderOptions.SectionName));

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddDbContext<EquityLensDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("EquityLens")));

builder.Services.AddMemoryCache();
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalFrontend", policy =>
        policy
            .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IApiRequestLogService, ApiRequestLogService>();
builder.Services.AddScoped<DemoResearchDataProvider>();
builder.Services.AddHttpClient<LiveResearchDataProvider>();
builder.Services.AddScoped<IResearchDataProvider>(serviceProvider =>
{
    var providerOptions = serviceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiProviderOptions>>()
        .Value;

    return providerOptions.Mode.Equals("Demo", StringComparison.OrdinalIgnoreCase)
        ? serviceProvider.GetRequiredService<DemoResearchDataProvider>()
        : serviceProvider.GetRequiredService<LiveResearchDataProvider>();
});
builder.Services.AddSingleton<IPerformanceService, PerformanceService>();
builder.Services.AddSingleton<INewsRankingService, NewsRankingService>();
builder.Services.AddSingleton<IFinancialDirectionService, FinancialDirectionService>();
builder.Services.AddSingleton<IRiskAnalysisService, RiskAnalysisService>();
builder.Services.AddSingleton<IResearchSummaryService, ResearchSummaryService>();
builder.Services.AddScoped<IStockDashboardService, StockDashboardService>();
builder.Services.AddScoped<IResearchSnapshotService, ResearchSnapshotService>();
builder.Services.AddScoped<IWatchlistService, WatchlistService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<EquityLensDbContext>();
    dbContext.Database.EnsureCreated();
}

app.UseExceptionHandler("/error");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("LocalFrontend");
app.UseAuthorization();

app.MapGet("/", (IResearchDataProvider provider) => Results.Ok(new
{
    Name = "EquityLens API",
    Description = "A C#/.NET stock research dashboard backend with transparent performance, risk, news, and filing analysis.",
    Provider = provider.ProviderName,
    Docs = "/swagger"
}));

app.Map("/error", (HttpContext context) =>
{
    var exception = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;

    if (exception is HttpRequestException httpException)
    {
        var statusCode = httpException.StatusCode == HttpStatusCode.TooManyRequests
            ? StatusCodes.Status429TooManyRequests
            : StatusCodes.Status503ServiceUnavailable;
        var detail = statusCode == StatusCodes.Status429TooManyRequests
            ? "The external data provider is temporarily rate-limiting requests. Wait a minute, then try again."
            : "An external data provider failed while EquityLens was building this dashboard.";

        return Results.Problem(
            title: "External data provider error",
            detail: detail,
            statusCode: statusCode);
    }

    return Results.Problem("An unexpected error occurred while processing the EquityLens request.");
});
app.MapControllers();

app.Run();

public partial class Program;

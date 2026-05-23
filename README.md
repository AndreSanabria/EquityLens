# EquityLens

[![CI](https://github.com/AndreSanabria/EquityLens/actions/workflows/ci.yml/badge.svg)](https://github.com/AndreSanabria/EquityLens/actions/workflows/ci.yml)

EquityLens is a C#/.NET stock research dashboard that combines historical price behavior, SEC filing data, financial trend signals, and recent market news into a structured company research view.

The application is designed around explainable research organization. It does not produce trade recommendations or price forecasts. The dashboard surfaces calculations, source context, data freshness, and methodology so each result can be inspected.

## System Overview

EquityLens uses an ASP.NET Core Web API as the primary application layer and a React frontend as the presentation layer. The backend is responsible for ticker validation, external data retrieval, normalization, risk scoring, financial direction analysis, news ranking, persistence, logging, and API responses. The frontend renders the prepared dashboard sections without duplicating calculation logic.

```text
Ticker request
    |
    v
ASP.NET Core API
    |
    |-- Market data service
    |-- SEC filing service
    |-- Financial direction service
    |-- Risk analysis service
    |-- News ranking service
    |-- Watchlist and snapshot services
    |
    v
SQLite persistence + structured dashboard response
    |
    v
React dashboard
```

## Core Features

- Ticker research dashboard with company profile, price history, risk analysis, financial direction, news, and SEC filings
- Historical price chart with selectable ranges: `1D`, `3D`, `1W`, `2W`, `4W`, `3M`, `6M`, `1Y`, and `5Y`
- Return calculations for `1M`, `3M`, `6M`, `1Y`, and `5Y`
- Annualized volatility and max drawdown calculations
- Weighted risk score with component-level explanations
- SEC EDGAR filing panel for recent `10-K`, `10-Q`, and `8-K` filings
- SEC company facts processing for revenue, net income, assets, liabilities, cash, and debt direction
- Rule-based news ranking with categories, recency scoring, and direct article links when available
- Watchlist with ticker notes and latest known risk score
- Research snapshots for saving prior dashboard results
- Data freshness panel showing provider, latest prices, latest financial filing date, news update date, and filing update date
- Swagger/OpenAPI documentation for backend endpoints

## Technical Stack

- C# / ASP.NET Core Web API
- Entity Framework Core
- SQLite
- Serilog
- Swagger / OpenAPI
- React
- TypeScript
- Vite
- Recharts
- xUnit
- Docker Compose
- SEC EDGAR APIs
- Yahoo Finance chart/RSS endpoints for local market-data retrieval

## Repository Structure

```text
EquityLens.sln
|-- src
|   |-- EquityLens.Api
|   |   |-- Configuration
|   |   |-- Controllers
|   |   |-- Data
|   |   |-- DTOs
|   |   |-- Models
|   |   |-- Services
|   |   `-- Utilities
|   `-- EquityLens.Web
|       |-- src
|       |-- public
|       `-- package.json
|-- tests
|   `-- EquityLens.Api.Tests
|-- docs
|   |-- methodology.md
|   `-- screenshots
`-- docker-compose.yml
```

## Backend Design

The API is organized around focused services rather than placing business logic in controllers.

- `MarketDataService` retrieves and normalizes historical price data.
- `PerformanceService` calculates period returns and chart-ready price points.
- `RiskAnalysisService` calculates volatility, max drawdown, weighted component scores, and risk labels.
- `SecFilingService` resolves SEC identifiers, retrieves filings, and extracts company facts.
- `FinancialDirectionService` compares recent financial metrics and labels direction changes.
- `NewsService` retrieves recent company-related headlines.
- `NewsRankingService` categorizes headlines and calculates relevance/news-risk scores.
- `DashboardService` combines all sections into a single structured dashboard response.
- `WatchlistService` and `ResearchSnapshotService` persist saved tickers, notes, and dashboard snapshots.

Controllers expose concise REST endpoints and delegate calculation, retrieval, and persistence work to the service layer.

## Data Flow

1. A ticker is submitted to `GET /api/stocks/{ticker}/dashboard`.
2. The API validates and normalizes the ticker symbol.
3. Market data is fetched, cached, and converted into chart points and return metrics.
4. SEC data is retrieved for filings and company financial facts.
5. Financial facts are normalized into year-over-year direction labels.
6. News headlines are categorized and ranked by relevance, recency, and risk keywords.
7. Risk scoring combines price behavior, financial trend stability, debt pressure, and news risk.
8. A single dashboard DTO is returned to the React frontend.

## Risk Methodology

EquityLens uses a transparent weighted formula:

```text
Final Risk Score =
VolatilityScore * 0.30
+ MaxDrawdownScore * 0.25
+ RevenueInstabilityScore * 0.15
+ EarningsInstabilityScore * 0.15
+ DebtPressureScore * 0.10
+ NewsRiskScore * 0.05
```

Score labels:

- `0-25`: Low risk
- `26-50`: Moderate risk
- `51-75`: High risk
- `76-100`: Very high risk

Component details:

- Volatility uses the annualized standard deviation of daily returns.
- Max drawdown measures the largest peak-to-trough decline in the available price series.
- Revenue instability penalizes unstable or declining annual revenue trends.
- Earnings instability penalizes unstable or declining annual net income trends.
- Debt pressure uses debt-to-cash balance and liability growth as balance-sheet pressure signals.
- News risk uses headline category, recency, and source quality signals.

Additional methodology documentation is available in [docs/methodology.md](docs/methodology.md).

## Data Sources

EquityLens supports two provider modes:

- `Live`: retrieves price history from Yahoo Finance chart data, recent headlines from Yahoo Finance RSS, and company facts/filings from SEC EDGAR.
- `Demo`: generates deterministic sample data for supported example tickers so the application can run without network access.

The default provider mode is `Live`.

SEC requests require a descriptive contact string through `ApiProviderOptions__SecUserAgent`. For production market data, the Yahoo Finance adapter should be replaced with a licensed provider such as Alpha Vantage, Twelve Data, Finnhub, Polygon, or another contracted data source.

## API Surface

- `GET /api/stocks/supported`
- `GET /api/stocks/{ticker}/dashboard`
- `GET /api/stocks/{ticker}/performance`
- `GET /api/stocks/{ticker}/risk`
- `GET /api/stocks/{ticker}/news`
- `GET /api/stocks/{ticker}/filings`
- `POST /api/stocks/{ticker}/snapshot`
- `GET /api/stocks/{ticker}/snapshots`
- `GET /api/watchlist`
- `POST /api/watchlist`
- `PUT /api/watchlist/{ticker}/notes`
- `DELETE /api/watchlist/{ticker}`
- `GET /api/methodology`

Swagger is available at `http://127.0.0.1:5077/swagger` during local API execution.

## Screenshots

### Dashboard

![EquityLens dashboard showing company summary, key metrics, data freshness, and price history](docs/screenshots/dashboard.png)

### Risk Breakdown

![Risk breakdown panel showing weighted component scores and explanations](docs/screenshots/risk-breakdown.png)

### Research Sections

![Risk breakdown, financial direction, relevant news, and SEC filings shown together](docs/screenshots/analysis-news-filings.png)

### Returns

![Returns panel showing period returns, annual volatility, and max drawdown](docs/screenshots/returns.png)

### Financial Direction

![Financial direction panel showing SEC-derived year-over-year metric direction](docs/screenshots/financial-direction.png)

### Methodology

![Methodology panel explaining how EquityLens calculates the risk score](docs/screenshots/methodology.png)

## Local Execution

Requirements:

- .NET 8 SDK
- Node.js 20+
- PowerShell on Windows

API:

```powershell
dotnet restore
$env:ApiProviderOptions__SecUserAgent="EquityLens contact@example.com"
dotnet run --project .\src\EquityLens.Api --urls http://127.0.0.1:5077
```

Web app:

```powershell
cd .\src\EquityLens.Web
npm.cmd install
npm.cmd run build
npm.cmd run preview -- --host 127.0.0.1 --port 5173
```

Application URL:

```text
http://127.0.0.1:5173
```

Docker:

```powershell
docker compose up --build
```

## Verification

```powershell
dotnet build .\EquityLens.sln
dotnet test .\EquityLens.sln
cd .\src\EquityLens.Web
npm.cmd run build
```

## Known Limitations

- The application organizes research data and does not recommend buying, selling, or holding a security.
- Yahoo Finance chart/RSS endpoints are used for local market-data retrieval and should not be treated as a production data license.
- Market cap is shown as `N/A` in live mode unless a licensed quote/fundamentals provider is added.
- SEC company facts can lag company events and depend on consistent XBRL tags across filings.
- News relevance is rule-based and explainable, but it can still miss context that a human analyst would catch.

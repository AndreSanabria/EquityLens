import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import {
  AlertTriangle,
  BarChart3,
  CalendarClock,
  ExternalLink,
  FileText,
  Info,
  Newspaper,
  RefreshCw,
  Search,
  ShieldAlert,
  Star,
  Trash2,
  TrendingDown,
  TrendingUp,
} from 'lucide-react'
import {
  createSnapshot,
  deleteWatchlistItem,
  getDashboard,
  getMethodology,
  getSnapshots,
  getSupportedTickers,
  getWatchlist,
  saveWatchlistItem,
} from './api.ts'
import {
  compactNumber,
  currency,
  formatDate,
  formatPercent,
} from './format.ts'
import {
  chartRanges,
  DEFAULT_TICKER,
  riskComponents,
  termDefinitions,
  type ChartRangeKey,
} from './dashboardConfig.ts'
import {
  DashboardHeader,
  FinancialTable,
  FreshnessItem,
  InfoTooltip,
  MetricTile,
  PriceChart,
} from './dashboardComponents.tsx'
import type {
  Methodology,
  PricePoint,
  RiskComponentDetail,
  ResearchSnapshot,
  StockDashboard,
  WatchlistItem,
} from './types.ts'
import './App.css'

function App() {
  const initialTicker = readInitialTicker()
  const [tickerInput, setTickerInput] = useState(initialTicker)
  const [activeTicker, setActiveTicker] = useState(initialTicker)
  const [activeRange, setActiveRange] = useState<ChartRangeKey>(readInitialRange())
  const [dashboard, setDashboard] = useState<StockDashboard | null>(null)
  const [watchlist, setWatchlist] = useState<WatchlistItem[]>([])
  const [snapshots, setSnapshots] = useState<ResearchSnapshot[]>([])
  const [supportedTickers, setSupportedTickers] = useState<string[]>([])
  const [methodology, setMethodology] = useState<Methodology | null>(null)
  const [notes, setNotes] = useState('')
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    void refreshReferenceData()
  }, [])

  useEffect(() => {
    void loadDashboard(activeTicker)
  }, [activeTicker])

  async function refreshReferenceData() {
    try {
      const [tickers, items, methodologyResult] = await Promise.all([
        getSupportedTickers(),
        getWatchlist(),
        getMethodology(),
      ])
      setSupportedTickers(tickers)
      setWatchlist(items)
      setMethodology(methodologyResult)
    } catch (requestError) {
      setError(getErrorMessage(requestError))
    }
  }

  async function loadDashboard(ticker: string) {
    setIsLoading(true)
    setError(null)

    try {
      const [nextDashboard, nextSnapshots] = await Promise.all([
        getDashboard(ticker),
        getSnapshots(ticker),
      ])
      setDashboard(nextDashboard)
      setSnapshots(nextSnapshots)
      setTickerInput(nextDashboard.ticker)
    } catch (requestError) {
      setError(getErrorMessage(requestError))
    } finally {
      setIsLoading(false)
    }
  }

  function handleSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const ticker = tickerInput.trim().toUpperCase()

    if (ticker.length > 0) {
      updateActiveTicker(ticker)
    }
  }

  function updateActiveTicker(ticker: string) {
    setActiveTicker(ticker)
    updateUrlParam('ticker', ticker)
  }

  function updateActiveRange(range: ChartRangeKey) {
    setActiveRange(range)
    updateUrlParam('range', range)
  }

  async function handleSaveWatchlist() {
    if (!dashboard) {
      return
    }

    setIsSaving(true)
    setError(null)

    try {
      await saveWatchlistItem(dashboard.ticker, notes)
      const items = await getWatchlist()
      setWatchlist(items)
      setNotes('')
    } catch (requestError) {
      setError(getErrorMessage(requestError))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleCreateSnapshot() {
    if (!dashboard) {
      return
    }

    setIsSaving(true)
    setError(null)

    try {
      await createSnapshot(dashboard.ticker)
      setSnapshots(await getSnapshots(dashboard.ticker))
    } catch (requestError) {
      setError(getErrorMessage(requestError))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleDeleteWatchlistItem(ticker: string) {
    setError(null)

    try {
      await deleteWatchlistItem(ticker)
      setWatchlist(await getWatchlist())
    } catch (requestError) {
      setError(getErrorMessage(requestError))
    }
  }

  const chartPoints = useMemo(() => {
    if (!dashboard) {
      return []
    }

    return compressChartPoints(filterChartPoints(dashboard.performance.chartPoints, activeRange))
  }, [activeRange, dashboard])

  const watchlistHasTicker = dashboard
    ? watchlist.some((item) => item.ticker === dashboard.ticker)
    : false

  return (
    <main className="app-shell">
      <aside className="sidebar" aria-label="EquityLens navigation">
        <div className="brand-lockup">
          <span className="brand-mark">EL</span>
          <div>
            <strong>EquityLens</strong>
            <span>Research dashboard</span>
          </div>
        </div>

        <form className="search-panel" onSubmit={handleSearch}>
          <label htmlFor="ticker-search">Ticker</label>
          <div className="search-row">
            <input
              id="ticker-search"
              value={tickerInput}
              onChange={(event) => setTickerInput(event.target.value)}
              placeholder="MSFT"
              maxLength={10}
            />
            <button type="submit" title="Load ticker">
              <Search size={18} />
            </button>
          </div>
        </form>

        <section className="ticker-rail" aria-label="Supported tickers">
          {supportedTickers.map((ticker) => (
            <button
              className={ticker === activeTicker ? 'active' : ''}
              key={ticker}
              onClick={() => updateActiveTicker(ticker)}
              type="button"
            >
              {ticker}
            </button>
          ))}
        </section>

        <section className="watchlist-panel" aria-label="Watchlist">
          <div className="section-title compact">
            <Star size={17} />
            <h2>Watchlist</h2>
          </div>
          {watchlist.length === 0 ? (
            <p className="muted">No saved tickers yet.</p>
          ) : (
            <div className="watchlist-items">
              {watchlist.map((item) => (
                <div className="watchlist-item" key={item.id}>
                  <button
                    className="watchlist-ticker"
                    onClick={() => updateActiveTicker(item.ticker)}
                    type="button"
                  >
                    {item.ticker}
                    {item.lastKnownRiskScore !== undefined ? (
                      <span>{item.lastKnownRiskScore}</span>
                    ) : null}
                  </button>
                  <button
                    className="icon-button"
                    onClick={() => handleDeleteWatchlistItem(item.ticker)}
                    title={`Remove ${item.ticker}`}
                    type="button"
                  >
                    <Trash2 size={15} />
                  </button>
                </div>
              ))}
            </div>
          )}
        </section>
      </aside>

      <section className="workspace">
        {error ? (
          <div className="status-banner error">
            <AlertTriangle size={18} />
            <span>{error}</span>
          </div>
        ) : null}

        {isLoading || !dashboard ? (
          <div className="loading-panel">
            <RefreshCw size={24} />
            <span>Loading research view...</span>
          </div>
        ) : (
          <>
            <DashboardHeader
              dashboard={dashboard}
              isSaving={isSaving}
              notes={notes}
              onCreateSnapshot={handleCreateSnapshot}
              onNotesChange={setNotes}
              onSaveWatchlist={handleSaveWatchlist}
              watchlistHasTicker={watchlistHasTicker}
            />

            <section className="summary-band">
              <div>
                <span className="eyebrow">Research summary</span>
                <p>{dashboard.narrativeSummary}</p>
              </div>
              <div className={`risk-badge ${riskClass(dashboard.riskAnalysis.finalScore)}`}>
                <ShieldAlert size={19} />
                <span>
                  {dashboard.riskAnalysis.riskLevel}
                  <InfoTooltip text={termDefinitions['Risk Breakdown']} />
                </span>
                <strong>{dashboard.riskAnalysis.finalScore}</strong>
              </div>
            </section>

            <section className="metric-strip" aria-label="Key metrics">
              <MetricTile
                icon={<BarChart3 size={18} />}
                label="Current price"
                info={termDefinitions['Current price']}
                value={currency.format(dashboard.performance.currentPrice)}
              />
              <MetricTile
                icon={<TrendingUp size={18} />}
                label="52-week high"
                info={termDefinitions['52-week high']}
                value={currency.format(dashboard.companyProfile.fiftyTwoWeekHigh)}
              />
              <MetricTile
                icon={<TrendingDown size={18} />}
                label="52-week low"
                info={termDefinitions['52-week low']}
                value={currency.format(dashboard.companyProfile.fiftyTwoWeekLow)}
              />
              <MetricTile
                icon={<CalendarClock size={18} />}
                label="Market cap"
                info={termDefinitions['Market cap']}
                value={formatMarketCap(dashboard.companyProfile.marketCap)}
              />
            </section>

            <section className="freshness-band">
              <div className="section-title compact">
                <CalendarClock size={17} />
                <h2>Data Freshness</h2>
                <InfoTooltip text={termDefinitions['Data Freshness']} />
              </div>
              <div className="freshness-grid">
                <FreshnessItem label="Provider" value={dashboard.dataFreshness.providerMode} />
                <FreshnessItem label="Prices through" value={formatOptionalDate(dashboard.dataFreshness.priceDataThrough)} />
                <FreshnessItem label="Financials filed" value={formatOptionalDate(dashboard.dataFreshness.financialDataFiledAt)} />
                <FreshnessItem label="News updated" value={formatOptionalDate(dashboard.dataFreshness.newsUpdatedAt)} />
                <FreshnessItem label="Filings updated" value={formatOptionalDate(dashboard.dataFreshness.filingsUpdatedAt)} />
              </div>
              <ul>
                {dashboard.dataFreshness.limitations.map((limitation) => (
                  <li key={limitation}>{limitation}</li>
                ))}
              </ul>
            </section>

            <section className="dashboard-grid">
              <section className="panel chart-panel">
                <div className="section-title">
                  <BarChart3 size={18} />
                  <h2>Price History</h2>
                  <InfoTooltip text={termDefinitions['Price History']} />
                </div>
                <div className="range-controls" aria-label="Price history time range">
                  {chartRanges.map((range) => (
                    <button
                      className={range.key === activeRange ? 'active' : ''}
                      key={range.key}
                      onClick={() => updateActiveRange(range.key)}
                      title={range.description}
                      type="button"
                    >
                      {range.label}
                    </button>
                  ))}
                </div>
                <div className="chart-frame">
                  <PriceChart points={chartPoints} />
                </div>
              </section>

              <section className="panel returns-panel">
                <div className="section-title">
                  <TrendingUp size={18} />
                  <h2>Returns</h2>
                  <InfoTooltip text={termDefinitions.Returns} />
                </div>
                <div className="return-list">
                  {dashboard.performance.returns.map((metric) => (
                    <div className="return-row" key={metric.period}>
                      <span>{metric.period}</span>
                      <strong className={metric.percentReturn >= 0 ? 'positive' : 'negative'}>
                        {formatPercent(metric.percentReturn)}
                      </strong>
                    </div>
                  ))}
                </div>
                <div className="risk-stats">
                  <span>
                    Annual volatility
                    <InfoTooltip text={termDefinitions['Annual volatility']} />
                  </span>
                  <strong>{formatPercent(dashboard.performance.annualizedVolatility)}</strong>
                  <span>
                    Max drawdown
                    <InfoTooltip text={termDefinitions['Max drawdown']} />
                  </span>
                  <strong>{formatPercent(-dashboard.performance.maxDrawdown)}</strong>
                </div>
              </section>

              <section className="panel risk-panel">
                <div className="section-title">
                  <ShieldAlert size={18} />
                  <h2>Risk Breakdown</h2>
                  <InfoTooltip text={termDefinitions['Risk Breakdown']} />
                </div>
                <p className="methodology-note">
                  Final score = volatility 30%, max drawdown 25%, revenue instability 15%,
                  earnings instability 15%, debt pressure 10%, and news risk 5%.
                </p>
                <div className="risk-meter">
                  <span style={{ width: `${dashboard.riskAnalysis.finalScore}%` }} />
                </div>
                <div className="component-list">
                  {riskComponents.map(({ label, key, weight, description }) => {
                    const detail = findRiskDetail(dashboard.riskAnalysis.components, label)
                    const score = detail?.score ?? dashboard.riskAnalysis[key]

                    return (
                      <div className="component-card" key={key}>
                        <div className="component-row">
                          <span>
                            {label}
                            <InfoTooltip text={`${detail?.explanation ?? description} Weight: ${formatWeight(detail?.weight, weight)}.`} />
                          </span>
                          <div>
                            <span style={{ width: `${score}%` }} />
                          </div>
                          <strong>{score}</strong>
                        </div>
                        {detail ? (
                          <p className="component-explanation">
                            <b>{detail.metricValue}</b>{' '}
                            {detail.explanation}
                          </p>
                        ) : null}
                      </div>
                    )
                  })}
                </div>
                <div className="driver-list">
                  {dashboard.riskAnalysis.mainDrivers.map((driver) => (
                    <span key={driver}>{driver}</span>
                  ))}
                </div>
              </section>

              <section className="panel financial-panel">
                <div className="section-title">
                  <FileText size={18} />
                  <h2>Financial Direction</h2>
                  <InfoTooltip text={termDefinitions['Financial Direction']} />
                </div>
                <p className="direction-line">{dashboard.financialDirection.overallDirection}</p>
                <FinancialTable metrics={dashboard.financialDirection.metrics} />
              </section>

              <section className="panel news-panel">
                <div className="section-title">
                  <Newspaper size={18} />
                  <h2>Relevant News</h2>
                  <InfoTooltip text={termDefinitions['Relevant News']} />
                </div>
                <div className="news-list">
                  {dashboard.relevantNews.slice(0, 5).map((item) => (
                    <a href={item.url} key={item.title} rel="noreferrer" target="_blank">
                      <span>{item.category}</span>
                      <strong>{item.title}</strong>
                      <small>
                        {item.source} - {formatDate(item.publishedAt)} - Relevance {item.relevanceScore}
                        <InfoTooltip text={termDefinitions['Relevance score']} />
                      </small>
                      <em className={item.isDirectArticleUrl ? 'link-kind direct' : 'link-kind demo'}>
                        <ExternalLink size={13} />
                        {item.isDirectArticleUrl ? 'Direct article' : 'Demo source search'}
                      </em>
                    </a>
                  ))}
                </div>
              </section>

              <section className="panel filings-panel">
                <div className="section-title">
                  <FileText size={18} />
                  <h2>SEC Filings</h2>
                  <InfoTooltip text={termDefinitions['SEC Filings']} />
                </div>
                <div className="filing-list">
                  {dashboard.latestFilings.map((filing) => (
                    <a href={filing.filingUrl} key={`${filing.formType}-${filing.filedAt}`} rel="noreferrer" target="_blank">
                      <strong>
                        {filing.formType}
                        <InfoTooltip text={termDefinitions[filing.formType] ?? termDefinitions['SEC Filings']} />
                      </strong>
                      <span>{filing.description}</span>
                      <small>{formatDate(filing.filedAt)}</small>
                    </a>
                  ))}
                </div>
              </section>

              <section className="panel snapshots-panel">
                <div className="section-title">
                  <CalendarClock size={18} />
                  <h2>Research Snapshots</h2>
                  <InfoTooltip text={termDefinitions['Research Snapshots']} />
                </div>
                {snapshots.length === 0 ? (
                  <p className="muted">No snapshots saved for {dashboard.ticker}.</p>
                ) : (
                  <div className="snapshot-list">
                    {snapshots.map((snapshot) => (
                      <div className="snapshot-row" key={snapshot.id}>
                        <div>
                          <strong>{formatDate(snapshot.createdAt)}</strong>
                          <span>{snapshot.summary}</span>
                        </div>
                        <span>{snapshot.riskScore}</span>
                      </div>
                    ))}
                  </div>
                )}
              </section>

              {methodology ? (
                <section className="panel methodology-panel">
                  <div className="section-title">
                    <Info size={18} />
                    <h2>Methodology</h2>
                    <InfoTooltip text="Explains how EquityLens calculates returns, volatility, drawdown, risk scoring, and news relevance." />
                  </div>
                  <p className="methodology-summary">{methodology.summary}</p>
                  <code className="formula-block">{methodology.riskFormula}</code>
                  <div className="methodology-list">
                    {methodology.components.map((component) => (
                      <article key={component.name}>
                        <strong>{component.name}</strong>
                        <span>{formatWeight(component.weight)}</span>
                        <p>{component.description}</p>
                      </article>
                    ))}
                  </div>
                </section>
              ) : null}
            </section>
          </>
        )}
      </section>
    </main>
  )
}

function compressChartPoints(points: PricePoint[]) {
  if (points.length <= 320) {
    return points
  }

  const interval = Math.ceil(points.length / 320)
  return points.filter((_, index) => index % interval === 0 || index === points.length - 1)
}

function filterChartPoints(points: PricePoint[], rangeKey: ChartRangeKey) {
  if (points.length === 0 || rangeKey === '5Y') {
    return points
  }

  const range = chartRanges.find((item) => item.key === rangeKey)
  const latestDate = new Date(points[points.length - 1].date)

  if (!range) {
    return points
  }

  if (range.sessions) {
    return points.slice(Math.max(points.length - range.sessions, 0))
  }

  const startDate = new Date(latestDate)

  if (range.days) {
    startDate.setDate(startDate.getDate() - range.days)
  }

  if (range.months) {
    startDate.setMonth(startDate.getMonth() - range.months)
  }

  if (range.years) {
    startDate.setFullYear(startDate.getFullYear() - range.years)
  }

  return points.filter((point) => new Date(point.date) >= startDate)
}

function formatMarketCap(value: number) {
  return value > 0 ? compactNumber.format(value) : 'N/A'
}

function formatOptionalDate(value?: string) {
  return value ? formatDate(value) : 'N/A'
}

function findRiskDetail(components: RiskComponentDetail[], label: string) {
  return components.find((component) => component.name.toLowerCase() === label.toLowerCase())
}

function formatWeight(value?: number, fallback?: string) {
  if (typeof value === 'number') {
    return `${Math.round(value * 100)}%`
  }

  return fallback ?? 'N/A'
}

function riskClass(score: number) {
  if (score <= 25) {
    return 'low'
  }

  if (score <= 50) {
    return 'moderate'
  }

  if (score <= 75) {
    return 'high'
  }

  return 'very-high'
}

function getErrorMessage(error: unknown) {
  if (error instanceof Error) {
    return error.message
  }

  return 'Unable to load EquityLens data.'
}

function readInitialTicker() {
  if (typeof window === 'undefined') {
    return DEFAULT_TICKER
  }

  return new URLSearchParams(window.location.search).get('ticker')?.trim().toUpperCase() || DEFAULT_TICKER
}

function readInitialRange(): ChartRangeKey {
  if (typeof window === 'undefined') {
    return '1Y'
  }

  const requestedRange = new URLSearchParams(window.location.search)
    .get('range')
    ?.trim()
    .toUpperCase()

  return chartRanges.some((range) => range.key === requestedRange)
    ? requestedRange as ChartRangeKey
    : '1Y'
}

function updateUrlParam(key: string, value: string) {
  const url = new URL(window.location.href)
  url.searchParams.set(key, value)
  window.history.replaceState(null, '', url)
}

export default App

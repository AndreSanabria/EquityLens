import { useEffect, useMemo, useState } from 'react'
import type { FormEvent, ReactNode } from 'react'
import {
  AlertTriangle,
  BarChart3,
  BookmarkPlus,
  CalendarClock,
  ExternalLink,
  FileText,
  Info,
  Newspaper,
  RefreshCw,
  Save,
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
import type {
  FinancialMetricDirection,
  Methodology,
  PricePoint,
  RiskComponentDetail,
  ResearchSnapshot,
  StockDashboard,
  WatchlistItem,
} from './types.ts'
import './App.css'

const DEFAULT_TICKER = 'MSFT'

type ChartRangeKey = '1D' | '3D' | '1W' | '2W' | '4W' | '3M' | '6M' | '1Y' | '5Y'

const chartRanges: Array<{
  key: ChartRangeKey
  label: string
  description: string
  days?: number
  months?: number
  years?: number
  sessions?: number
}> = [
  { key: '1D', label: '1D', sessions: 2, description: 'Last two available trading closes.' },
  { key: '3D', label: '3D', sessions: 4, description: 'Last four available trading closes.' },
  { key: '1W', label: '1W', days: 7, description: 'Price history from the last calendar week.' },
  { key: '2W', label: '2W', days: 14, description: 'Price history from the last two calendar weeks.' },
  { key: '4W', label: '4W', days: 28, description: 'Price history from the last four calendar weeks.' },
  { key: '3M', label: '3M', months: 3, description: 'Price history from the last three months.' },
  { key: '6M', label: '6M', months: 6, description: 'Price history from the last six months.' },
  { key: '1Y', label: '1Y', years: 1, description: 'Price history from the last year.' },
  { key: '5Y', label: '5Y', years: 5, description: 'All five years of available price history.' },
]

const riskComponents = [
  {
    label: 'Volatility',
    key: 'volatilityScore',
    weight: '30%',
    description: 'Measures how much the stock price has moved day to day. Higher annualized volatility creates a higher risk score.',
  },
  {
    label: 'Max drawdown',
    key: 'maxDrawdownScore',
    weight: '25%',
    description: 'Measures the worst drop from a previous high. A deeper past decline means the stock has shown larger downside risk.',
  },
  {
    label: 'Revenue instability',
    key: 'revenueInstabilityScore',
    weight: '15%',
    description: 'Looks at annual revenue changes. Falling or inconsistent revenue raises the score.',
  },
  {
    label: 'Earnings instability',
    key: 'earningsInstabilityScore',
    weight: '15%',
    description: 'Looks at annual net income changes. Profit declines or sharp swings raise the score.',
  },
  {
    label: 'Debt pressure',
    key: 'debtPressureScore',
    weight: '10%',
    description: 'Uses debt, cash, and liability growth. More debt relative to cash or rising liabilities raises the score.',
  },
  {
    label: 'News risk',
    key: 'newsRiskScore',
    weight: '5%',
    description: 'Scores recent headlines by category, recency, and source quality. Legal, debt, and layoff headlines carry more risk weight.',
  },
] as const

const termDefinitions: Record<string, string> = {
  'Current price': 'The latest closing price in the available data set.',
  '52-week high': 'The highest price reached during the most recent year of available data.',
  '52-week low': 'The lowest price reached during the most recent year of available data.',
  'Market cap': 'Market capitalization: estimated company value based on share price and shares outstanding.',
  'Price History': 'Historical closing prices used to show how the stock moved over the selected time range.',
  Returns: 'Percentage change from a past price to the latest available price.',
  'Annual volatility': 'Daily return volatility converted to a yearly estimate using 252 trading days.',
  'Max drawdown': 'The largest decline from a previous high to a later low in the available price series.',
  'Risk Breakdown': 'A transparent risk estimate based on price movement, financial stability, debt pressure, and headline risk.',
  'Financial Direction': 'A year-over-year comparison of financial metrics such as revenue, net income, cash, debt, assets, and liabilities.',
  'Relevant News': 'Headlines ranked by category, recency, and source quality.',
  'SEC Filings': 'Official filings submitted to the U.S. Securities and Exchange Commission.',
  'Research Snapshots': 'Saved copies of the dashboard result so the same company can be compared over time.',
  'Data Freshness': 'Shows which live data sources were used and how current each major dashboard section is.',
  CIK: 'Central Index Key: the SEC identifier used to find company filings.',
  'Relevance score': 'A 0-100 score based on headline keywords, recency, and source quality.',
  '10-Q': 'Quarterly SEC report covering recent financial performance and business updates.',
  '10-K': 'Annual SEC report covering full-year results, business risks, and company details.',
  '8-K': 'Current SEC report used for important company events.',
  Assets: 'Resources the company owns or controls.',
  Cash: 'Cash and cash-like resources available to the company.',
  Debt: 'Borrowed money or debt-like obligations.',
  Liabilities: 'Company obligations, including debts and other amounts owed.',
  'Net Income': 'Profit after expenses, taxes, and other costs.',
  Revenue: 'Sales or operating income before expenses.',
}

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

function DashboardHeader({
  dashboard,
  isSaving,
  notes,
  onCreateSnapshot,
  onNotesChange,
  onSaveWatchlist,
  watchlistHasTicker,
}: {
  dashboard: StockDashboard
  isSaving: boolean
  notes: string
  onCreateSnapshot: () => void
  onNotesChange: (value: string) => void
  onSaveWatchlist: () => void
  watchlistHasTicker: boolean
}) {
  return (
    <header className="dashboard-header">
      <div>
        <span className="eyebrow">{dashboard.companyProfile.exchange}</span>
        <h1>
          {dashboard.companyProfile.ticker}
          <span>{dashboard.companyProfile.companyName}</span>
        </h1>
        <p>
          {dashboard.companyProfile.sector} - {dashboard.companyProfile.industry} - CIK {dashboard.companyProfile.cik}
          <InfoTooltip text={termDefinitions.CIK} />
        </p>
      </div>
      <div className="action-panel">
        <input
          aria-label="Watchlist notes"
          onChange={(event) => onNotesChange(event.target.value)}
          placeholder="Watchlist note"
          value={notes}
        />
        <button disabled={isSaving} onClick={onSaveWatchlist} type="button">
          <BookmarkPlus size={17} />
          {watchlistHasTicker ? 'Update' : 'Watch'}
        </button>
        <button disabled={isSaving} onClick={onCreateSnapshot} type="button">
          <Save size={17} />
          Snapshot
        </button>
      </div>
    </header>
  )
}

function MetricTile({
  icon,
  label,
  info,
  value,
}: {
  icon: ReactNode
  label: string
  info?: string
  value: string
}) {
  return (
    <div className="metric-tile">
      {icon}
      <span>
        {label}
        {info ? <InfoTooltip text={info} /> : null}
      </span>
      <strong>{value}</strong>
    </div>
  )
}

function InfoTooltip({ text }: { text: string }) {
  return (
    <span className="info-tooltip">
      <button aria-label={text} type="button">
        <Info size={13} />
      </button>
      <span role="tooltip">{text}</span>
    </span>
  )
}

function FreshnessItem({ label, value }: { label: string; value: string }) {
  return (
    <div className="freshness-item">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  )
}

function FinancialTable({ metrics }: { metrics: FinancialMetricDirection[] }) {
  return (
    <div className="table-wrap">
      <table>
        <thead>
          <tr>
            <th>Metric</th>
            <th>Previous</th>
            <th>Current</th>
            <th>Direction</th>
          </tr>
        </thead>
        <tbody>
          {metrics.map((metric) => (
            <tr key={metric.metricName}>
              <td>
                {splitMetric(metric.metricName)}
                <InfoTooltip text={termDefinitions[splitMetric(metric.metricName)] ?? 'Financial metric used in the year-over-year direction check.'} />
              </td>
              <td>{compactNumber.format(metric.previousValue)}</td>
              <td>{compactNumber.format(metric.currentValue)}</td>
              <td>
                <span className={`direction-pill ${directionClass(metric.directionLabel)}`}>
                  {metric.directionLabel}
                </span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function PriceChart({ points }: { points: PricePoint[] }) {
  if (points.length === 0) {
    return <div className="empty-chart">No price data</div>
  }

  const width = 900
  const height = 320
  const padding = { top: 18, right: 22, bottom: 36, left: 58 }
  const closes = points.map((point) => point.close)
  const min = Math.min(...closes)
  const max = Math.max(...closes)
  const range = Math.max(max - min, 1)

  const getX = (index: number) =>
    padding.left + (index / Math.max(points.length - 1, 1)) * (width - padding.left - padding.right)
  const getY = (close: number) =>
    padding.top + ((max - close) / range) * (height - padding.top - padding.bottom)

  const linePath = points
    .map((point, index) => `${index === 0 ? 'M' : 'L'} ${getX(index).toFixed(2)} ${getY(point.close).toFixed(2)}`)
    .join(' ')
  const areaPath = `${linePath} L ${getX(points.length - 1).toFixed(2)} ${height - padding.bottom} L ${padding.left} ${height - padding.bottom} Z`
  const first = points[0]
  const last = points[points.length - 1]
  const midIndex = Math.floor(points.length / 2)
  const ticks = [first, points[midIndex], last]

  return (
    <svg className="price-chart" role="img" viewBox={`0 0 ${width} ${height}`} aria-label="Historical close price chart">
      <defs>
        <linearGradient id="nativePriceFill" x1="0" x2="0" y1="0" y2="1">
          <stop offset="0%" stopColor="#2b7a63" stopOpacity="0.32" />
          <stop offset="100%" stopColor="#2b7a63" stopOpacity="0.02" />
        </linearGradient>
      </defs>
      {[0, 1, 2, 3].map((line) => {
        const y = padding.top + (line / 3) * (height - padding.top - padding.bottom)
        return <line className="chart-grid" key={line} x1={padding.left} x2={width - padding.right} y1={y} y2={y} />
      })}
      <text className="chart-axis" x={8} y={padding.top + 5}>
        {currency.format(max)}
      </text>
      <text className="chart-axis" x={8} y={height - padding.bottom}>
        {currency.format(min)}
      </text>
      {ticks.map((point, index) => (
        <text className="chart-axis date" key={`${point.date}-${index}`} x={getX(index === 0 ? 0 : index === 1 ? midIndex : points.length - 1)} y={height - 10}>
          {formatDate(point.date)}
        </text>
      ))}
      <path className="chart-area" d={areaPath} />
      <path className="chart-line" d={linePath} />
      <circle className="chart-last-point" cx={getX(points.length - 1)} cy={getY(last.close)} r="5" />
      <text className="chart-last-label" x={getX(points.length - 1) - 86} y={getY(last.close) - 10}>
        {currency.format(last.close)}
      </text>
    </svg>
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

function splitMetric(metricName: string) {
  return metricName.replace(/([a-z])([A-Z])/g, '$1 $2')
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

function directionClass(label: string) {
  if (label === 'Improving' || label === 'Lower Risk') {
    return 'good'
  }

  if (label === 'Weakening' || label === 'Higher Risk') {
    return 'caution'
  }

  return 'neutral'
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

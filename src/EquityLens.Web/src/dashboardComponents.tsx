import type { ReactNode } from 'react'
import {
  BookmarkPlus,
  Info,
  Save,
} from 'lucide-react'
import {
  compactNumber,
  currency,
  formatDate,
} from './format.ts'
import { termDefinitions } from './dashboardConfig.ts'
import type {
  FinancialMetricDirection,
  PricePoint,
  StockDashboard,
} from './types.ts'

export function DashboardHeader({
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

export function MetricTile({
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

export function InfoTooltip({ text }: { text: string }) {
  return (
    <span className="info-tooltip">
      <button aria-label={text} type="button">
        <Info size={13} />
      </button>
      <span role="tooltip">{text}</span>
    </span>
  )
}

export function FreshnessItem({ label, value }: { label: string; value: string }) {
  return (
    <div className="freshness-item">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  )
}

export function FinancialTable({ metrics }: { metrics: FinancialMetricDirection[] }) {
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

export function PriceChart({ points }: { points: PricePoint[] }) {
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

function splitMetric(metricName: string) {
  return metricName.replace(/([a-z])([A-Z])/g, '$1 $2')
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

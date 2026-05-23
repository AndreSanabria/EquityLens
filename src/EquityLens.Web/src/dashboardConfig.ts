import type { RiskScore } from './types.ts'

export const DEFAULT_TICKER = 'MSFT'

export type ChartRangeKey = '1D' | '3D' | '1W' | '2W' | '4W' | '3M' | '6M' | '1Y' | '5Y'

export const chartRanges: Array<{
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

export const riskComponents: Array<{
  label: string
  key: keyof Pick<
    RiskScore,
    | 'volatilityScore'
    | 'maxDrawdownScore'
    | 'revenueInstabilityScore'
    | 'earningsInstabilityScore'
    | 'debtPressureScore'
    | 'newsRiskScore'
  >
  weight: string
  description: string
}> = [
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
]

export const termDefinitions: Record<string, string> = {
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

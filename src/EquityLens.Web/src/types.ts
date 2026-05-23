export type StockDashboard = {
  ticker: string
  generatedAt: string
  narrativeSummary: string
  companyProfile: CompanyProfile
  performance: PerformanceOverview
  riskAnalysis: RiskScore
  financialDirection: FinancialDirection
  relevantNews: RankedNewsItem[]
  latestFilings: LatestFiling[]
  dataFreshness: DataFreshness
}

export type CompanyProfile = {
  ticker: string
  companyName: string
  sector: string
  industry: string
  exchange: string
  cik: string
  marketCap: number
  currentPrice: number
  fiftyTwoWeekHigh: number
  fiftyTwoWeekLow: number
  latestFilingForm: string
  lastUpdated: string
}

export type PerformanceOverview = {
  currentPrice: number
  returns: ReturnMetric[]
  chartPoints: PricePoint[]
  annualizedVolatility: number
  maxDrawdown: number
}

export type ReturnMetric = {
  period: string
  percentReturn: number
}

export type PricePoint = {
  date: string
  close: number
  volume: number
}

export type RiskScore = {
  finalScore: number
  riskLevel: string
  volatilityScore: number
  maxDrawdownScore: number
  revenueInstabilityScore: number
  earningsInstabilityScore: number
  debtPressureScore: number
  newsRiskScore: number
  mainDrivers: string[]
  components: RiskComponentDetail[]
}

export type RiskComponentDetail = {
  name: string
  score: number
  weight: number
  metricValue: string
  explanation: string
}

export type FinancialDirection = {
  metrics: FinancialMetricDirection[]
  overallDirection: string
}

export type FinancialMetricDirection = {
  metricName: string
  previousValue: number
  currentValue: number
  directionLabel: string
}

export type RankedNewsItem = {
  title: string
  source: string
  url: string
  isDirectArticleUrl: boolean
  publishedAt: string
  category: string
  relevanceScore: number
}

export type LatestFiling = {
  formType: string
  filedAt: string
  description: string
  filingUrl: string
}

export type WatchlistItem = {
  id: number
  ticker: string
  notes: string
  addedAt: string
  lastViewedAt: string
  lastKnownRiskScore?: number
}

export type ResearchSnapshot = {
  id: number
  ticker: string
  createdAt: string
  riskScore: number
  oneYearReturn: number
  summary: string
}

export type DataFreshness = {
  providerMode: string
  priceDataThrough?: string
  financialDataFiledAt?: string
  newsUpdatedAt?: string
  filingsUpdatedAt?: string
  limitations: string[]
}

export type Methodology = {
  summary: string
  riskFormula: string
  components: MethodologyComponent[]
}

export type MethodologyComponent = {
  name: string
  weight: number
  description: string
}

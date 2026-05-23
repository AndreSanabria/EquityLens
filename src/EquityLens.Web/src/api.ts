import type {
  Methodology,
  ResearchSnapshot,
  StockDashboard,
  WatchlistItem,
} from './types.ts'

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL?.replace(/\/$/, '') ??
  'http://127.0.0.1:5077'

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...init?.headers,
    },
    ...init,
  })

  if (!response.ok) {
    throw new Error(await buildErrorMessage(response))
  }

  if (response.status === 204) {
    return undefined as T
  }

  return response.json() as Promise<T>
}

export function getDashboard(ticker: string) {
  return request<StockDashboard>(`/api/stocks/${ticker}/dashboard`)
}

export function getSupportedTickers() {
  return request<string[]>('/api/stocks/supported')
}

export function getMethodology() {
  return request<Methodology>('/api/methodology')
}

export function getWatchlist() {
  return request<WatchlistItem[]>('/api/watchlist')
}

export function saveWatchlistItem(ticker: string, notes: string) {
  return request<WatchlistItem>('/api/watchlist', {
    method: 'POST',
    body: JSON.stringify({ ticker, notes }),
  })
}

export function deleteWatchlistItem(ticker: string) {
  return request<void>(`/api/watchlist/${ticker}`, {
    method: 'DELETE',
  })
}

export function createSnapshot(ticker: string) {
  return request<ResearchSnapshot>(`/api/stocks/${ticker}/snapshot`, {
    method: 'POST',
  })
}

export function getSnapshots(ticker: string) {
  return request<ResearchSnapshot[]>(`/api/stocks/${ticker}/snapshots`)
}

async function buildErrorMessage(response: Response) {
  const text = await response.text()
  let serverMessage = text

  try {
    const parsed = JSON.parse(text) as { error?: string; title?: string; detail?: string }
    serverMessage = parsed.error ?? parsed.detail ?? parsed.title ?? text
  } catch {
    serverMessage = text
  }

  if (response.status === 429 || serverMessage.toLowerCase().includes('too many requests')) {
    return 'The market-data provider is temporarily rate-limiting requests. Wait a minute, then try again.'
  }

  if (response.status === 404) {
    return serverMessage || 'That ticker is not available from the current data provider.'
  }

  if (response.status >= 500) {
    return serverMessage
      ? `The data provider or API failed: ${serverMessage}`
      : 'The data provider or API failed while building this dashboard.'
  }

  return serverMessage || `Request failed with status ${response.status}`
}

# EquityLens Methodology

EquityLens is designed as a research organization tool. The dashboard uses transparent calculations so risk scores, financial direction, and headline relevance remain auditable.

## Returns

Returns compare the latest close against the nearest available trading day on or before each lookback date.

```text
Return percentage = (Current close - Past close) / Past close * 100
```

The dashboard currently reports `1M`, `3M`, `6M`, `1Y`, and `5Y` returns.

## Volatility

Volatility is calculated from daily close-to-close returns.

```text
Daily return = (Current close - Previous close) / Previous close
Annualized volatility = Standard deviation of daily returns * sqrt(252)
```

The model treats volatility below 20% as low risk and volatility above 60% as high risk.

## Max Drawdown

Max drawdown measures the worst decline from a prior peak in the available price series.

```text
Drawdown = (Current close - Prior peak) / Prior peak
```

The model treats drawdowns below 10% as low risk and drawdowns above 50% as high risk.

## Financial Direction

Financial direction compares annual SEC company facts year over year.

Tracked metrics:

- Revenue
- Net income
- Assets
- Liabilities
- Cash
- Debt

Revenue, net income, assets, and cash are generally better when rising. Liabilities and debt are treated as higher risk when rising materially.

## News Ranking

News ranking is rule-based. Headlines receive points for:

- Category relevance, such as earnings, legal, debt, leadership, M&A, product, or layoffs
- Recency
- Source quality

The highest-ranked articles are not automatically negative. Relevance means the article is more likely to matter for company research.

## Risk Formula

```text
Final Risk Score =
VolatilityScore * 0.30
+ MaxDrawdownScore * 0.25
+ RevenueInstabilityScore * 0.15
+ EarningsInstabilityScore * 0.15
+ DebtPressureScore * 0.10
+ NewsRiskScore * 0.05
```

Risk labels:

- `0-25`: Low risk
- `26-50`: Moderate risk
- `51-75`: High risk
- `76-100`: Very high risk

## Interpretation

The risk score is a structured estimate based on historical and current research signals. It does not forecast future price movement or provide financial advice.

# EquityLens Web

React + TypeScript frontend for the EquityLens stock research dashboard.

The frontend intentionally keeps business logic light. It renders structured dashboard data returned by `EquityLens.Api`, including chart points, returns, risk components, news rankings, SEC filings, watchlist data, and saved snapshots.

## Run

```powershell
npm.cmd install
npm.cmd run build
npm.cmd run preview -- --host 127.0.0.1 --port 5173
```

Custom API base URL:

```powershell
$env:VITE_API_BASE_URL="http://127.0.0.1:5077"
```

## Notes

- `npm.cmd run dev` works in normal paths, but folders containing `#` can trigger a Vite path warning. The root README documents the build/preview flow used for stable local execution.
- The UI includes hover tooltips for technical terms and risk components so the dashboard remains explainable and auditable.

# EquityLens Web

React + TypeScript frontend for the EquityLens stock research dashboard.

The frontend intentionally keeps business logic light. It renders structured dashboard data returned by `EquityLens.Api`, including chart points, returns, risk components, news rankings, SEC filings, watchlist data, and saved snapshots.

## Run

```powershell
npm.cmd install
npm.cmd run build
npm.cmd run preview -- --host 127.0.0.1 --port 5173
```

Set a custom API base URL when needed:

```powershell
$env:VITE_API_BASE_URL="http://127.0.0.1:5077"
```

## Notes

- `npm.cmd run dev` works in normal paths, but this workspace's `C# Game` folder can trigger a Vite path warning. Use the build/preview flow from the root README for the most reliable local run.
- The UI includes hover tooltips for technical terms and risk components so the dashboard remains explainable and auditable.

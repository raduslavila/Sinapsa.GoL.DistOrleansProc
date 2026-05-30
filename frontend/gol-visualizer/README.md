# gol-visualizer

React 19 + TypeScript frontend for the distributed Game of Life backend.

## Features

- Real-time grid visualization with incremental delta updates (`GET /api/grid/update`)
- Visual chunk boundary overlay (configurable)
- Configurable universe: chunks X/Y, chunk size (8–128 cells), live density
- Auto-run with variable speed; single-step mode; pause/resume
- Live statistics: generation count, grid dimensions, living cell count

## Development

```bash
# Requires .NET backend running on http://localhost:5050
cd frontend/gol-visualizer
npm install
npm start      # http://localhost:3000
```

The API base URL is configured via `REACT_APP_API_URL` (defaults to `http://localhost:5050/api`).

## Production (Kubernetes)

The frontend is built into a Docker image with an nginx server. The nginx config proxies `/api/` to the backend service and serves the React SPA.

```bash
# Build
docker build -t gol-frontend:local .

# Deploy via the root deploy script
cd ../..
.\deploy-kind.ps1
# Frontend: http://localhost:30000
```

## API Calls

| Call | Endpoint |
|------|----------|
| Initialize | `POST /api/init?chunksX=&chunksY=&chunkSize=&liveDensity=` |
| Re-initialize | `POST /api/reinit?chunksX=&chunksY=&chunkSize=&liveDensity=` |
| Step | `POST /api/step` |
| Full grid | `GET /api/grid` |
| Delta update | `GET /api/grid/update?lastSeenGeneration=N` |


# Sinapsa.GoL.DistOrleansProc

Distributed [Conway's Game of Life](https://en.wikipedia.org/wiki/Conway%27s_Game_of_Life) on **Microsoft Orleans 10.1 / .NET 9**. The grid is split into chunks managed by Orleans grains, coordinated by a cluster-singleton `GoLUniverseGrain` that keeps state consistent across all silos.

## Quick Start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Node.js 20+ (for the frontend)
- Docker Desktop (for Kubernetes deployment)

### Local development (single silo)

```bash
# Backend
cd Sinapsa.GoL.DistOrleansProc
dotnet run
# API → http://localhost:5050
# Dashboard → http://localhost:5050/dashboard

# Frontend (separate terminal)
cd frontend/gol-visualizer
npm install
npm start
# → http://localhost:3000
```

### Docker Compose (single silo + frontend)

Requires Docker Desktop running.

```bash
docker compose up --build
```

| Service | URL |
|---------|-----|
| Frontend | http://localhost:3000 |
| API | http://localhost:5050/api |
| Orleans Dashboard | http://localhost:5050/dashboard |
| Redis | `redis:6379` |
| RedisInsight | http://localhost:5540 |

Redis is used as the Orleans persistence store. RedisInsight is preconfigured to connect to the `redis` service in the compose stack.

### Kubernetes (multi-silo)

Requires Docker Desktop with Kubernetes enabled (or Kind).

```powershell
.\deploy-kind.ps1
```

This builds both images, loads them into cluster nodes, applies all manifests, waits for rollout, and starts `kubectl port-forward` background jobs.

| Service | URL |
|---------|-----|
| Frontend | http://localhost:30000 |
| API | http://localhost:30050/api |
| Orleans Dashboard | http://localhost:30050/dashboard |

See [docs/QUICKSTART_DISTRIBUTED.md](docs/QUICKSTART_DISTRIBUTED.md) for options and re-deploy instructions.

## API

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/init` | Initialize universe (`chunksX`, `chunksY`, `chunkSize`, `liveDensity`) |
| `POST` | `/api/reinit` | Re-randomize; reuses grain topology if dimensions unchanged |
| `POST` | `/api/step` | Advance one generation across all chunk grains |
| `GET` | `/api/grid` | Full grid snapshot (`{width, height, cells[][]}`) |
| `GET` | `/api/grid/update?lastSeenGeneration=N` | Delta update or full snapshot if >1 gen behind |
| `GET` | `/hc` | Health check |

Swagger UI: `http://localhost:5050/swagger` (development only)

## Configuration

`appsettings.json`:

```json
{
  "ClusterConfig": {
    "UseLocalhost": true,
    "UseKubernetesHosting": false,
    "UseDashboard": true,
    "ClusterOptions": {
      "ClusterId": "dev-GoL",
      "ServiceId": "dev-GoL-one"
    },
    "ClusterEndpointOptions": {
      "SiloPort": 30000,
      "GatewayPort": 11111
    }
  },
  "GrainStorageOptions": {
    "Providers": {
      "PubSubStore": {
        "ProviderKind": "Memory"
      },
      "GoLUniverseStore": {
        "ProviderKind": "Memory"
      },
      "GoLChunkStore": {
        "ProviderKind": "Memory"
      }
    }
  }
}
```

To use Redis-backed grain storage, change the provider kind for `GoLUniverseStore` and/or `GoLChunkStore` to `Redis` and set `RedisConnectionString` if needed. The Docker Compose setup provides `redis:6379` by default.

For Kubernetes, `UseKubernetesHosting: true` is set via environment variables in `k8s/backend.yaml`. Orleans uses CRD-based cluster membership (`Orleans.Clustering.Kubernetes`).

## Project Structure

```
Sinapsa.GoL.DistOrleansProc/          # ASP.NET Core host + Orleans silo
  Controllers/UniverseController.cs   # REST API
  Program.cs                          # Silo bootstrap
  Startup.cs                          # Middleware
Sinapsa.GoL.DistOrleansProc.Domain/   # Application layer
  Services/GoLService.cs              # Thin proxy to GoLUniverseGrain
Sinapsa.GoL.DistOrleansProc.GrainInterfaces/
  IGoLUniverseGrain.cs               # Cluster-singleton coordinator
  IGoLChunkGrain.cs                  # Per-chunk grain
Sinapsa.GoL.DistOrleansProc.Grains/
  GoLUniverseGrain.cs                # Universe coordinator implementation
  GoLChunkGrain.cs                   # Chunk grain implementation
Sinapsa.GoL.DistOrleansProc.Orleans.Core/  # Orleans infrastructure helpers
frontend/gol-visualizer/             # React + TypeScript UI
k8s/                                 # Kubernetes manifests
deploy-kind.ps1                      # One-command local K8s deploy
```

## Docs

| | |
|-|-|
| [Architecture](docs/ARCHITECTURE.md) | Grain design, placement, clustering, storage |
| [Distributed Chunks](docs/DISTRIBUTED_CHUNKS.md) | Inter-chunk communication protocol |
| [Kubernetes Quickstart](docs/QUICKSTART_DISTRIBUTED.md) | Full deploy walkthrough |
| [Visual Guide](docs/VISUAL_GUIDE.md) | Grid layout and request flow diagrams |

## Stack

.NET 9 · ASP.NET Core 9 · Orleans 10.1 · Orleans.Clustering.Kubernetes 10.0.1 · Orleans Dashboard · Redis · RedisInsight · React 19 · TypeScript

## License

[LICENSE.txt](LICENSE.txt) — Author: Radu Slavila

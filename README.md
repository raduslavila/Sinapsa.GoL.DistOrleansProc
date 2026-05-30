
# Sinapsa.GoL.DistOrleansProc — Distributed Conway's Game of Life

A distributed implementation of [Conway's Game of Life](https://en.wikipedia.org/wiki/Conway%27s_Game_of_Life) running on **Microsoft Orleans 10.1 / .NET 9**. The game grid is partitioned into chunks, each managed by an Orleans grain, and coordinated by a cluster-singleton universe grain — giving true horizontal scalability across multiple silos.

## Features

- **GoLUniverseGrain** — cluster-singleton that owns generation counter and grid dimensions; no duplicate or inconsistent state across pods.
- **GoLChunkGrain** — one grain per chunk, distributed across silos with `ActivationCountBasedPlacement`, communicating edge states during each step.
- **React + TypeScript frontend** with incremental delta updates (`GET /api/grid/update`).
- **Orleans Dashboard** on the same port as the API (`/dashboard`).
- **One-command Kubernetes deploy** (`deploy-kind.ps1`) for Docker Desktop or Kind.

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

See [docs/QUICKSTART_DISTRIBUTED.md](docs/QUICKSTART_DISTRIBUTED.md) for full details and options.

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
  }
}
```

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

- [Architecture](docs/ARCHITECTURE.md) — grain design, placement, state management
- [Distributed Chunks](docs/DISTRIBUTED_CHUNKS.md) — inter-chunk communication protocol
- [Kubernetes Quickstart](docs/QUICKSTART_DISTRIBUTED.md) — full K8s deploy walkthrough
- [Visual Guide](docs/VISUAL_GUIDE.md) — ASCII diagrams of the grid and grain layout

## Technologies

- [.NET 9](https://dotnet.microsoft.com/download/dotnet/9.0) / ASP.NET Core 9
- [Microsoft Orleans 10.1](https://github.com/dotnet/orleans)
- [Orleans.Clustering.Kubernetes 10.0.1](https://github.com/OrleansContrib/Orleans.Clustering.Kubernetes)
- [Orleans Dashboard](https://github.com/OrleansContrib/OrleansDashboard)
- React 19 + TypeScript

## License

See [LICENSE.txt](LICENSE.txt).

---

*Author: Radu Slavila*

> A distributed implementation of Conway's Game of Life using Microsoft Orleans

## Overview

This project demonstrates a scalable, distributed implementation of [Conway's Game of Life](https://en.wikipedia.org/wiki/Conway%27s_Game_of_Life) using the [Microsoft Orleans](https://github.com/dotnet/orleans) actor framework. The simulation is distributed across Orleans grains, enabling fault-tolerant and horizontally scalable execution of the cellular automaton.

## Features

- ? **Distributed Architecture**: Uses Orleans grains to distribute the game grid across multiple actors
- ?? **RESTful API**: Simple HTTP endpoints to control the simulation
- ?? **Real-time Monitoring**: Integrated Orleans Dashboard for cluster observability
- ?? **Docker Support**: Container-ready with Docker Compose configuration
- ?? **Kubernetes Ready**: Support for Kubernetes-based clustering
- ?? **Configurable**: Flexible configuration through appsettings.json
- ?? **Random Initialization**: Generate random initial states with configurable density

## Quick Start

### Prerequisites

- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) or later
- (Optional) Docker Desktop for containerized deployment
- (Optional) Visual Studio 2022 or VS Code

### Running Locally

1. **Clone the repository**
   ```bash
   git clone https://github.com/raduslavila/Sinapsa.GoL.DistOrleansProc
   cd Sinapsa.GoL.DistOrleansProc
   ```

2. **Build the solution**
   ```bash
   dotnet build
   ```

3. **Run the application**
   ```bash
   cd Sinapsa.GoL.DistOrleansProc
   dotnet run
   ```

4. **Access the API**
   - API: `http://localhost:5050`
   - Swagger UI: `http://localhost:5050/swagger`
   - Orleans Dashboard: `http://localhost:8000/dashboard`

### Using Docker

1. **Build and run with Docker Compose**
   ```bash
   docker-compose up --build
   ```

2. **Or build and run manually**
   ```bash
   docker build -t gol-dist-orleans-api .\Sinapsa.GoL.DistOrleansProc\
   docker run -p 5050:5050 -p 8000:8000 gol-dist-orleans-api:latest
   ```

3. **Access the services**
   - API endpoints available on port 5050
   - Dashboard accessible at `http://localhost:8000/dashboard`

## Usage

### Initialize the Game Grid

Initialize a distributed universe with configurable grid:

```bash
# 2x2 chunks (default), each 32x32 cells = 64x64 total
curl -X POST "http://localhost:5050/api/init"

# Or customize the grid
curl -X POST "http://localhost:5050/api/init?chunksX=4&chunksY=4&chunkWidth=32&chunkHeight=32&liveDensity=0.15"
```

**Response:**
```json
{
  "message": "Distributed universe initialized",
  "configuration": {
    "chunksX": 2,
    "chunksY": 2,
    "totalChunks": 4
  }
}
```

### Advance the Simulation

Run one step of the simulation and get the current state:

```bash
curl -X POST http://localhost:5050/api/step
```

**Response:** ASCII representation of the grid (# = alive, space = dead)
```

      #   #                     
       ##                       
      #                         

...
```

### View Current State

Get the current state without advancing:

```bash
curl http://localhost:5050/api/state
```

### Health Check

Check if the service is running:

```bash
curl http://localhost:5050/hc
```

## API Documentation

### Endpoints

| Method | Endpoint      | Description                                    |
|--------|---------------|------------------------------------------------|
| POST   | `/api/init`   | Initialize the distributed universe grid       |
| POST   | `/api/step`   | Advance simulation by one generation           |
| GET    | `/api/state`  | Get current state without advancing            |
| GET    | `/hc`         | Health check endpoint                          |

### Swagger UI

Interactive API documentation is available at `http://localhost:5050/swagger` when running in development mode.

## Configuration

Configuration is managed through `appsettings.json`:

```json
{
  "ClusterConfig": {
    "UseLocalhost": true,           // Use localhost clustering
    "UseKubernetesHosting": false,  // Enable for K8s deployment
    "UseDashboard": true,            // Enable Orleans Dashboard
    "DashboardPort": 8000,           // Dashboard port
    "UseLinuxStatistics": false,     // Linux performance counters
    "ClusterOptions": {
      "ClusterId": "dev-GoL",        // Unique cluster identifier
      "ServiceId": "dev-GoL-one"     // Service identifier
    },
    "ClusterEndpointOptions": {
      "SiloPort": 30000,             // Inter-silo communication
      "GatewayPort": 11111           // Client gateway port
    }
  }
}
```

### Configuration Options

- **UseLocalhost**: `true` for single-machine development, `false` for distributed deployment
- **UseKubernetesHosting**: Enable Kubernetes-aware clustering
- **UseDashboard**: Enable/disable Orleans Dashboard
- **DashboardPort**: Port for the dashboard web UI
- **ClusterId**: Must be the same across all silos in a cluster
- **ServiceId**: Identifies the service (affects grain compatibility)
- **SiloPort**: Port for silo-to-silo communication (default: 30000)
- **GatewayPort**: Port for client-to-silo communication (default: 11111)

## Project Structure

```
Sinapsa.GoL.DistOrleansProc/
??? Sinapsa.GoL.DistOrleansProc/              # Web API Host
?   ??? Controllers/                           # REST API Controllers
?   ?   ??? WarmUpController.cs               # Initialization endpoint
?   ?   ??? StepController.cs                 # Simulation step endpoint
?   ??? Program.cs                             # Application entry point
?   ??? Startup.cs                             # Service configuration
?   ??? appsettings.json                       # Configuration file
?
??? Sinapsa.GoL.DistOrleansProc.Domain/       # Business Logic
?   ??? Services/                              # Domain services
?   ?   ??? IGoLService.cs                    # Service interface
?   ?   ??? GoLService.cs                     # Service implementation
?   ??? Configuration/                         # Configuration models
?   ??? Extensions/                            # DI and Orleans extensions
?
??? Sinapsa.GoL.DistOrleansProc.GrainInterfaces/  # Grain Contracts
?   ??? IGoLChunkGrain.cs                     # Grain interface
?   ??? Models/                                # Data models
?   ?   ??? Cell.cs                           # Individual cell
?   ?   ??? GoLChunkGrainState.cs            # Grain state
?   ??? Services/                              # Utilities
?
??? Sinapsa.GoL.DistOrleansProc.Grains/       # Grain Implementations
?   ??? GoLChunkGrain.cs                      # Game of Life logic
?
??? Sinapsa.GoL.DistOrleansProc.Orleans.Core/ # Orleans Infrastructure
    ??? Services/                              # Generic grain factories
```

## How It Works

### Conway's Game of Life Rules

The simulation follows the classic rules:

1. **Underpopulation**: A live cell with fewer than 2 live neighbors dies
2. **Survival**: A live cell with 2-3 live neighbors lives on
3. **Overpopulation**: A live cell with more than 3 live neighbors dies
4. **Reproduction**: A dead cell with exactly 3 live neighbors becomes alive

### Implementation Details

- **Grain Architecture**: Each `GoLChunkGrain` manages a portion of the game grid (currently 32x32 cells)
- **State Management**: Cell states are persisted using Orleans in-memory storage
- **Neighbor Connectivity**: Cells maintain references to their 8 neighbors (Moore neighborhood)
- **Two-Phase Update**: Uses `IsAlive` and `IsAliveNext` to prevent race conditions during updates

### Workflow

1. **Initialization**: 
   - `/api/init` endpoint creates a grid of grains
   - Each chunk initialized with random state (15% alive by default)
   - Neighbor connections are established between chunks

2. **Simulation Step**:
   - `/api/step` endpoint triggers parallel advancement of all chunks
   - Each chunk fetches edge states from neighbors
   - Rules are applied considering cross-chunk neighbors
   - All cells update simultaneously
   - State is persisted to Orleans storage

3. **State Retrieval**:
   - Current state is retrieved from all chunks
   - Combined into single ASCII representation
   - Returned as HTTP response

## Orleans Dashboard

The Orleans Dashboard provides real-time insights into the cluster:

- **Silo Status**: View active silos and their health
- **Grain Statistics**: Monitor grain activations and method calls
- **Performance Metrics**: CPU, memory, and throughput statistics
- **Reminders & Timers**: Track scheduled grain operations

Access at: `http://localhost:8000/dashboard`

## Development

### Building from Source

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests (if available)
dotnet test

# Run the application
dotnet run --project Sinapsa.GoL.DistOrleansProc
```

### Development Tools

- **Visual Studio 2022**: Full IDE support with debugging
- **VS Code**: Lightweight editor with C# extension
- **Rider**: JetBrains IDE for .NET development

## Deployment

### Docker Deployment

The solution includes Docker support:

```bash
# Build Docker image
docker build -t gol-dist-orleans-api .\Sinapsa.GoL.DistOrleansProc\

# Run container
docker run -p 5050:5050 -p 8000:8000 gol-dist-orleans-api:latest
```

### Kubernetes Deployment

For Kubernetes deployment:

1. Set `UseKubernetesHosting: true` in configuration
2. Deploy using provided Kubernetes manifests (if available)
3. Configure service discovery and networking

```bash
kubectl apply -f kubernetes/
```

## Extensibility

### Adding New Features

The architecture supports several extensions:

1. **Multi-Chunk Grids**: Create multiple grains for larger grids
2. **Pattern Library**: Pre-defined initial configurations (gliders, oscillators, etc.)
3. **Web UI**: Real-time visualization using SignalR
4. **Persistence**: Replace memory storage with durable storage (Azure, SQL, Redis)
5. **Game Variations**: Implement different cellular automaton rules

### Custom Storage Provider

To use persistent storage:

1. Add storage provider package (e.g., `Microsoft.Orleans.Persistence.AzureStorage`)
2. Configure in `Program.cs`:
   ```csharp
   siloBuilder.AddAzureTableGrainStorage("ChunkMemory", options => {
       options.ConnectionString = "...";
   });
   ```
3. Update grain storage provider attribute if needed

## Performance

### Current Capabilities

- **Grid Size**: 32x32 cells per grain (configurable)
- **Update Speed**: Limited by grain activation and storage persistence
- **Scalability**: Horizontal scaling through Orleans clustering

### Optimization Tips

1. **Batch Updates**: Process multiple steps before persisting
2. **Lazy Persistence**: Only save state on deactivation
3. **Stateless Workers**: Use stateless worker grains for computation
4. **Grain Pooling**: Reuse grain instances (infrastructure present)

## Troubleshooting

### Common Issues

**Issue**: Orleans dashboard not accessible
```
Solution: Ensure UseDashboard is true and port 8000 is not blocked
```

**Issue**: Grain activation failures
```
Solution: Check that all grain assemblies are properly referenced in Program.cs
```

**Issue**: State not persisting
```
Solution: Verify storage provider configuration and [StorageProvider] attribute
```

### Logging

Enable detailed logging in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning",
      "Orleans": "Debug"
    }
  }
}
```

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues.

### Development Guidelines

1. Follow existing code style and conventions
2. Add unit tests for new features
3. Update documentation for API changes
4. Ensure Docker build succeeds

## Technologies

- [.NET 6.0](https://dotnet.microsoft.com/download/dotnet/6.0)
- [Microsoft Orleans 3.6.5](https://github.com/dotnet/orleans)
- [ASP.NET Core](https://docs.microsoft.com/aspnet/core)
- [Swashbuckle (Swagger)](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
- [Orleans Dashboard](https://github.com/OrleansContrib/OrleansDashboard)

## Resources

- [Microsoft Orleans Documentation](https://docs.microsoft.com/dotnet/orleans/)
- [Conway's Game of Life - Wikipedia](https://en.wikipedia.org/wiki/Conway%27s_Game_of_Life)
- [Actor Model Pattern](https://en.wikipedia.org/wiki/Actor_model)
- [Game of Life Patterns](https://conwaylife.com/wiki/Main_Page)

## Architecture

For detailed architecture documentation, see [ARCHITECTURE.md](./ARCHITECTURE.md)

## License

This project is available under the license specified in the repository.

## Author

Created by Radu Slavila

## Acknowledgments

- Microsoft Orleans team for the excellent actor framework
- John Conway for creating the Game of Life
- The .NET community for continuous support

---

**Note**: This is a demonstration project showcasing Orleans capabilities. For production use, consider implementing authentication, persistent storage, and additional error handling.
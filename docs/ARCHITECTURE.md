# Architecture

## Overview

Conway's Game of Life runs as an **Orleans actor cluster** on .NET 9. The game grid is partitioned into square chunks — each managed by a `GoLChunkGrain` — coordinated by a single `GoLUniverseGrain` that lives on exactly one silo across the whole cluster.

## Grain Design

### GoLUniverseGrain (`key = "universe"`)

Cluster-singleton coordinator grain. Because Orleans guarantees a single activation per (type, key) pair, this grain is the one authoritative source of truth for:

- Universe dimensions (`ChunksX`, `ChunksY`, `ChunkSize`)
- Current generation counter
- Grid snapshots (in activation memory; rebuilt from chunk grains on reactivation)

Persistence: `[StorageProvider(ProviderName = "ChunkMemory")]` — in-memory by default.

The `GoLService` (application layer) is a thin proxy that always calls this grain:

```csharp
private IGoLUniverseGrain Universe =>
    _grainFactory.GetGrain<IGoLUniverseGrain>("universe");
```

Before this design, `GoLService` was a per-silo DI singleton holding `_currentGeneration` as an instance field. When `/step` hit a different pod than `/init`, the counter restarted from 0.

### GoLChunkGrain

One grain per chunk of the grid. Each grain owns a `ChunkSize × ChunkSize` boolean cell array and communicates edge states with its 8 neighbors during each `Advance()` call.

**Placement**: `[ActivationCountBasedPlacement]` — overrides the global `PreferLocalPlacement` default, so chunk grains distribute across silos rather than all landing on the same one.

**Neighbor communication**: Edge and corner methods are marked `[AlwaysInterleave]` to allow concurrent reads during a step:

```csharp
[AlwaysInterleave] Task<bool[]> GetTopEdge();
[AlwaysInterleave] Task<bool[]> GetBottomEdge();
[AlwaysInterleave] Task<bool[]> GetLeftEdge();
[AlwaysInterleave] Task<bool[]> GetRightEdge();
[AlwaysInterleave] Task<bool> GetTopLeftCorner();
// ... etc.
```

Persistence: same `ChunkMemory` provider.

## Request Flow

```
HTTP client
    ↓ POST /api/step
UniverseController
    ↓ GoLService.RunUniverseStep()
    ↓ GoLUniverseGrain("universe").StepUniverse()   [one silo]
        ↓ parallel fan-out
    GoLChunkGrain("0_0").Advance()   [silo A]
    GoLChunkGrain("0_1").Advance()   [silo B]
    GoLChunkGrain("1_0").Advance()   [silo C]
    GoLChunkGrain("1_1").Advance()   [silo D]
        each chunk pulls edge states from all 8 neighbors
        ↑ State.CurrentGeneration++
    ↑ Ok()
```

## Project Layer Map

| Project | Role |
|---------|------|
| `Sinapsa.GoL.DistOrleansProc` | ASP.NET Core host, Orleans silo bootstrap, REST controllers |
| `Sinapsa.GoL.DistOrleansProc.Domain` | `GoLService` (proxy), DI wiring, `SiloHostBuilderExtensions` |
| `Sinapsa.GoL.DistOrleansProc.GrainInterfaces` | `IGoLUniverseGrain`, `IGoLChunkGrain`, DTOs, state models |
| `Sinapsa.GoL.DistOrleansProc.Grains` | `GoLUniverseGrain`, `GoLChunkGrain` implementations |
| `Sinapsa.GoL.DistOrleansProc.Orleans.Core` | Generic grain infrastructure helpers |

## Clustering

**Local development**: `UseLocalhost` clustering (single silo, in-process).

**Kubernetes**: `Orleans.Clustering.Kubernetes 10.0.1` with CRD-based membership.
- CRDs: `silos.orleans.dot.net`, `clusterversions.orleans.dot.net` (applied from `k8s/crds.yaml`)
- ServiceAccount with RBAC to read/write CRDs (applied from `k8s/rbac.yaml`)
- `ClusterId` must be RFC 1123-compliant (lowercase only) — currently `k8s-gol`
- Pod IP injected via K8s downward API (`POD_IP` env var) and used as `AdvertisedIPAddress`

## Ports

| Port | Purpose |
|------|---------|
| 5050 | HTTP API + Orleans Dashboard (at `/dashboard`) |
| 11111 | Client gateway (silo ↔ client) |
| 30000 | Silo-to-silo communication |

The Orleans Dashboard is served by `MapOrleansDashboard()` on the same Kestrel port as the API — it is **not** a standalone server on a separate port.

## Storage

Currently uses `AddMemoryGrainStorage("ChunkMemory")` — in-memory, cleared on silo restart. The `[StorageProvider]` attributes on both grains reference this provider. Replacing with Redis or another durable store requires only changing the provider registration in `Program.cs`.

## Sinapsa.GoL.DistOrleansProc - Distributed Conway's Game of Life

### Overview

This project implements Conway's Game of Life using Microsoft Orleans, a distributed actor framework. The implementation distributes the game grid across multiple grains, enabling scalable and fault-tolerant execution of the cellular automaton simulation.

## System Architecture

### High-Level Architecture

```
???????????????????????????????????????????????????????????????
?                      ASP.NET Core Host                      ?
?  ??????????????????  ????????????????????????????????????  ?
?  ?  Web API       ?  ?    Orleans Silo                  ?  ?
?  ?  Controllers   ????    - Grain Hosting               ?  ?
?  ?                ?  ?    - Cluster Membership          ?  ?
?  ??????????????????  ?    - Dashboard (Port 8000)       ?  ?
?                      ????????????????????????????????????  ?
???????????????????????????????????????????????????????????????
           ?                           ?
           ?                           ?
      HTTP API                    Actor Model
     (Port 5050)                  (Orleans)
           ?                           ?
           ?                           ?
    ???????????????          ??????????????????
    ?  GoLService ???????????? GoLChunkGrain  ?
    ?             ?          ?  (Actor/Grain) ?
    ???????????????          ??????????????????
                                     ?
                                     ?
                              ???????????????
                              ?   State     ?
                              ?  (Memory)   ?
                              ???????????????
```

### Project Structure

The solution follows a layered architecture with clear separation of concerns:

#### 1. **Sinapsa.GoL.DistOrleansProc** (Web API Host)
- **Purpose**: Main entry point and web API host
- **Target Framework**: .NET 6.0
- **Responsibilities**:
  - Hosting the Orleans Silo
  - Exposing REST API endpoints
  - Configuring Swagger/OpenAPI
  - HTTP endpoint on port 5050

**Key Components**:
- `Program.cs`: Application bootstrapping, Orleans silo configuration
- `Startup.cs`: Service registration, middleware pipeline
- `Controllers/`:
  - `WarmUpController`: Initializes the Game of Life grid
  - `StepController`: Advances the simulation and retrieves current state

#### 2. **Sinapsa.GoL.DistOrleansProc.Domain** (Business Logic)
- **Purpose**: Domain services, configuration, and extensions
- **Target Framework**: .NET 6.0
- **Responsibilities**:
  - Business logic orchestration
  - Orleans configuration abstractions
  - Dependency injection setup

**Key Components**:
- `Services/`:
  - `IGoLService`: Service interface for Game of Life operations
  - `GoLService`: Implementation managing grain interactions
- `Configuration/`:
  - `ClusterConfig`: Cluster configuration model
  - `ClientConfig`: Client configuration
  - `ClusterEndpointOptions`: Network endpoint configuration
- `Extensions/`:
  - `SiloHostBuilderExtensions`: Orleans silo configuration
  - `ServiceCollectionExtensions`: DI container setup

#### 3. **Sinapsa.GoL.DistOrleansProc.GrainInterfaces** (Contracts)
- **Purpose**: Grain contracts and data models
- **Target Framework**: .NET 6.0
- **Responsibilities**:
  - Defining grain interfaces
  - Data transfer objects
  - Shared models

**Key Components**:
- `IGoLChunkGrain`: Interface for Game of Life chunk grain
- `Models/`:
  - `Cell`: Represents individual cell state
  - `GoLChunkGrainState`: Persistent state for chunk grain
- `Services/`:
  - `TwoDimensionalIntArrayJsonConverter`: JSON serialization support

#### 4. **Sinapsa.GoL.DistOrleansProc.Grains** (Implementation)
- **Purpose**: Orleans grain implementations
- **Target Framework**: .NET 6.0
- **Responsibilities**:
  - Implementing grain logic
  - Managing cell state
  - Computing Game of Life rules

**Key Components**:
- `GoLChunkGrain`: Implements the Game of Life logic for a chunk of cells
  - Manages a 2D grid of cells (configurable size)
  - Connects neighboring cells
  - Applies Conway's Game of Life rules
  - Persists state using memory storage

#### 5. **Sinapsa.GoL.DistOrleansProc.Orleans.Core** (Infrastructure)
- **Purpose**: Orleans infrastructure abstractions
- **Target Framework**: .NET Standard 2.0 (for broader compatibility)
- **Responsibilities**:
  - Generic grain factory patterns
  - Grain pooling infrastructure
  - Common Orleans utilities

**Key Components**:
- `IGrainFactoryOfT<TGrain, TGrainIdentity>`: Generic grain factory interface
- `Services/`:
  - `GrainFactoryWithStringIdentity<T>`: String-keyed grain factory
  - `GrainFactoryWithGuidIdentity<T>`: GUID-keyed grain factory
  - `GrainFactoryWithIntegerIdentity<T>`: Integer-keyed grain factory
  - `IPooledGrainFactoryOfT`: Grain pooling support
  - `IClock`: Time abstraction
  - `IRentedGrain`: Pooled grain lifecycle management

## Key Design Patterns

### 1. **Actor Model (Orleans Grains)**
- Each `GoLChunkGrain` is an independent actor managing a portion of the game grid
- Grains are activated on-demand and automatically deactivated when idle
- State persistence through Orleans storage providers

### 2. **Generic Grain Factories**
- Type-safe grain access through `IGrainFactory<TGrain, TGrainIdentity>`
- Support for multiple identity types (string, GUID, integer)
- Simplified grain retrieval and management

### 3. **Dependency Injection**
- Services registered using ASP.NET Core DI container
- Grain factories injected into services
- Configuration-driven behavior

### 4. **Configuration-Based Clustering**
- Flexible cluster configuration through `appsettings.json`
- Support for localhost, Kubernetes, and custom hosting
- Optional Orleans Dashboard integration

## Data Flow

### Initialization Flow
```
HTTP GET /warmup
    ?
WarmUpController
    ?
IGoLService.InitChunk()
    ?
IGrainFactory<IGoLChunkGrain, string>.GetGrain("myBallzz")
    ?
GoLChunkGrain.InitRandomChunk(32, 32, 0.15)
    ?
- Create 32x32 cell grid
- Randomly initialize cells (15% alive)
- Connect neighboring cells
- Persist state to memory storage
```

### Simulation Step Flow
```
HTTP GET /step
    ?
StepController
    ?
IGoLService.RunUniverseStep()
    ?
GoLChunkGrain.Advance()
    ?
For each cell:
  - Count live neighbors
  - Apply Conway's rules:
    * Live cell: 2-3 neighbors ? survives
    * Live cell: <2 or >3 neighbors ? dies
    * Dead cell: 3 neighbors ? becomes alive
    ?
Update all cells to next state
    ?
Persist state
    ?
IGoLService.DisplayCurrentState()
    ?
Return ASCII representation
```

## Conway's Game of Life Rules Implementation

The simulation implements the classic Conway's Game of Life rules:

1. **Underpopulation**: Any live cell with fewer than 2 live neighbors dies
2. **Survival**: Any live cell with 2-3 live neighbors survives
3. **Overpopulation**: Any live cell with more than 3 live neighbors dies
4. **Reproduction**: Any dead cell with exactly 3 live neighbors becomes alive

Implementation location: `GoLChunkGrain.Advance()` method

```csharp
int liveNeighbors = currentState.Cells[w, h].neighbors.Count(x => x.IsAlive);

if (currentState.Cells[w, h].IsAlive)
    currentState.Cells[w, h].IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
else
    currentState.Cells[w, h].IsAliveNext = liveNeighbors == 3;
```

## Storage Architecture

### Memory Storage
- **Provider**: In-memory grain storage
- **Configuration**: Automatic setup based on `[StorageProvider]` attributes
- **Persistence**: `ChunkMemory` storage provider for `GoLChunkGrain`
- **Scope**: Per-silo (data lost on silo restart)

### State Management
- `GoLChunkGrainState`: Contains cell grid and metadata
  - `Cells[,]`: 2D array of Cell objects
  - `Width`, `Height`: Grid dimensions
  - `ChunkId`, `ChunkLocationX`, `ChunkLocationY`: Chunk identification

## Clustering & Deployment

### Local Development
```json
{
  "ClusterConfig": {
    "UseLocalhost": true,
    "UseDashboard": true,
    "DashboardPort": 8000
  }
}
```

### Kubernetes Deployment
```json
{
  "ClusterConfig": {
    "UseLocalhost": false,
    "UseKubernetesHosting": true,
    "UseDashboard": true
  }
}
```

### Cluster Configuration Options
- **ClusterId**: Unique identifier for the Orleans cluster
- **ServiceId**: Service identification for grain compatibility
- **SiloPort**: Inter-silo communication port (default: 30000)
- **GatewayPort**: Client-to-silo communication port (default: 11111)
- **Dashboard**: Optional web-based monitoring UI (port 8000)

## Observability

### Orleans Dashboard
- **Access**: `http://localhost:8000/dashboard`
- **Features**:
  - Real-time grain statistics
  - Cluster membership status
  - Silo performance metrics
  - Grain activation/deactivation tracking

### Health Checks
- **Endpoint**: `/hc`
- **Purpose**: Kubernetes liveness/readiness probes

### Logging
- ASP.NET Core logging infrastructure
- Console logging provider
- Configurable log levels through `appsettings.json`

## Extensibility Points

### 1. Multi-Chunk Support
Current implementation uses a single chunk. The architecture supports:
- Multiple grains for different grid regions
- Cross-grain communication for edge cells
- Dynamic chunk creation/destruction

### 2. Alternative Storage Providers
Replace memory storage with:
- Azure Table Storage
- Azure Blob Storage
- SQL Server
- Redis
- Custom storage provider

### 3. Client Applications
- Web UI for visualization
- External clients using Orleans client
- Cross-platform clients (.NET, JavaScript)

### 4. Advanced Features
- Grid expansion/contraction
- Pattern libraries
- Time-travel (state history)
- Multi-player collaborative editing

## Performance Considerations

### Current Implementation
- **Grid Size**: Fixed 32x32 cells per chunk
- **Live Density**: 15% initial population
- **State Updates**: Synchronous, per-step persistence

### Optimization Opportunities
1. **Lazy State Persistence**: Only persist on deactivation
2. **Grain Pooling**: Reuse grain instances (infrastructure already present)
3. **Batch Updates**: Process multiple steps before persisting
4. **Edge Optimization**: Only activate edge cells when needed
5. **Stateless Worker Grains**: Separate compute from state

## Technology Stack

### Core Technologies
- **.NET 6.0**: Runtime and framework
- **Microsoft Orleans 3.6.5**: Actor framework
- **ASP.NET Core**: Web API hosting
- **Swagger/OpenAPI**: API documentation

### Dependencies
- `Microsoft.Orleans.OrleansRuntime`: Core Orleans functionality
- `Microsoft.Orleans.OrleansProviders`: Storage providers
- `OrleansDashboard`: Monitoring UI
- `Microsoft.Orleans.Hosting.Kubernetes`: K8s support
- `Swashbuckle.AspNetCore`: Swagger generation

## Security Considerations

### Current State
- No authentication/authorization
- Localhost/development configuration
- HTTP endpoints (non-TLS)

### Production Recommendations
1. Enable HTTPS
2. Implement API authentication (JWT, API keys)
3. Configure network policies for Orleans ports
4. Use secure storage providers with encryption
5. Implement rate limiting
6. Add request validation

## Future Architectural Improvements

### 1. Distributed Grid Management
- Implement grain-to-grain communication for edge cells
- Support infinite/expanding grids
- Load balancing across multiple silos

### 2. Event Sourcing
- Store state transitions as events
- Enable replay and time-travel debugging
- Audit trail for simulation history

### 3. CQRS Pattern
- Separate read and write models
- Optimize query performance
- Support real-time subscriptions

### 4. Streaming
- Orleans Streams for real-time updates
- SignalR integration for web clients
- Event-driven architecture

### 5. Persistence
- Snapshot + event log storage
- State compression for large grids
- Distributed caching layer

## References

- [Microsoft Orleans Documentation](https://docs.microsoft.com/en-us/dotnet/orleans/)
- [Conway's Game of Life](https://en.wikipedia.org/wiki/Conway%27s_Game_of_Life)
- [Actor Model](https://en.wikipedia.org/wiki/Actor_model)

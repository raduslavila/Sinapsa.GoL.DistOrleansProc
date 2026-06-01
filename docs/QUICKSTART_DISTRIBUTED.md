# Kubernetes Quickstart

## Prerequisites

- Docker Desktop with Kubernetes enabled (or Kind)
- `kubectl` configured to talk to your cluster
- PowerShell 7+ (`pwsh`)

## One-Command Deploy

From the repository root:

```powershell
.\deploy-kind.ps1
```

The script:
1. Detects whether the active `kubectl` context is Docker Desktop or Kind.
2. Builds the backend (`gol-backend:local`) and frontend (`gol-frontend:local`) Docker images.
3. Loads images into every cluster node (Docker Desktop multi-node: imports via `ctr`; Kind: uses `kind load`).
4. Applies all manifests in `k8s/` (CRDs, namespace, RBAC, Redis, RedisInsight, backend deployment, frontend deployment).
5. Waits for rollouts to complete.
6. Uses NodePort/Kind host mappings for local access.

### Script parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `-KindClusterName` | `gol` | Kind cluster name |
| `-SkipBuild` | off | Reuse existing local images |

## Access

After the script completes:

| Service | URL |
|---------|-----|
| Frontend | http://localhost:30000 |
| Backend API | http://localhost:30050/api |
| Swagger UI | http://localhost:30050/swagger |
| Orleans Dashboard | http://localhost:30050/dashboard |

## Verify the cluster

```powershell
kubectl get pods -n gol
# All 5 pods (4 backend + 1 frontend) should be Running

# Health check
Invoke-RestMethod http://localhost:30050/hc

# Initialize and step
Invoke-RestMethod -Uri "http://localhost:30050/api/init?chunksX=2&chunksY=2&chunkSize=32" -Method POST
Invoke-RestMethod -Uri "http://localhost:30050/api/step" -Method POST
```

## Re-deploying after a code change

```powershell
# Rebuild and redeploy (skip if images already rebuilt)
.\deploy-kind.ps1 -SkipBuild:$false

# OR: rebuild manually, then rolling restart
docker build -t gol-backend:local . -f .\Sinapsa.GoL.DistOrleansProc\Dockerfile
# Load into nodes (Docker Desktop multi-node):
$nodes = (kubectl get nodes -o jsonpath='{.items[*].metadata.name}') -split ' '
foreach ($n in $nodes) {
    cmd /c "docker save gol-backend:local | docker exec -i $n ctr --namespace k8s.io images import -"
}
kubectl rollout restart deployment/gol-backend -n gol
kubectl rollout status deployment/gol-backend  -n gol --timeout=180s
```

## Kubernetes manifests (`k8s/`)

| File | Contents |
|------|----------|
| `crds.yaml` | Orleans clustering CRDs (`silos.orleans.dot.net`, `clusterversions.orleans.dot.net`) |
| `namespace.yaml` | Namespace `gol` |
| `rbac.yaml` | ServiceAccount + Role with CRD read/write access |
| `redis.yaml` | Redis Deployment + ClusterIP Service |
| `redis-insight.yaml` | RedisInsight Deployment + NodePort Service (`30054`) |
| `backend.yaml` | 4-replica `gol-backend` Deployment + NodePort Service (`gol-backend-external`, port 30050→5050) |
| `frontend.yaml` | 1-replica `gol-frontend` Deployment + NodePort Service (`gol-frontend`, port 30000→80) |

## Notes

- NodePort services are exposed on `localhost` through the Kind host mappings in `kind-config.yaml`.
- `ClusterId` must be lowercase (`k8s-gol`) to satisfy RFC 1123 / Kubernetes CRD naming rules.
- `imagePullPolicy: IfNotPresent` — a rebuilt image must be explicitly loaded into each node before a rollout picks it up.
- The Orleans Dashboard (`/dashboard`) is served by the backend pods on the same 5050 NodePort as the API.

#!/usr/bin/env pwsh
<#
.SYNOPSIS
    One-command deploy of Game of Life (Orleans silos + React frontend) to a local Kubernetes cluster.

.DESCRIPTION
    Supports both Docker Desktop Kubernetes and Kind clusters.

    Steps:
    1. Verifies prerequisites (docker, kubectl).
    2. Detects the cluster type (Docker Desktop or Kind).
       - Kind: also checks/creates cluster and loads images with 'kind load'.
       - Docker Desktop: images from 'docker build' are immediately available.
    3. Builds Docker images for the .NET backend and React frontend.
    4. Applies all Kubernetes manifests (namespace, RBAC, Redis, RedisInsight, backend x4, frontend x1).
    5. Waits for rollouts to complete.
    6. Prints access URLs.

.PARAMETER KindClusterName
    Name of the Kind cluster to create/use when running with Kind. Default: "gol"

.PARAMETER SkipBuild
    Skip docker build steps (reuse existing local images).

.PARAMETER ForcePortForward
    Always start port-forward jobs, even on Docker Desktop.
#>
param(
    [string]$KindClusterName = "gol",
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ROOT           = $PSScriptRoot
$BACKEND_IMAGE  = "gol-backend:local"
$FRONTEND_IMAGE = "gol-frontend:local"
$NAMESPACE      = "gol"

function Write-Step { param($msg) Write-Host "`n==> $msg" -ForegroundColor Cyan }
function Write-Ok   { param($msg) Write-Host "    OK  $msg" -ForegroundColor Green }
function Write-Warn { param($msg) Write-Host "    WARN $msg" -ForegroundColor Yellow }

# ── 1. Prerequisite check ─────────────────────────────────────────────────────
Write-Step "Checking prerequisites"
foreach ($tool in @("docker", "kubectl")) {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
        Write-Error "$tool is not installed or not on PATH. Aborting."
        exit 1
    }
    Write-Ok "$tool found"
}

# ── 2. Detect cluster type ────────────────────────────────────────────────────
Write-Step "Detecting Kubernetes cluster"
$currentContext  = kubectl config current-context 2>&1
Write-Ok "Active context: $currentContext"

$isDockerDesktop = $currentContext -eq "docker-desktop"
$isKind          = $currentContext -like "kind-*"

if ($isDockerDesktop) {
    Write-Ok "Docker Desktop Kubernetes — locally-built images are immediately available"
} elseif ($isKind) {
    if (-not (Get-Command kind -ErrorAction SilentlyContinue)) {
        Write-Error "Context is '$currentContext' (Kind) but 'kind' CLI is not on PATH."
        Write-Error "Install Kind: https://kind.sigs.k8s.io/docs/user/quick-start/#installation"
        exit 1
    }
    Write-Ok "Kind cluster detected: $currentContext"
} else {
    Write-Warn "Unknown context '$currentContext'. Treating as generic cluster."
    Write-Warn "Ensure images '$BACKEND_IMAGE' and '$FRONTEND_IMAGE' are accessible from the cluster."
}

# ── 3. Ensure Kind cluster exists (Kind only) ─────────────────────────────────
if ($isKind) {
    $existingClusters = kind get clusters 2>&1
    if ($existingClusters -notcontains $KindClusterName) {
        Write-Step "Creating Kind cluster '$KindClusterName' with port mappings from kind-config.yaml"
        kind create cluster --name $KindClusterName --config "$ROOT\kind-config.yaml"
        kubectl config use-context "kind-$KindClusterName"
        Write-Ok "Cluster '$KindClusterName' created"
    } else {
        Write-Ok "Kind cluster '$KindClusterName' already exists"
        Write-Warn "If NodePort services are unreachable on localhost, recreate with port mappings:"
        Write-Warn "  kind delete cluster --name $KindClusterName"
        Write-Warn "  kind create cluster --name $KindClusterName --config kind-config.yaml"
    }
}

# ── 4. Build Docker images ────────────────────────────────────────────────────
if (-not $SkipBuild) {
    Write-Step "Building backend image ($BACKEND_IMAGE)"
    docker build -t $BACKEND_IMAGE -f "$ROOT\Sinapsa.GoL.DistOrleansProc\Dockerfile" $ROOT
    Write-Ok "Backend image built"

    Write-Step "Building frontend image ($FRONTEND_IMAGE)"
    docker build -t $FRONTEND_IMAGE -f "$ROOT\frontend\gol-visualizer\Dockerfile" "$ROOT\frontend\gol-visualizer"
    Write-Ok "Frontend image built"
} else {
    Write-Warn "Skipping image build (-SkipBuild)"
}

# ── 5. Load images into cluster nodes ────────────────────────────────────────
if ($isKind) {
    Write-Step "Loading images into Kind cluster '$KindClusterName'"
    kind load docker-image $BACKEND_IMAGE  --name $KindClusterName
    kind load docker-image $FRONTEND_IMAGE --name $KindClusterName
    Write-Ok "Images loaded into Kind"
} elseif ($isDockerDesktop) {
    # Docker Desktop multi-node: worker nodes run as Docker containers and do NOT
    # share the host daemon's image store — must import explicitly via ctr.
    Write-Step "Loading images into Docker Desktop worker nodes"
    $ddNodes = kubectl get nodes -o jsonpath='{.items[*].metadata.name}' | ForEach-Object { $_ -split ' ' }
    foreach ($node in $ddNodes) {
        Write-Host "    Loading $BACKEND_IMAGE  -> $node"
        cmd /c "docker save $BACKEND_IMAGE | docker exec -i $node ctr --namespace k8s.io images import -"
        Write-Host "    Loading $FRONTEND_IMAGE -> $node"
        cmd /c "docker save $FRONTEND_IMAGE | docker exec -i $node ctr --namespace k8s.io images import -"
    }
    Write-Ok "Images loaded into all Docker Desktop nodes"
}

# ── 6. Apply Kubernetes manifests ─────────────────────────────────────────────
Write-Step "Applying Kubernetes manifests"
kubectl apply -f "$ROOT\k8s\crds.yaml"         # Orleans.Clustering.Kubernetes CRDs (cluster-scoped)
kubectl apply -f "$ROOT\k8s\namespace.yaml"
kubectl apply -f "$ROOT\k8s\rbac.yaml"
kubectl apply -f "$ROOT\k8s\redis.yaml"
kubectl apply -f "$ROOT\k8s\redis-insight.yaml"
kubectl apply -f "$ROOT\k8s\backend.yaml"
kubectl apply -f "$ROOT\k8s\frontend.yaml"
Write-Ok "Manifests applied"

# ── 7. Wait for rollouts ──────────────────────────────────────────────────────
Write-Step "Waiting for backend rollout (4 Orleans silo replicas)..."
kubectl rollout status deployment/gol-backend  -n $NAMESPACE --timeout=180s

Write-Step "Waiting for frontend rollout..."
kubectl rollout status deployment/gol-frontend -n $NAMESPACE --timeout=90s

Write-Step "Waiting for Redis rollout..."
kubectl rollout status deployment/redis -n $NAMESPACE --timeout=90s

Write-Step "Waiting for RedisInsight rollout..."
kubectl rollout status deployment/redis-insight -n $NAMESPACE --timeout=90s

Write-Ok "All deployments ready"
# ── 8. Summary ────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║       Game of Life — deployed to local Kubernetes        ║" -ForegroundColor Magenta
Write-Host "╠══════════════════════════════════════════════════════════╣" -ForegroundColor Magenta
Write-Host "║  Frontend        http://localhost:30000                  ║" -ForegroundColor Magenta
Write-Host "║  Backend API     http://localhost:30050/api              ║" -ForegroundColor Magenta
Write-Host "║  Orleans dash    http://localhost:30050/dashboard        ║" -ForegroundColor Magenta
Write-Host "║  RedisInsight    http://localhost:30054                  ║" -ForegroundColor Magenta
Write-Host "╠══════════════════════════════════════════════════════════╣" -ForegroundColor Magenta
Write-Host "║  backend: 4 Orleans silo replicas | frontend: 1 replica  ║" -ForegroundColor Magenta
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Magenta
Write-Host ""
Write-Host "Useful commands:" -ForegroundColor Yellow
Write-Host "  kubectl get pods -n gol"
Write-Host "  kubectl logs -l app=gol-backend  -n gol --tail=50 -f"
Write-Host "  kubectl logs -l app=gol-frontend -n gol --tail=50"
Write-Host ""
Write-Host "Tear down:"
Write-Host "  kubectl delete namespace gol                  # removes all resources"
if ($isKind) {
    Write-Host "  kind delete cluster --name $KindClusterName  # removes the cluster entirely"
}

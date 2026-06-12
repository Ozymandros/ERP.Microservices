#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Optional stage-test deploy of ERP manifests to local Minikube (not production).

.DESCRIPTION
  - Starts Minikube (Docker driver) with enough RAM for SQL Server
  - Installs ingress-nginx, cert-manager, Dapr via Helm
  - Applies deploy/k8s/overlays/minikube (or minikube-core) with dev secrets
  - Builds service images into Minikube's Docker daemon

.EXAMPLE
  .\scripts\deploy-minikube-stage.ps1

.EXAMPLE
  .\scripts\deploy-minikube-stage.ps1 -Profile Core -SkipBuild

.EXAMPLE
  .\scripts\deploy-minikube-stage.ps1 -Teardown
#>
[CmdletBinding()]
param(
    [ValidateSet('Full', 'Core')]
    [string] $Profile = 'Full',

    [switch] $Teardown,
    [switch] $SkipMinikubeStart,
    [switch] $SkipHelm,
    [switch] $SkipBuild,
    [switch] $SkipApply,
    [switch] $SkipWait,

    [string] $MinikubeProfile = 'minikube',
    [int] $MinikubeMemoryMb = 12288,
    [int] $MinikubeCpus = 4,
    [string] $ImageTag = 'minikube-stage',
    [string[]] $Services,

    [int] $SqlReadyTimeoutSec = 600,
    [int] $AppReadyTimeoutSec = 900
)

$ErrorActionPreference = 'Stop'

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$OverlayFull = Join-Path $RepoRoot 'deploy/k8s/overlays/minikube'
$OverlayCore = Join-Path $RepoRoot 'deploy/k8s/overlays/minikube-core'
$DaprComponentsDir = Join-Path $RepoRoot 'deploy/k8s/base/dapr/components'

$K8sServicesFull = @(
    'auth-service', 'billing-service', 'inventory-service', 'orders-service',
    'purchasing-service', 'sales-service', 'crm-service', 'gateway'
)
$K8sServicesCore = @('auth-service', 'gateway')

$ServiceDockerfiles = @{
    'auth-service'      = 'src/MyApp.Auth/MyApp.Auth.API/Dockerfile'
    'billing-service'   = 'src/MyApp.Billing/MyApp.Billing.API/Dockerfile'
    'inventory-service' = 'src/MyApp.Inventory/MyApp.Inventory.API/Dockerfile'
    'orders-service'    = 'src/MyApp.Orders/MyApp.Orders.API/Dockerfile'
    'purchasing-service'= 'src/MyApp.Purchasing/MyApp.Purchasing.API/Dockerfile'
    'sales-service'     = 'src/MyApp.Sales/MyApp.Sales.API/Dockerfile'
    'crm-service'       = 'src/MyApp.Crm/MyApp.Crm.API/Dockerfile'
    'gateway'           = 'src/ErpApiGateway/Dockerfile'
}

function Write-Title([string] $Text) {
    Write-Host ""
    Write-Host "=== $Text ===" -ForegroundColor Cyan
}

function Write-Ok([string] $Text) { Write-Host "  OK  $Text" -ForegroundColor Green }
function Write-Warn2([string] $Text) { Write-Host "  !!  $Text" -ForegroundColor Yellow }
function Write-Err([string] $Text) { Write-Host "  XX  $Text" -ForegroundColor Red }

function Assert-Command([string] $Name) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command not found on PATH: $Name"
    }
}

function Invoke-Checked {
    param(
        [scriptblock] $Block,
        [string] $FailureMessage
    )
    & $Block
    if ($LASTEXITCODE -ne 0) {
        throw $FailureMessage
    }
}

function Get-OverlayPath {
    if ($Profile -eq 'Core') { return $OverlayCore }
    return $OverlayFull
}

function Get-ServicesToBuild {
    $catalog = if ($Profile -eq 'Core') { $K8sServicesCore } else { $K8sServicesFull }
    if ($Services -and $Services.Count -gt 0) {
        $catalog = $Services | ForEach-Object { $_.Trim() }
        foreach ($s in $catalog) {
            if (-not $ServiceDockerfiles.ContainsKey($s)) {
                throw "Unknown service '$s'. Valid: $($ServiceDockerfiles.Keys -join ', ')"
            }
        }
    }
    return $catalog
}

function Install-HelmRelease {
    param(
        [string] $Release,
        [string] $Chart,
        [string] $Namespace,
        [string[]] $Set = @()
    )
    $nsExists = kubectl get namespace $Namespace -o name 2>$null
    if (-not $nsExists) {
        kubectl create namespace $Namespace | Out-Null
    }

    $helmArgs = @(
        'upgrade', '--install', $Release, $Chart,
        '--namespace', $Namespace,
        '--create-namespace',
        '--wait', '--timeout', '10m'
    )
    foreach ($s in $Set) {
        $helmArgs += '--set', $s
    }

    Write-Host "  helm $($helmArgs -join ' ')" -ForegroundColor DarkGray
    helm @helmArgs
    if ($LASTEXITCODE -ne 0) { throw "Helm install failed: $Release" }
}

function Use-MinikubeDockerEnv {
    $envLines = minikube -p $MinikubeProfile docker-env --shell powershell 2>&1
    if ($LASTEXITCODE -ne 0) { throw "minikube docker-env failed: $envLines" }
    foreach ($line in $envLines) {
        if ($line -match '^\s*#\s*') { continue }
        if ($line -match '^\s*$') { continue }
        Invoke-Expression $line
    }
}

function Build-StageImages {
    param([string[]] $Names)

    Write-Title "Build images (tag: $ImageTag)"
    Push-Location $RepoRoot
    try {
        $env:DOCKER_BUILDKIT = '1'
        Use-MinikubeDockerEnv

        $base = docker images myapp-microservices-base:10.0 --format '{{.Repository}}:{{.Tag}}' 2>$null
        if (-not ($base -match 'myapp-microservices-base:10.0')) {
            Write-Host "  Building shared base image..." -ForegroundColor Yellow
            docker build -f docker/microservices-base.Dockerfile -t myapp-microservices-base:10.0 .
            if ($LASTEXITCODE -ne 0) { throw 'Base image build failed' }
        }
        else {
            Write-Ok 'Base image myapp-microservices-base:10.0 already present'
        }

        foreach ($name in $Names) {
            $dockerfile = $ServiceDockerfiles[$name]
            $image = "myapp-$name`:$ImageTag"
            Write-Host "  Building $image ..." -ForegroundColor Yellow
            docker build -f $dockerfile -t $image .
            if ($LASTEXITCODE -ne 0) { throw "Docker build failed for $name" }
            Write-Ok $image
        }
    }
    finally {
        Pop-Location
    }
}

function Wait-SqlReady {
    Write-Title "Wait for SQL Server"
    $deadline = (Get-Date).AddSeconds($SqlReadyTimeoutSec)
    while ((Get-Date) -lt $deadline) {
        $phase = kubectl get pod -n myapp-apps -l app.kubernetes.io/name=sqlserver -o jsonpath='{.items[0].status.phase}' 2>$null
        $ready = kubectl get pod -n myapp-apps -l app.kubernetes.io/name=sqlserver -o jsonpath='{.items[0].status.conditions[?(@.type=="Ready")].status}' 2>$null
        if ($phase -eq 'Running' -and $ready -eq 'True') {
            Write-Ok 'SQL Server pod is ready'
            return
        }
        Write-Host "  SQL pod phase=$phase ready=$ready — waiting..." -ForegroundColor DarkGray
        Start-Sleep -Seconds 10
    }
    throw "SQL Server not ready within ${SqlReadyTimeoutSec}s. Check: kubectl describe pod -n myapp-apps -l app.kubernetes.io/name=sqlserver"
}

function Wait-BootstrapJob {
    Write-Title "Wait for database bootstrap job"
    $job = 'sql-bootstrap-databases'
    $deadline = (Get-Date).AddSeconds(300)
    while ((Get-Date) -lt $deadline) {
        $succeeded = kubectl get job $job -n myapp-apps -o jsonpath='{.status.succeeded}' 2>$null
        $failed = kubectl get job $job -n myapp-apps -o jsonpath='{.status.failed}' 2>$null
        if ($succeeded -eq '1') {
            Write-Ok 'Bootstrap job completed'
            return
        }
        if ($failed -and [int]$failed -ge 1) {
            kubectl logs -n myapp-apps "job/$job" --tail=80 2>$null
            throw 'Bootstrap job failed — see logs above'
        }
        Start-Sleep -Seconds 5
    }
    Write-Warn2 'Bootstrap job still running; apps may retry migrations until DBs exist'
}

function Wait-AppDeployments {
    param([string[]] $Names)
    Write-Title "Wait for application deployments"
    $deadline = (Get-Date).AddSeconds($AppReadyTimeoutSec)
    foreach ($name in $Names) {
        if ($name -eq 'gateway') { continue } # ingress + TLS can lag; checked separately
        while ((Get-Date) -lt $deadline) {
            $ready = kubectl get deploy $name -n myapp-apps -o jsonpath='{.status.readyReplicas}' 2>$null
            $desired = kubectl get deploy $name -n myapp-apps -o jsonpath='{.spec.replicas}' 2>$null
            if ($ready -eq $desired -and $ready -eq '1') {
                Write-Ok "$name ready"
                break
            }
            Write-Host "  $name ready=$ready/$desired ..." -ForegroundColor DarkGray
            Start-Sleep -Seconds 8
        }
    }
}

function Show-AccessInfo {
    Write-Title 'Stage-test access'
    $ip = minikube -p $MinikubeProfile ip 2>$null
    if ($LASTEXITCODE -ne 0) { $ip = '<minikube-ip>' }

    Write-Host @"

  Hosts file (Administrator may be required):
    $ip    gateway.local

  Gateway (self-signed TLS):
    https://gateway.local/health
    https://gateway.local/auth/api/...   (via Ocelot routes)

  If ingress is not reachable, port-forward:
    kubectl port-forward -n myapp-apps svc/gateway 8080:8080
    http://localhost:8080/health

  Aspire dashboard UI:
    kubectl port-forward -n myapp-platform svc/aspire-dashboard 18888:18888
    http://localhost:18888

  Useful commands:
    kubectl get pods -n myapp-apps
    kubectl get pods -n myapp-platform
    dapr list -k
    minikube dashboard

  DEV secrets only — see deploy/k8s/overlays/minikube/stage-dev-secrets.yaml

"@ -ForegroundColor Gray
}

# --- main ---

Write-Host ''
Write-Host 'ERP Minikube stage-test deploy' -ForegroundColor Cyan
Write-Host 'NOT FOR PRODUCTION — fixed dev passwords' -ForegroundColor Yellow
Write-Host ''

if ($Teardown) {
    $teardownScript = Join-Path $PSScriptRoot 'teardown-minikube-stage.ps1'
    & $teardownScript -MinikubeProfile $MinikubeProfile -Force
    exit $LASTEXITCODE
}

Assert-Command minikube
Assert-Command kubectl
Assert-Command helm
Assert-Command docker

$overlay = Get-OverlayPath
$toBuild = Get-ServicesToBuild
$appDeployments = $toBuild | Where-Object { $_ -ne 'gateway' }

if (-not $SkipMinikubeStart) {
    Write-Title "Minikube profile '$MinikubeProfile'"
    $status = minikube status -p $MinikubeProfile -f '{{.Host}}' 2>$null
    if ($status -ne 'Running') {
        Write-Host "  Starting Minikube (memory=${MinikubeMemoryMb}MB cpus=$MinikubeCpus)..." -ForegroundColor Yellow
        minikube start -p $MinikubeProfile `
            --driver=docker `
            --memory=$MinikubeMemoryMb `
            --cpus=$MinikubeCpus `
            --kubernetes-version=stable
        if ($LASTEXITCODE -ne 0) { throw 'minikube start failed' }
    }
    else {
        Write-Ok 'Minikube already running'
    }

    minikube -p $MinikubeProfile update-context | Out-Null
}

if (-not $SkipHelm) {
    Write-Title 'Helm dependencies'
    helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx 2>$null | Out-Null
    helm repo add jetstack https://charts.jetstack.io 2>$null | Out-Null
    helm repo add dapr https://dapr.github.io/helm-charts/ 2>$null | Out-Null
    helm repo update | Out-Null

    Install-HelmRelease -Release 'ingress-nginx' -Chart 'ingress-nginx/ingress-nginx' -Namespace 'ingress-nginx' -Set @(
        'controller.admissionWebhooks.enabled=false'
    )

    Install-HelmRelease -Release 'cert-manager' -Chart 'jetstack/cert-manager' -Namespace 'cert-manager' -Set @(
        'crds.enabled=true'
    )

    Install-HelmRelease -Release 'dapr' -Chart 'dapr/dapr' -Namespace 'dapr-system'
    Write-Ok 'Helm releases ready'
}

if (-not $SkipBuild) {
    Build-StageImages -Names $toBuild
}

if (-not $SkipApply) {
    Write-Title 'Validate kustomize overlay'
    kubectl kustomize $overlay | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "kustomize validation failed for $overlay" }
    Write-Ok "Overlay OK: $overlay"

    Write-Title 'Apply stage secrets and config (before Redis/SQL)'
    $secretsFile = Join-Path $RepoRoot 'deploy/k8s/overlays/minikube/stage-dev-secrets.yaml'
    $configFile = Join-Path $RepoRoot 'deploy/k8s/overlays/minikube/app-config.yaml'
    kubectl apply -f $secretsFile
    if ($LASTEXITCODE -ne 0) { throw 'secrets apply failed' }
    kubectl apply -f $configFile
    if ($LASTEXITCODE -ne 0) { throw 'app-config apply failed' }

    Write-Title 'Apply platform + SQL'
    kubectl apply -k (Join-Path $RepoRoot 'deploy/k8s/base/platform')
    if ($LASTEXITCODE -ne 0) { throw 'platform apply failed' }

    kubectl apply -k (Join-Path $RepoRoot 'deploy/k8s/base/sql')
    if ($LASTEXITCODE -ne 0) { throw 'sql apply failed' }

    if (-not $SkipWait) {
        Wait-SqlReady
    }

    Write-Title 'Apply Dapr components'
    Get-ChildItem $DaprComponentsDir -Filter '*.yaml' | ForEach-Object {
        kubectl apply -f $_.FullName
        if ($LASTEXITCODE -ne 0) { throw "Dapr component apply failed: $($_.Name)" }
    }

    Write-Title 'Apply Minikube overlay (apps + secrets + config)'
    kubectl apply -k $overlay
    if ($LASTEXITCODE -ne 0) { throw 'overlay apply failed' }

    if (-not $SkipWait) {
        Wait-BootstrapJob
        Wait-AppDeployments -Names $appDeployments
    }
}

Show-AccessInfo
Write-Ok 'Stage-test deploy finished'

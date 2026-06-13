#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Tear down the ERP Minikube stage-test stack (workloads, Dapr, Helm deps).

.DESCRIPTION
  Reverses what deploy-minikube-stage.ps1 applies:
  - Kustomize overlays (full + core)
  - Base platform/SQL (if still present)
  - Dapr components
  - Helm: dapr, cert-manager, ingress-nginx
  - Application namespaces (myapp-apps, myapp-platform)

  By default the Minikube VM/cluster is left running so you can redeploy quickly.
  Use -DeleteMinikube to destroy the cluster entirely.

.EXAMPLE
  .\scripts\teardown-minikube-stage.ps1

.EXAMPLE
  .\scripts\teardown-minikube-stage.ps1 -DeleteMinikube -Wait

.EXAMPLE
  .\scripts\teardown-minikube-stage.ps1 -KeepHelm
#>
[CmdletBinding()]
param(
    [string] $MinikubeProfile = 'minikube',

    [switch] $KeepHelm,
    [switch] $DeleteMinikube,
    [switch] $Wait,
    [switch] $Force,

    [int] $NamespaceWaitTimeoutSec = 180
)

$ErrorActionPreference = 'Stop'

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$OverlayFull = Join-Path $RepoRoot 'deploy/k8s/overlays/minikube'
$OverlayCore = Join-Path $RepoRoot 'deploy/k8s/overlays/minikube-core'
$PlatformKustomize = Join-Path $RepoRoot 'deploy/k8s/base/platform'
$SqlKustomize = Join-Path $RepoRoot 'deploy/k8s/base/sql'
$DaprComponentsDir = Join-Path $RepoRoot 'deploy/k8s/base/dapr/components'

$AppNamespaces = @('myapp-apps', 'myapp-platform')
$HelmNamespaces = @('dapr-system', 'ingress-nginx', 'cert-manager')

function Write-Title([string] $Text) {
    Write-Host ""
    Write-Host "=== $Text ===" -ForegroundColor Cyan
}

function Write-Ok([string] $Text) { Write-Host "  OK  $Text" -ForegroundColor Green }
function Write-Warn2([string] $Text) { Write-Host "  !!  $Text" -ForegroundColor Yellow }

function Assert-Command([string] $Name) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command not found on PATH: $Name"
    }
}

function Test-MinikubeRunning {
    $status = minikube status -p $MinikubeProfile -f '{{.Host}}' 2>$null
    return ($LASTEXITCODE -eq 0 -and $status -eq 'Running')
}

function Use-MinikubeContext {
    if (-not (Test-MinikubeRunning)) {
        Write-Warn2 "Minikube profile '$MinikubeProfile' is not running - skipping cluster operations"
        return $false
    }
    minikube -p $MinikubeProfile update-context 2>$null | Out-Null
    return $true
}

function Invoke-KubectlQuiet {
    param([Parameter(Mandatory)][string[]] $KubectlArgs)
    $prev = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    try {
        return & kubectl @KubectlArgs 2>$null
    }
    finally {
        $ErrorActionPreference = $prev
    }
}

function Invoke-KubectlDelete {
    param(
        [string] $Label,
        [scriptblock] $Action
    )
    try {
        & $Action
        # kubectl delete returns 0 even when resources are already gone
        Write-Ok $Label
    }
    catch {
        Write-Warn2 "$Label - $($_.Exception.Message)"
    }
}

function Remove-KustomizeOverlay {
    param([string] $Path)
    if (-not (Test-Path $Path)) { return }
    Invoke-KubectlDelete "delete -k $Path" {
        kubectl delete -k $Path --ignore-not-found --wait=false 2>$null | Out-Null
    }
}

function Remove-DaprComponents {
    if (-not (Test-Path $DaprComponentsDir)) { return }
    Get-ChildItem $DaprComponentsDir -Filter '*.yaml' | ForEach-Object {
        Invoke-KubectlDelete "delete Dapr component $($_.Name)" {
            kubectl delete -f $_.FullName --ignore-not-found --wait=false 2>$null | Out-Null
        }
    }
}

function Uninstall-HelmReleases {
    if (-not (Get-Command helm -ErrorAction SilentlyContinue)) {
        Write-Warn2 'helm not found - skipping Helm uninstall'
        return
    }

    $releases = @(
        @{ Release = 'dapr'; Namespace = 'dapr-system' },
        @{ Release = 'cert-manager'; Namespace = 'cert-manager' },
        @{ Release = 'ingress-nginx'; Namespace = 'ingress-nginx' }
    )

    foreach ($r in $releases) {
        $exists = helm list -n $r.Namespace -q 2>$null | Select-String -Pattern "^$($r.Release)$"
        if ($exists) {
            Write-Host "  helm uninstall $($r.Release) -n $($r.Namespace)" -ForegroundColor DarkGray
            helm uninstall $r.Release -n $r.Namespace 2>$null | Out-Null
            Write-Ok "Helm release $($r.Release) removed"
        }
    }
}

function Remove-Namespaces {
    param([string[]] $Names)

    foreach ($ns in $Names) {
        $exists = Invoke-KubectlQuiet -KubectlArgs @('get', 'namespace', $ns, '-o', 'name')
        if ($LASTEXITCODE -ne 0 -or -not $exists) { continue }

        Write-Host "  deleting namespace $ns ..." -ForegroundColor DarkGray
        if ($Wait) {
            Invoke-KubectlQuiet -KubectlArgs @(
                'delete', 'namespace', $ns, '--wait=true', "--timeout=${NamespaceWaitTimeoutSec}s"
            ) | Out-Null
        }
        else {
            Invoke-KubectlQuiet -KubectlArgs @('delete', 'namespace', $ns, '--wait=false') | Out-Null
        }
        Write-Ok "namespace $ns delete requested"
    }
}

function Wait-NamespacesGone {
    param([string[]] $Names)
    $deadline = (Get-Date).AddSeconds($NamespaceWaitTimeoutSec)
    foreach ($ns in $Names) {
        while ((Get-Date) -lt $deadline) {
            $exists = Invoke-KubectlQuiet -KubectlArgs @('get', 'namespace', $ns, '-o', 'name')
            if ($LASTEXITCODE -ne 0 -or -not $exists) {
                Write-Ok "namespace $ns terminated"
                break
            }
            Start-Sleep -Seconds 3
        }
        if ((Get-Date) -ge $deadline) {
            Write-Warn2 "namespace $ns still terminating after ${NamespaceWaitTimeoutSec}s"
        }
    }
}

# --- main ---

Write-Host ''
Write-Host 'ERP Minikube stage-test TEARDOWN' -ForegroundColor Cyan
Write-Host ''

if (-not $Force) {
    Write-Host 'This removes stage-test workloads and (by default) Helm dependencies.' -ForegroundColor Yellow
    Write-Host 'Minikube cluster stays unless you pass -DeleteMinikube.' -ForegroundColor Yellow
    $answer = Read-Host 'Continue? [y/N]'
    if ($answer -notmatch '^(y|yes)$') {
        Write-Host 'Cancelled.' -ForegroundColor Gray
        exit 0
    }
}

Assert-Command kubectl

$clusterAvailable = Use-MinikubeContext

if ($clusterAvailable) {
    Write-Title 'Remove application overlays'
    Remove-KustomizeOverlay -Path $OverlayFull
    Remove-KustomizeOverlay -Path $OverlayCore

    Write-Title 'Remove base platform and SQL (deploy applies these separately)'
    Remove-KustomizeOverlay -Path $SqlKustomize
    Remove-KustomizeOverlay -Path $PlatformKustomize

    Write-Title 'Remove Dapr components'
    Remove-DaprComponents

    if (-not $KeepHelm) {
        Write-Title 'Uninstall Helm releases'
        Uninstall-HelmReleases
    }
    else {
        Write-Warn2 'Keeping Helm releases (-KeepHelm)'
    }

    Write-Title 'Delete application namespaces'
    $allNs = $AppNamespaces
    if (-not $KeepHelm) {
        $allNs += $HelmNamespaces
    }
    Remove-Namespaces -Names $allNs

    if ($Wait) {
        Write-Title 'Wait for namespaces to terminate'
        Wait-NamespacesGone -Names $allNs
    }
}
else {
    Write-Warn2 'Cluster not available - only local Minikube delete may apply'
}

if ($DeleteMinikube) {
    Assert-Command minikube
    Write-Title "Delete Minikube profile '$MinikubeProfile'"
    minikube delete -p $MinikubeProfile
    if ($LASTEXITCODE -ne 0) {
        throw "minikube delete failed for profile $MinikubeProfile"
    }
    Write-Ok "Minikube profile '$MinikubeProfile' deleted"
}
else {
    Write-Host ''
    Write-Host '  Minikube cluster is still running. Redeploy with:' -ForegroundColor Gray
    Write-Host '    .\scripts\deploy-minikube-stage.ps1' -ForegroundColor Gray
    Write-Host ''
    Write-Host '  To destroy the VM/cluster completely:' -ForegroundColor Gray
    Write-Host "    .\scripts\teardown-minikube-stage.ps1 -DeleteMinikube -Force" -ForegroundColor Gray
}

Write-Ok 'Teardown finished'

#!/usr/bin/env pwsh
# ============================================================================
# DEPLOYMENT HELPER SCRIPT
# ============================================================================
# Purpose: Simple wrapper to build and deploy all services
# Usage: ./Deploy.ps1 or ./Deploy.ps1 -Environment prod
# ============================================================================

param(
    [ValidateSet('dev', 'staging', 'prod')]
    [string]$Environment = 'dev',
    
    [switch]$SkipBuild,
    [switch]$SkipDeploy,
    [switch]$Verify
)

$ErrorActionPreference = 'Stop'

# ============================================================================
# CONFIGURATION
# ============================================================================

Write-Host "═════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🚀 ERP MICROSERVICES - DEPLOYMENT SCRIPT" -ForegroundColor Cyan
Write-Host "═════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Environment: $Environment" -ForegroundColor Yellow
Write-Host "Resource Group: rg-myapp-$Environment-core" -ForegroundColor Yellow
Write-Host "Container Registry: ghcr.io" -ForegroundColor Yellow
Write-Host ""

# ============================================================================
# PHASE 1: BUILD IMAGES
# ============================================================================

if (-not $SkipBuild) {
    Write-Host "PHASE 1: Building Docker Images" -ForegroundColor Cyan
    Write-Host "─────────────────────────────────────────────────────────────────" -ForegroundColor Cyan
    # Set environment variables for build script
    $env:AZURE_CONTAINER_REGISTRY_ENDPOINT = "ghcr.io/$($env:GITHUB_REPOSITORY)"
    $env:GHCR_USERNAME = $env:GITHUB_ACTOR
    $env:GHCR_PAT = $env:GITHUB_TOKEN
    $env:AZURE_ENV_NAME = $Environment
    # Run build script
    $BuildScript = Join-Path $PSScriptRoot 'infra/scripts/build-push-images.ps1'
    if (Test-Path $BuildScript) {
        & $BuildScript
    } else {
        Write-Host "❌ Build script not found: $BuildScript" -ForegroundColor Red
        exit 1
    }
    Write-Host ""
}

# ============================================================================
# PHASE 2: DEPLOY WITH AZD
# ============================================================================

if (-not $SkipDeploy) {
    Write-Host "PHASE 2: Deploying Infrastructure with AZD" -ForegroundColor Cyan
    Write-Host "─────────────────────────────────────────────────────────────────" -ForegroundColor Cyan
    
    $env:AZURE_ENV_NAME = $Environment
    $env:AZURE_LOCATION = "westeurope"
    
    Write-Host "Step 2.1: Previewing infrastructure changes..." -ForegroundColor Yellow
    azd provision --preview 2>&1 | Out-Host
    if ($LASTEXITCODE -ne 0) {
        Write-Host "⚠ Preview completed with warnings (continuing with deployment)" -ForegroundColor Yellow
    } else {
        Write-Host "✓ Preview completed successfully" -ForegroundColor Green
    }
    Write-Host ""
    
    Write-Host "Step 2.2: Provisioning infrastructure..." -ForegroundColor Yellow
    azd provision --no-prompt
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Provisioning failed" -ForegroundColor Red
        exit 1
    }
    Write-Host ""
    
    Write-Host "Step 2.3: Deploying application..." -ForegroundColor Yellow
    azd deploy --no-prompt
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Deployment failed" -ForegroundColor Red
        exit 1
    }
    
    Write-Host ""
}

# ============================================================================
# PHASE 3: VERIFICATION
# ============================================================================

if ($Verify -or ($SkipBuild -eq $false -and $SkipDeploy -eq $false)) {
    Write-Host "PHASE 3: Verifying Deployment" -ForegroundColor Cyan
    Write-Host "─────────────────────────────────────────────────────────────────" -ForegroundColor Cyan
    
    $Services = @(
        'auth-service',
        'billing-service',
        'inventory-service',
        'orders-service',
        'purchasing-service',
        'sales-service',
        'api-gateway'
    )
    
    Write-Host ""
    Write-Host "Services Status:" -ForegroundColor Yellow
    foreach ($Service in $Services) {
        $ServiceName = "myapp-$Environment-$Service"
        try {
            $App = az containerapp show `
                --name $ServiceName `
                --resource-group rg-myapp-$Environment-core `
                --query "properties.latestRevisionFqdn" -o tsv 2>/dev/null
            
            if ($App) {
                Write-Host "  ✓ $ServiceName" -ForegroundColor Green
                Write-Host "    FQDN: $App" -ForegroundColor Gray
            } else {
                Write-Host "  ⚠ $ServiceName - Not found" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "  ⚠ $ServiceName - Error" -ForegroundColor Yellow
        }
    }
    
    Write-Host ""
}

# ============================================================================
# COMPLETION
# ============================================================================

Write-Host ""
Write-Host "═════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ DEPLOYMENT COMPLETE" -ForegroundColor Green
Write-Host "═════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Check Azure Portal for Container App status"
Write-Host "  2. Test service endpoints at:"
Write-Host "     https://myapp-$Environment-auth-service.<region>.containerapp.io"
Write-Host "  3. Monitor logs in Application Insights"
Write-Host ""
Write-Host "Common Commands:" -ForegroundColor Cyan
    Write-Host "  View logs:       az containerapp logs show --name myapp-$Environment-auth-service --resource-group rg-myapp-$Environment-core"
    Write-Host "  Check status:    az containerapp show --name myapp-$Environment-auth-service --resource-group rg-myapp-$Environment-core"
    Write-Host "  List revisions:  az containerapp revision list --name myapp-$Environment-auth-service --resource-group rg-myapp-$Environment-core"
Write-Host ""

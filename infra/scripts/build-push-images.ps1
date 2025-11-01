#!/usr/bin/env pwsh
# ============================================================================
# Azure DevOps Build & Push Images Script
# ============================================================================
# Purpose: Build all microservice Docker images and push to Azure Container Registry
# Called by: azure.yaml postprovision hook
# Input: Bicep deployment outputs (registry name, resource group, etc.)
# Output: Images pushed to ACR, Container Apps ready for deployment
# ============================================================================

param(
    [string]$ResourceGroupName = $env:AZURE_RESOURCE_GROUP,
    [string]$RegistryName = $env:AZURE_CONTAINER_REGISTRY_NAME,
    [string]$EnvironmentName = $env:AZURE_ENV_NAME,
    [string]$SourceDirectory = (Get-Item -Path "$PSScriptRoot/../../" -Force).FullName
)

# ============================================================================
# CONFIGURATION
# ============================================================================

$ErrorActionPreference = 'Stop'
$VerbosePreference = 'Continue'

# Service configuration: (ServicePath, Dockerfile, ImageName)
$Services = @(
    @{ Path = 'MyApp.Auth/MyApp.Auth.API'; Dockerfile = 'Dockerfile'; ImageName = 'auth-service' }
    @{ Path = 'MyApp.Billing/MyApp.Billing.API'; Dockerfile = 'Dockerfile'; ImageName = 'billing-service' }
    @{ Path = 'MyApp.Inventory/MyApp.Inventory.API'; Dockerfile = 'Dockerfile'; ImageName = 'inventory-service' }
    @{ Path = 'MyApp.Orders/MyApp.Orders.API'; Dockerfile = 'Dockerfile'; ImageName = 'orders-service' }
    @{ Path = 'MyApp.Purchasing/MyApp.Purchasing.API'; Dockerfile = 'Dockerfile'; ImageName = 'purchasing-service' }
    @{ Path = 'MyApp.Sales/MyApp.Sales.API'; Dockerfile = 'Dockerfile'; ImageName = 'sales-service' }
    @{ Path = 'ErpApiGateway'; Dockerfile = 'Dockerfile'; ImageName = 'erp-api-gateway' }
)

# Image tag (use commit hash for unique tags in production)
$ImageTag = if ($env:BUILD_SOURCEVERSION) { $env:BUILD_SOURCEVERSION.Substring(0, 7) } else { 'latest' }

# ============================================================================
# HELPER FUNCTIONS
# ============================================================================

function Write-Header {
    param([string]$Message)
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host $Message -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
}

function Write-Step {
    param([string]$Message)
    Write-Host "→ $Message" -ForegroundColor Yellow
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

# ============================================================================
# VALIDATION
# ============================================================================

Write-Header "🔍 VALIDATING ENVIRONMENT"

if (-not $ResourceGroupName) {
    Write-Error "ResourceGroupName is required. Set AZURE_RESOURCE_GROUP environment variable."
    exit 1
}

if (-not $RegistryName) {
    Write-Error "RegistryName is required. Set AZURE_CONTAINER_REGISTRY_NAME environment variable."
    exit 1
}

if (-not (Test-Path $SourceDirectory)) {
    Write-Error "Source directory not found: $SourceDirectory"
    exit 1
}

Write-Step "ResourceGroup: $ResourceGroupName"
Write-Step "Registry: $RegistryName"
Write-Step "Environment: $EnvironmentName"
Write-Step "Source Directory: $SourceDirectory"
Write-Step "Image Tag: $ImageTag"
Write-Success "Environment validation passed"

# ============================================================================
# LOGIN TO ACR
# ============================================================================

Write-Header "🔐 AUTHENTICATING TO AZURE CONTAINER REGISTRY"

Write-Step "Logging in to ACR: $RegistryName..."
$AcrLoginOutput = az acr login --name $RegistryName 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to login to ACR: $AcrLoginOutput"
    exit 1
}
Write-Success "Successfully authenticated to ACR"

$RegistryUri = "$RegistryName.azurecr.io"
Write-Step "Registry URI: $RegistryUri"

# ============================================================================
# BUILD & PUSH IMAGES
# ============================================================================

Write-Header "🐳 BUILDING & PUSHING DOCKER IMAGES"

$SuccessCount = 0
$FailureCount = 0
$FailedServices = @()

foreach ($Service in $Services) {
    $ServicePath = $Service.Path
    $Dockerfile = $Service.Dockerfile
    $ImageName = $Service.ImageName
    $FullImageName = "$RegistryUri/$ImageName`:$ImageTag"
    
    Write-Host ""
    Write-Step "Building: $ImageName"
    Write-Host "  Service Path: $ServicePath"
    Write-Host "  Image URI: $FullImageName"
    
    $ServiceFullPath = Join-Path $SourceDirectory $ServicePath
    if (-not (Test-Path $ServiceFullPath)) {
        Write-Error "Service directory not found: $ServiceFullPath"
        $FailureCount++
        $FailedServices += $ImageName
        continue
    }
    
    # Build using ACR (faster, parallel builds, cached layers)
    Write-Host "  Submitting build to ACR..." -ForegroundColor Gray
    $BuildOutput = az acr build `
        --registry $RegistryName `
        --image "$ImageName`:$ImageTag" `
        --file "$Dockerfile" `
        $ServiceFullPath 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to build $ImageName"
        Write-Host $BuildOutput -ForegroundColor Red
        $FailureCount++
        $FailedServices += $ImageName
        continue
    }
    
    Write-Success "Successfully built and pushed $ImageName`:$ImageTag"
    $SuccessCount++
}

# ============================================================================
# SUMMARY
# ============================================================================

Write-Header "📊 BUILD SUMMARY"

Write-Host ""
Write-Host "Total Services: $($Services.Count)" -ForegroundColor Cyan
Write-Host "Successful: $SuccessCount" -ForegroundColor Green
Write-Host "Failed: $FailureCount" -ForegroundColor $(if ($FailureCount -gt 0) { 'Red' } else { 'Green' })

if ($FailureCount -gt 0) {
    Write-Host ""
    Write-Host "Failed services:" -ForegroundColor Red
    $FailedServices | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    Write-Host ""
    Write-Error "Build completed with errors. Please review the logs above."
    exit 1
}

Write-Host ""
Write-Success "All images built and pushed successfully!"
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Container Apps will pull images with tag: $ImageTag"
Write-Host "  2. Deployments will be updated automatically"
Write-Host "  3. Services will be available at their configured FQDNs"
Write-Host ""

exit 0

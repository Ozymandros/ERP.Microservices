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
    [string]$RegistryEndpoint = $env:AZURE_CONTAINER_REGISTRY_ENDPOINT,
    [string]$GHCRUsername = $env:GHCR_USERNAME,
    [string]$GHCRPat = $env:GHCR_PAT,
    [string]$EnvironmentName = $env:AZURE_ENV_NAME,
    [string]$SourceDirectory = (Get-Item -Path "$PSScriptRoot/../../" -Force).FullName
)

# ============================================================================
# CONFIGURATION
# ============================================================================

$ErrorActionPreference = 'Stop'
$VerbosePreference = 'Continue'

# Service configuration: (ServicePath, Dockerfile, ImageName)
Write-Header "🔍 VALIDATING ENVIRONMENT"

if (-not $ResourceGroupName) {
    Write-Error "ResourceGroupName is required. Set AZURE_RESOURCE_GROUP environment variable."
    exit 1
}

if (-not $RegistryEndpoint) {
    Write-Error "RegistryEndpoint is required. Set AZURE_CONTAINER_REGISTRY_ENDPOINT environment variable."
    exit 1
}

if (-not $GHCRUsername) {
    Write-Error "GHCRUsername is required. Set GHCR_USERNAME environment variable."
    exit 1
}

if (-not $GHCRPat) {
    Write-Error "GHCR_PAT is required. Set GHCR_PAT environment variable."
    exit 1
}

if (-not (Test-Path $SourceDirectory)) {
    Write-Error "Source directory not found: $SourceDirectory"
    exit 1
}

Write-Step "ResourceGroup: $ResourceGroupName"
Write-Step "Registry Endpoint: $RegistryEndpoint"
Write-Step "GHCR Username: $GHCRUsername"
Write-Step "Environment: $EnvironmentName"
Write-Step "Source Directory: $SourceDirectory"
Write-Step "Image Tag: $ImageTag"
Write-Success "Environment validation passed"

# ============================================================================
# LOGIN TO GHCR
# ============================================================================

Write-Header "🔐 AUTHENTICATING TO GITHUB CONTAINER REGISTRY (GHCR)"

Write-Step "Logging in to GHCR..."
docker logout $RegistryEndpoint 2>&1 | Out-Null
$loginResult = docker login $RegistryEndpoint -u $GHCRUsername --password-stdin <<< $GHCRPat
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to login to GHCR: $loginResult"
    exit 1
}
Write-Success "Successfully authenticated to GHCR"

$RegistryUri = $RegistryEndpoint
Write-Step "Registry URI: $RegistryUri"
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
    
    # Build and push to GHCR
    $BuildOutput = docker build -t $FullImageName -f "$ServiceFullPath/$Dockerfile" $ServiceFullPath 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to build $ImageName"
        Write-Host $BuildOutput -ForegroundColor Red
        $FailureCount++
        $FailedServices += $ImageName
        continue
    }
    $PushOutput = docker push $FullImageName 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to push $ImageName"
        Write-Host $PushOutput -ForegroundColor Red
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

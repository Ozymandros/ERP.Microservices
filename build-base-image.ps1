#!/usr/bin/env pwsh
# ============================================================================
# Build Base Image Script
# ============================================================================
# Purpose: Build the shared microservices base image before building services
# Usage: .\build-base-image.ps1
# ============================================================================

$ErrorActionPreference = 'Stop'

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Building Microservices Base Image" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "→ Building myapp-microservices-base:10.0..." -ForegroundColor Yellow

# Build the base image using docker-compose
docker-compose build microservices-base

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "✗ Failed to build base image" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✓ Base image built successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Build all services: docker-compose build" -ForegroundColor Gray
Write-Host "  2. Or start services: docker-compose up" -ForegroundColor Gray
Write-Host ""

exit 0


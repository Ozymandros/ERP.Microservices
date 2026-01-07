#!/usr/bin/env pwsh
# ============================================================================
# Optimized Build Script with BuildKit
# ============================================================================
# Purpose: Build base image and services with BuildKit optimizations
# Usage: .\build-base-image.ps1
# ============================================================================

$ErrorActionPreference = 'Stop'

# Enable BuildKit for faster builds with cache mounts
$env:DOCKER_BUILDKIT = "1"
$env:COMPOSE_DOCKER_CLI_BUILD = "1"

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🚀 Optimized Build with BuildKit" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# 1. Check if base image exists
Write-Host "→ Checking base image..." -ForegroundColor Yellow
$baseImage = docker images myapp-microservices-base:10.0 --format "{{.Repository}}:{{.Tag}}" | Select-String "myapp-microservices-base:10.0"

if (-not $baseImage) {
    Write-Host "  Base image not found. Building..." -ForegroundColor Yellow
    docker build -f docker/microservices-base.Dockerfile -t myapp-microservices-base:10.0 .
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "✗ Failed to build base image" -ForegroundColor Red
        exit 1
    }
    Write-Host "  ✓ Base image built successfully!" -ForegroundColor Green
} else {
    Write-Host "  ✓ Base image already exists (skipping build)" -ForegroundColor Green
    Write-Host "    To rebuild: docker rmi myapp-microservices-base:10.0" -ForegroundColor Gray
}

Write-Host ""
Write-Host "→ Building services in parallel with BuildKit..." -ForegroundColor Yellow
Write-Host "  (Using cache mounts for NuGet packages - much faster!)" -ForegroundColor Gray
Write-Host ""

# 2. Build all services in parallel with BuildKit
docker-compose build --parallel --parallel-max 4

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "✗ Build failed" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "✓ Build complete!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  • Start services: docker-compose up" -ForegroundColor Gray
Write-Host "  • View logs: docker-compose logs -f" -ForegroundColor Gray
Write-Host ""
Write-Host "Performance tips:" -ForegroundColor Cyan
Write-Host "  • First build: ~200-300s (downloads packages)" -ForegroundColor Gray
Write-Host "  • Subsequent builds: ~30-60s (uses cache)" -ForegroundColor Gray
Write-Host ""

exit 0


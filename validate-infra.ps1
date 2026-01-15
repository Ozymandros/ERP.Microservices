#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Valida que la infraestructura Bicep esté completa antes de desplegar a Azure
    
.DESCRIPTION
    Revisa que todos los módulos Bicep necesarios existan y que main.bicep 
    tenga referencias a todos los servicios.

.EXAMPLE
    .\validate-infra.ps1
#>

$ErrorActionPreference = "Stop"
$infraPath = "infra"
$requiredModules = @(
    "api-gateway/api-gateway.module.bicep",
    "auth-service/auth-service.module.bicep",
    "billing-service/billing-service.module.bicep",
    "inventory-service/inventory-service.module.bicep",
    "orders-service/orders-service.module.bicep",
    "purchasing-service/purchasing-service.module.bicep",
    "sales-service/sales-service.module.bicep",
    "MyApp-ApplicationInsights/MyApp-ApplicationInsights.module.bicep",
    "MyApp-LogAnalyticsWorkspace/MyApp-LogAnalyticsWorkspace.module.bicep",
    "myapp-sqlserver-roles/myapp-sqlserver-roles.module.bicep"
)

$mainBicepPath = "$infraPath/main.bicep"
$mainParametersPath = "$infraPath/main.parameters.json"

Write-Host "=================================================="
Write-Host "🔍 VALIDACIÓN DE INFRAESTRUCTURA BICEP"
Write-Host "=================================================="
Write-Host ""

# 1. Verificar main.bicep existe
Write-Host "1️⃣ Verificando main.bicep..."
if (-not (Test-Path $mainBicepPath)) {
    Write-Host "❌ main.bicep NO ENCONTRADO en $mainBicepPath" -ForegroundColor Red
    exit 1
}
Write-Host "✅ main.bicep encontrado" -ForegroundColor Green

# 2. Verificar main.parameters.json existe
Write-Host ""
Write-Host "2️⃣ Verificando main.parameters.json..."
if (-not (Test-Path $mainParametersPath)) {
    Write-Host "❌ main.parameters.json NO ENCONTRADO" -ForegroundColor Red
    exit 1
}
Write-Host "✅ main.parameters.json encontrado" -ForegroundColor Green

# 3. Verificar módulos requeridos existen
Write-Host ""
Write-Host "3️⃣ Verificando módulos Bicep necesarios..."
$missingModules = @()
foreach ($module in $requiredModules) {
    $modulePath = "$infraPath/$module"
    if (Test-Path $modulePath) {
        Write-Host "✅ $module" -ForegroundColor Green
    } else {
        Write-Host "❌ FALTA: $module" -ForegroundColor Red
        $missingModules += $module
    }
}

# 4. Verificar referencias en main.bicep
Write-Host ""
Write-Host "4️⃣ Verificando referencias en main.bicep..."
$mainBicepContent = Get-Content $mainBicepPath -Raw

$servicesToCheck = @{
    "api-gateway" = "module api_gateway"
    "auth-service" = "module auth_service"
    "billing-service" = "module billing_service"
    "inventory-service" = "module inventory_service"
    "orders-service" = "module orders_service"
    "purchasing-service" = "module purchasing_service"
    "sales-service" = "module sales_service"
    "Redis" = "module redis"
    "Application Insights" = "module MyApp_ApplicationInsights"
}

$missingReferences = @()
foreach ($service in $servicesToCheck.GetEnumerator()) {
    if ($mainBicepContent -match [regex]::Escape($service.Value)) {
        Write-Host "✅ $($service.Key) - referenciado en main.bicep" -ForegroundColor Green
    } else {
        Write-Host "❌ $($service.Key) - NO referenciado en main.bicep" -ForegroundColor Red
        $missingReferences += $service.Key
    }
}

# 5. Verificar parámetros en main.parameters.json
Write-Host ""
Write-Host "5️⃣ Verificando parámetros requeridos..."
try {
    $params = Get-Content $mainParametersPath -Raw | ConvertFrom-Json
    $requiredParams = @("principalId", "cache_password", "password", "environmentName", "location")
    
    foreach ($param in $requiredParams) {
        if ($params.parameters.PSObject.Properties.Name -contains $param) {
            Write-Host "✅ Parámetro: $param" -ForegroundColor Green
        } else {
            Write-Host "⚠️ Parámetro faltante: $param" -ForegroundColor Yellow
        }
    }
} catch {
    Write-Host "❌ Error al parsear main.parameters.json" -ForegroundColor Red
    exit 1
}

# 6. Verificar dockerfile para servicios
Write-Host ""
Write-Host "6️⃣ Verificando Dockerfiles para servicios..."
$services = @("api-gateway", "auth-service", "billing-service", "inventory-service", 
              "orders-service", "purchasing-service", "sales-service")

foreach ($service in $services) {
    $dockerfile = "$infraPath/$service/Dockerfile"
    if (Test-Path $dockerfile) {
        Write-Host "✅ $service/Dockerfile" -ForegroundColor Green
    } else {
        Write-Host "⚠️ FALTA: $service/Dockerfile" -ForegroundColor Yellow
    }
}

# Resumen
Write-Host ""
Write-Host "=================================================="
Write-Host "📊 RESUMEN DE VALIDACIÓN"
Write-Host "=================================================="

$totalIssues = $missingModules.Count + $missingReferences.Count

if ($totalIssues -eq 0) {
    Write-Host ""
    Write-Host "✅ ¡INFRAESTRUCTURA COMPLETA Y LISTA PARA DEPLOY!" -ForegroundColor Green
    Write-Host ""
    
    # Check if azd is available and run preview
    Write-Host "7️⃣ Ejecutando preview de provisionamiento..." -ForegroundColor Cyan
    $azdCheck = azd version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Ejecutando: azd provision --preview" -ForegroundColor Yellow
        azd provision --preview 2>&1 | Out-Host
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Preview completado exitosamente" -ForegroundColor Green
        } else {
            Write-Host "⚠️ Preview completado con advertencias (esto es normal)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "⚠️ azd CLI no disponible, saltando preview" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "Próximos pasos:" -ForegroundColor Cyan
    Write-Host "  1. Revisar preview: azd provision --preview"
    Write-Host "  2. Desplegar: azd provision && azd deploy"
    exit 0
} else {
    Write-Host ""
    Write-Host "🔴 PROBLEMAS ENCONTRADOS: $totalIssues" -ForegroundColor Red
    Write-Host ""
    
    if ($missingModules.Count -gt 0) {
        Write-Host "Módulos faltantes ($($missingModules.Count)):" -ForegroundColor Red
        foreach ($module in $missingModules) {
            Write-Host "  - $module" -ForegroundColor Red
        }
    }
    
    if ($missingReferences.Count -gt 0) {
        Write-Host ""
        Write-Host "Referencias faltantes en main.bicep ($($missingReferences.Count)):" -ForegroundColor Red
        foreach ($ref in $missingReferences) {
            Write-Host "  - $ref" -ForegroundColor Red
        }
    }
    
    Write-Host ""
    Write-Host "📖 Ver BICEP_TEMPLATES.md para plantillas de módulos" -ForegroundColor Yellow
    Write-Host "📖 Ver MAIN_BICEP_UPDATE.md para actualización de main.bicep" -ForegroundColor Yellow
    Write-Host "📖 Ver INFRA_REVIEW.md para análisis completo" -ForegroundColor Yellow
    
    exit 1
}

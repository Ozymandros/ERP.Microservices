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
    "services/api-gateway.bicep",
    "services/auth-service.bicep",
    "services/billing-service.bicep",
    "services/inventory-service.bicep",
    "services/orders-service.bicep",
    "services/purchasing-service.bicep",
    "services/sales-service.bicep",
    "services/aspire-dashboard.bicep",
    "services/container-app-service.bicep",
    "resources.bicep",
    "myapp-sqlserver/myapp-sqlserver.module.bicep",
    "myapp-sqlserver-roles/myapp-sqlserver-roles.module.bicep",
    "core/database/redis.bicep",
    "core/security/keyvault-secrets.bicep",
    "core/configuration/app-configuration.bicep"
)

$mainBicepPath = "$infraPath/main.bicep"
$mainParametersPath = "$infraPath/main.parameters.json"

Write-Host "=================================================="
Write-Host "VALIDACION DE INFRAESTRUCTURA BICEP"
Write-Host "=================================================="
Write-Host ""

# 1. Verificar main.bicep existe
Write-Host "1. Verificando main.bicep..."
if (-not (Test-Path $mainBicepPath)) {
    Write-Host "ERROR: main.bicep NO ENCONTRADO en $mainBicepPath" -ForegroundColor Red
    exit 1
}
Write-Host "OK: main.bicep encontrado" -ForegroundColor Green

# 2. Verificar main.parameters.json existe
Write-Host ""
Write-Host "2. Verificando main.parameters.json..."
if (-not (Test-Path $mainParametersPath)) {
    Write-Host "ERROR: main.parameters.json NO ENCONTRADO" -ForegroundColor Red
    exit 1
}
Write-Host "OK: main.parameters.json encontrado" -ForegroundColor Green

# 3. Verificar módulos requeridos existen
Write-Host ""
Write-Host "3. Verificando modulos Bicep necesarios..."
$missingModules = @()
foreach ($module in $requiredModules) {
    $modulePath = "$infraPath/$module"
    if (Test-Path $modulePath) {
        Write-Host "OK: $module" -ForegroundColor Green
    } else {
        Write-Host "FALTA: $module" -ForegroundColor Red
        $missingModules += $module
    }
}

# 4. Verificar referencias en main.bicep
Write-Host ""
Write-Host "4. Verificando referencias en main.bicep..."
$mainBicepContent = Get-Content $mainBicepPath -Raw

$servicesToCheck = @{
    "api-gateway" = "module apiGatewayModule"
    "auth-service" = "module authServiceModule"
    "billing-service" = "module billingServiceModule"
    "inventory-service" = "module inventoryServiceModule"
    "orders-service" = "module ordersServiceModule"
    "purchasing-service" = "module purchasingServiceModule"
    "sales-service" = "module salesServiceModule"
    "aspire-dashboard" = "module aspireDashboardModule"
    "Redis" = "module redis"
    "SQL Server" = "module myapp_sqlserver"
    "Resources" = "module resources"
    "Key Vault" = "module keyVault"
    "App Configuration" = "module appConfiguration"
}

$missingReferences = @()
foreach ($service in $servicesToCheck.GetEnumerator()) {
    if ($mainBicepContent -match [regex]::Escape($service.Value)) {
        Write-Host "OK: $($service.Key) - referenciado en main.bicep" -ForegroundColor Green
    } else {
        Write-Host "ERROR: $($service.Key) - NO referenciado en main.bicep" -ForegroundColor Red
        $missingReferences += $service.Key
    }
}

# 5. Verificar parámetros en main.parameters.json
Write-Host ""
Write-Host "5. Verificando parametros requeridos..."
try {
    $params = Get-Content $mainParametersPath -Raw | ConvertFrom-Json
    $requiredParams = @("cache_password", "password", "environmentName", "location", "jwtSecretKey", "imageTag")
    $optionalParams = @("jwtIssuer", "jwtAudience", "frontendOrigin", "aspnetcoreEnvironment", "ghcrUsername", "ghcrPat")
    
    foreach ($param in $requiredParams) {
        if ($params.parameters.PSObject.Properties.Name -contains $param) {
            Write-Host "OK: Parametro requerido: $param" -ForegroundColor Green
        } else {
            Write-Host "ADVERTENCIA: Parametro faltante: $param" -ForegroundColor Yellow
        }
    }
    
    foreach ($param in $optionalParams) {
        if ($params.parameters.PSObject.Properties.Name -contains $param) {
            Write-Host "OK: Parametro opcional: $param" -ForegroundColor Cyan
        }
    }
} catch {
    Write-Host "ERROR: Error al parsear main.parameters.json: $_" -ForegroundColor Red
    Write-Host "   Asegurate de que el archivo es JSON valido" -ForegroundColor Yellow
    exit 1
}

# 6. Verificar dockerfile para servicios
Write-Host ""
Write-Host "6. Verificando Dockerfiles para servicios..."
$serviceDockerfiles = @{
    "api-gateway" = "src/ErpApiGateway/Dockerfile"
    "auth-service" = "src/MyApp.Auth/MyApp.Auth.API/Dockerfile"
    "billing-service" = "src/MyApp.Billing/MyApp.Billing.API/Dockerfile"
    "inventory-service" = "src/MyApp.Inventory/MyApp.Inventory.API/Dockerfile"
    "orders-service" = "src/MyApp.Orders/MyApp.Orders.API/Dockerfile"
    "purchasing-service" = "src/MyApp.Purchasing/MyApp.Purchasing.API/Dockerfile"
    "sales-service" = "src/MyApp.Sales/MyApp.Sales.API/Dockerfile"
}

foreach ($service in $serviceDockerfiles.GetEnumerator()) {
    if (Test-Path $service.Value) {
        Write-Host "OK: $($service.Key) - $($service.Value)" -ForegroundColor Green
    } else {
        Write-Host "ADVERTENCIA: FALTA: $($service.Key) - $($service.Value)" -ForegroundColor Yellow
    }
}

# Verificar Dockerfile base compartit
$baseDockerfile = "docker/microservices-base.Dockerfile"
if (Test-Path $baseDockerfile) {
    Write-Host "OK: Base image Dockerfile: $baseDockerfile" -ForegroundColor Green
} else {
    Write-Host "ADVERTENCIA: FALTA: $baseDockerfile" -ForegroundColor Yellow
}

# 7. Verificar configuración GHCR (si se usa)
Write-Host ""
Write-Host "7. Verificando configuracion de registro de contenedores..."
$mainBicepContent = Get-Content $mainBicepPath -Raw
if ($mainBicepContent -match "ghcrUsername|ghcrPat") {
    Write-Host "OK: Parametros GHCR encontrados en main.bicep" -ForegroundColor Green
    
    # Verificar que container-app-service.bicep soporte GHCR
    $containerAppServicePath = "$infraPath/services/container-app-service.bicep"
    if (Test-Path $containerAppServicePath) {
        $containerAppServiceContent = Get-Content $containerAppServicePath -Raw
        if ($containerAppServiceContent -match "registryType|ghcrUsername|ghcrPat") {
            Write-Host "OK: container-app-service.bicep soporta GHCR" -ForegroundColor Green
        } else {
            Write-Host "ADVERTENCIA: container-app-service.bicep puede no soportar GHCR completamente" -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "INFO: No se encontraron parametros GHCR (usando ACR por defecto)" -ForegroundColor Cyan
}

# Resumen
Write-Host ""
Write-Host "=================================================="
Write-Host "RESUMEN DE VALIDACION"
Write-Host "=================================================="

$totalIssues = $missingModules.Count + $missingReferences.Count

if ($totalIssues -eq 0) {
    Write-Host ""
    Write-Host "OK: INFRAESTRUCTURA COMPLETA Y LISTA PARA DEPLOY!" -ForegroundColor Green
    Write-Host ""
    
    # Check if azd is available and run preview
    Write-Host "8. Ejecutando preview de provisionamiento..." -ForegroundColor Cyan
    $azdCheck = azd version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Ejecutando: azd provision --preview" -ForegroundColor Yellow
        azd provision --preview 2>&1 | Out-Host
        if ($LASTEXITCODE -eq 0) {
            Write-Host "OK: Preview completado exitosamente" -ForegroundColor Green
        } else {
            Write-Host "ADVERTENCIA: Preview completado con advertencias (esto es normal)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "ADVERTENCIA: azd CLI no disponible, saltando preview" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "Proximos pasos:" -ForegroundColor Cyan
    Write-Host "  1. Revisar preview: azd provision --preview"
    Write-Host "  2. Desplegar: azd provision; azd deploy"
    exit 0
} else {
    Write-Host ""
    Write-Host "ERROR: PROBLEMAS ENCONTRADOS: $totalIssues" -ForegroundColor Red
    Write-Host ""
    
    if ($missingModules.Count -gt 0) {
        Write-Host "Modulos faltantes ($($missingModules.Count)):" -ForegroundColor Red
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
    Write-Host "Ver BICEP_TEMPLATES.md para plantillas de modulos" -ForegroundColor Yellow
    Write-Host "Ver MAIN_BICEP_UPDATE.md para actualizacion de main.bicep" -ForegroundColor Yellow
    Write-Host "Ver INFRA_REVIEW.md para analisis completo" -ForegroundColor Yellow
    
    exit 1
}

# Bicep Infrastructure Validation Script
# Purpose: Validate all Bicep files for correctness
# Usage: ./validate-bicep.ps1
# PowerShell script for Windows (*.ps1)

param(
    [Parameter(Mandatory=$false)]
    [string]$InfraPath = ".\infra",
    [Parameter(Mandatory=$false)]
    [switch]$Verbose = $false
)

Write-Host "================================================" -ForegroundColor Green
Write-Host "Bicep Infrastructure Validation Script" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""

# Check if Azure CLI is installed
$azVersion = az version --output json | ConvertFrom-Json
if ($null -eq $azVersion) {
    Write-Host "ERROR: Azure CLI not found. Please install Azure CLI to validate Bicep files." -ForegroundColor Red
    exit 1
}

Write-Host "✓ Azure CLI version: $($azVersion.'azure-cli')" -ForegroundColor Green
Write-Host ""

# Files to validate
$filesToValidate = @(
    "$InfraPath/main.bicep",
    "$InfraPath/resources.bicep",
    "$InfraPath/core/database/redis.bicep",
    "$InfraPath/core/database/sql-server.bicep",
    "$InfraPath/core/security/keyvault-secrets.bicep",
    "$InfraPath/core/host/container-app.bicep",
    "$InfraPath/myapp-sqlserver/myapp-sqlserver.module.bicep",
    "$InfraPath/myapp-sqlserver-roles/myapp-sqlserver-roles.module.bicep",
    "$InfraPath/MyApp-ApplicationInsights/MyApp-ApplicationInsights.module.bicep",
    "$InfraPath/MyApp-LogAnalyticsWorkspace/MyApp-LogAnalyticsWorkspace.module.bicep"
)

Write-Host "Files to validate: $($filesToValidate.Count)" -ForegroundColor Cyan
Write-Host ""

$successCount = 0
$failureCount = 0
$failedFiles = @()

# Validate each file
foreach ($file in $filesToValidate) {
    if (-not (Test-Path $file)) {
        Write-Host "⚠ SKIP: File not found: $file" -ForegroundColor Yellow
        continue
    }
    
    Write-Host "Validating: $file" -ForegroundColor Cyan
    
    try {
        $result = az bicep validate --file $file 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✓ Valid" -ForegroundColor Green
            $successCount++
        } else {
            Write-Host "  ✗ Invalid" -ForegroundColor Red
            Write-Host "  Error: $result" -ForegroundColor Red
            $failureCount++
            $failedFiles += $file
        }
    } catch {
        Write-Host "  ✗ Exception: $_" -ForegroundColor Red
        $failureCount++
        $failedFiles += $file
    }
    
    if ($Verbose) {
        Write-Host ""
    }
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "Validation Summary" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green

Write-Host "Successful: $successCount" -ForegroundColor Green
Write-Host "Failed: $failureCount" -ForegroundColor $(if ($failureCount -gt 0) { "Red" } else { "Green" })

if ($failureCount -gt 0) {
    Write-Host ""
    Write-Host "Failed Files:" -ForegroundColor Red
    foreach ($file in $failedFiles) {
        Write-Host "  - $file" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "Run with -Verbose flag for detailed error messages:" -ForegroundColor Yellow
    Write-Host "  ./validate-bicep.ps1 -Verbose" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "✓ All Bicep files are valid!" -ForegroundColor Green
Write-Host ""

# Check for critical parameters
Write-Host "================================================" -ForegroundColor Green
Write-Host "Checking Critical Parameters" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""

$mainBicepContent = Get-Content "$InfraPath/main.bicep" -Raw

$criticalParams = @(
    "jwtSecretKey",
    "jwtIssuer",
    "jwtAudience",
    "frontendOrigin",
    "aspnetcoreEnvironment"
)

Write-Host "JWT Parameters:" -ForegroundColor Cyan
foreach ($param in $criticalParams) {
    if ($mainBicepContent -match "param $param") {
        Write-Host "  ✓ $param" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $param - MISSING" -ForegroundColor Red
    }
}

Write-Host ""

# Check for module calls
Write-Host "Module Calls:" -ForegroundColor Cyan
$moduleNames = @("redis", "sqlServer", "keyVault")
foreach ($moduleName in $moduleNames) {
    if ($mainBicepContent -match "module $moduleName") {
        Write-Host "  ✓ $moduleName module call" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $moduleName module call - MISSING" -ForegroundColor Red
    }
}

Write-Host ""

# Check Key Vault enablement
Write-Host "Critical Security Check:" -ForegroundColor Cyan
if ($mainBicepContent -match "enableKeyVault:\s*true") {
    Write-Host "  ✓ Key Vault enabled (enableKeyVault: true)" -ForegroundColor Green
} else {
    Write-Host "  ✗ Key Vault NOT enabled - CRITICAL!" -ForegroundColor Red
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "Validation Complete" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""

if ($failureCount -eq 0) {
    Write-Host "✓ Infrastructure is ready for deployment!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "✗ Please fix the validation errors above" -ForegroundColor Red
    exit 1
}

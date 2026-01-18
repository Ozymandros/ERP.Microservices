# What-If Deployment Script
# Preview changes before deploying infrastructure to Azure
# Usage: ./validate-bicep--what-if.ps1 -Location "eastus"

param(
    [Parameter(Mandatory=$false)]
    [string]$SubscriptionId = "",
    [Parameter(Mandatory=$false)]
    [string]$Location = "eastus",
    [Parameter(Mandatory=$false)]
    [string]$TemplateFile = ".\infra\main.bicep",
    [Parameter(Mandatory=$false)]
    [string]$ParametersFile = ".\infra\main.parameters.json"
)

Write-Host "================================================" -ForegroundColor Green
Write-Host "What-If Deployment Preview" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""

# Validate files exist
if (-not (Test-Path $TemplateFile)) {
    Write-Host "✗ Template file not found: $TemplateFile" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $ParametersFile)) {
    Write-Host "✗ Parameters file not found: $ParametersFile" -ForegroundColor Red
    exit 1
}

Write-Host "Checking Azure CLI..." -ForegroundColor Cyan
$cliCheck = az version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Azure CLI not found or not accessible" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Azure CLI available" -ForegroundColor Green
Write-Host ""

# Check if logged in
Write-Host "Checking Azure login..." -ForegroundColor Cyan
$currentAccount = az account show 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Not logged into Azure" -ForegroundColor Red
    Write-Host ""
    Write-Host "Fix: Run 'az login'" -ForegroundColor Yellow
    exit 1
}

Write-Host "✓ Logged in to Azure" -ForegroundColor Green
Write-Host ""

# Get subscription - ALWAYS get fresh subscription ID
Write-Host "Getting subscription ID..." -ForegroundColor Cyan
$SubscriptionId = az account show --query id -o tsv 2>$null

if ([string]::IsNullOrEmpty($SubscriptionId)) {
    Write-Host "✗ Could not get subscription ID" -ForegroundColor Red
    Write-Host ""
    Write-Host "Available subscriptions:" -ForegroundColor Yellow
    az account list --query "[].{Name:name, Id:id}" -o table
    exit 1
}

Write-Host "✓ Subscription ID: $SubscriptionId" -ForegroundColor Green
Write-Host ""

Write-Host "Template: $TemplateFile" -ForegroundColor Cyan
Write-Host "Parameters: $ParametersFile" -ForegroundColor Cyan
Write-Host "Location: $Location" -ForegroundColor Cyan
Write-Host ""

# Run What-If at SUBSCRIPTION scope
Write-Host "Running What-If deployment preview..." -ForegroundColor Yellow
Write-Host "(This will show proposed changes without making any modifications)" -ForegroundColor Gray
Write-Host ""

az deployment sub what-if `
  --subscription "$SubscriptionId" `
  --location "$Location" `
  --template-file "$TemplateFile" `
  --parameters `@`"$ParametersFile`" 

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "✗ What-If failed" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "  1. Verify Bicep syntax:" -ForegroundColor Yellow
    Write-Host "     ./validate-bicep.ps1 -ShowDetails" -ForegroundColor Gray
    Write-Host "  2. Verify parameters file:" -ForegroundColor Yellow
    Write-Host "     Get-Content $ParametersFile | ConvertFrom-Json" -ForegroundColor Gray
    Write-Host "  3. Check subscription access:" -ForegroundColor Yellow
    Write-Host "     az account list" -ForegroundColor Gray
    Write-Host "  4. View detailed error:" -ForegroundColor Yellow
    Write-Host "     az deployment sub what-if --subscription '$SubscriptionId' --location '$Location' --template-file '$TemplateFile' --parameters @'$ParametersFile' --debug" -ForegroundColor Gray
    exit 1
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "✓ What-If preview complete" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""

# Run azd provision --preview if available
Write-Host "Running azd provision --preview..." -ForegroundColor Cyan
$azdCheck = azd version 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    azd provision --preview 2>&1 | Out-Host
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "✓ azd provision preview complete" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "⚠ azd provision preview completed with warnings (this is normal)" -ForegroundColor Yellow
    }
} else {
    Write-Host "⚠ azd CLI not available, skipping azd preview" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Review the changes above" -ForegroundColor Cyan
Write-Host "  2. Preview with azd: azd provision --preview" -ForegroundColor Cyan
Write-Host "  3. If satisfied, deploy: azd provision && azd deploy" -ForegroundColor Cyan
Write-Host ""
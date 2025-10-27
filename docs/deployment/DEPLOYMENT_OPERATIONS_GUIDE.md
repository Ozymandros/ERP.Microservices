# ERP Microservices - Deployment & Operations Guide

## Table of Contents

1. [Pre-Deployment Checklist](#pre-deployment-checklist)
2. [Deployment Architecture](#deployment-architecture)
3. [Step-by-Step Deployment](#step-by-step-deployment)
4. [Post-Deployment Verification](#post-deployment-verification)
5. [Operations & Monitoring](#operations--monitoring)
6. [Troubleshooting](#troubleshooting)
7. [Maintenance & Updates](#maintenance--updates)

---

## Pre-Deployment Checklist

### Prerequisites

- [ ] Azure Subscription (Owner or Contributor role)
- [ ] Azure CLI 2.50+ installed
- [ ] PowerShell 5.1+ installed
- [ ] Git installed (for version control)
- [ ] Network connectivity to Azure
- [ ] Sufficient quota in Azure subscription

### Required Permissions

```powershell
# Check current user permissions
az role assignment list --assignee (az account show --query user.name -o tsv)

# Verify required roles
# - Contributor (on subscription or resource group)
# - User Access Administrator (for RBAC assignments)
# - Key Vault Administrator (for secrets management)
```

### Environment Preparation

```powershell
# 1. Authenticate to Azure
az login

# 2. Set subscription
az account set --subscription "your-subscription-id"

# 3. Verify subscription
az account show
az account list-locations

# 4. Check CLI version
az --version
az bicep version
```

---

## Deployment Architecture

### Resource Creation Order

```
1. Resource Group (rg-{environment})
   ↓
2. Core Infrastructure
   ├─ Container Registry (ACR)
   ├─ Log Analytics Workspace
   ├─ Storage Account (file shares)
   ├─ Container Apps Environment
   └─ Network configuration
   ↓
3. Security Infrastructure
   ├─ Key Vault (with purge protection)
   ├─ Key Vault Secrets
   │  ├─ JWT secret key
   │  ├─ Redis password
   │  ├─ SQL passwords
   │  └─ Connection strings
   └─ Managed Identities (8 total)
   ↓
4. Configuration Management
   ├─ App Configuration Store
   ├─ Configuration Key-Values
   └─ Key Vault References
   ↓
5. Data Services
   ├─ SQL Server
   ├─ Databases (6)
   └─ Redis Cache
   ↓
6. Monitoring
   ├─ Application Insights
   └─ Diagnostic Settings
   ↓
7. Microservices (Parallel)
   ├─ Auth Service
   ├─ Billing Service
   ├─ Inventory Service
   ├─ Orders Service
   ├─ Purchasing Service
   ├─ Sales Service
   └─ API Gateway
   ↓
8. RBAC & Security
   ├─ Service → App Configuration RBAC
   ├─ App Configuration → Key Vault RBAC
   └─ All → Container Registry RBAC
```

### Bicep Module Execution Order

```bicep
1. main.bicep (subscription scope)
   ├─ Create resource group
   ├─ Call resources.bicep (shared services)
   │
   ├─ Call redis.bicep
   ├─ Call sql-server.bicep
   ├─ Call keyvault-secrets.bicep
   │
   ├─ Call app-configuration.bicep
   │
   ├─ Call each service bicep (parallel)
   │  ├─ auth-service.bicep
   │  ├─ billing-service.bicep
   │  ├─ inventory-service.bicep
   │  ├─ orders-service.bicep
   │  ├─ purchasing-service.bicep
   │  ├─ sales-service.bicep
   │  └─ api-gateway.bicep
   │
   └─ Call RBAC modules
      ├─ appconfig-rbac.bicep (×7 services)
      └─ keyvault-rbac.bicep (×1 for App Config)
```

---

## Step-by-Step Deployment

### Phase 1: Parameter Preparation

```powershell
# ============================================================================
# Step 1: Generate Secure Secrets
# ============================================================================

# Generate JWT Secret (384 bits = very secure)
$jwtSecretKey = [Convert]::ToBase64String(
    [System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48)
)
Write-Host "JWT Secret Key: $jwtSecretKey"

# Generate Redis Cache Password (256 bits)
$cachePassword = [Convert]::ToBase64String(
    [System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
)
Write-Host "Cache Password: $cachePassword"

# Generate SQL Admin Password (16+ chars, mixed case, numbers, special chars)
$sqlPassword = "P@$(([System.Guid]::NewGuid()).ToString().Replace('-','').Substring(0,20))!1"
Write-Host "SQL Password: $sqlPassword"

# Save to environment variables (temporary)
$env:JWT_SECRET_KEY = $jwtSecretKey
$env:CACHE_PASSWORD = $cachePassword
$env:SQL_PASSWORD = $sqlPassword

# ============================================================================
# Step 2: Create Parameters File
# ============================================================================

$params = @{
    environmentName         = "prod"
    location                = "eastus"
    jwtSecretKey            = $jwtSecretKey
    cache_password          = $cachePassword
    password                = $sqlPassword
    jwtIssuer               = "MyApp.Auth"
    jwtAudience             = "MyApp.All"
    frontendOrigin          = "https://app.contoso.com;https://www.contoso.com"
    aspnetcoreEnvironment   = "Production"
}

# Save to file
$params | ConvertTo-Json | Out-File "parameters-prod.json"

# ============================================================================
# Step 3: Validate Syntax
# ============================================================================

# Build Bicep to ARM template
az bicep build --file "infra/main.bicep"

# This creates main.json (ARM template)
# File: infra/main.json
```

### Phase 2: Validation

```powershell
# ============================================================================
# Step 4: Validate Deployment (Dry Run)
# ============================================================================

Write-Host "Starting deployment validation..." -ForegroundColor Cyan

$validationResult = az deployment sub validate `
    --template-file "infra/main.bicep" `
    --parameters "parameters-prod.json" `
    --location "eastus" `
    --output json | ConvertFrom-Json

if ($validationResult.error) {
    Write-Host "❌ Validation FAILED" -ForegroundColor Red
    Write-Host $validationResult.error.details
    exit 1
}

Write-Host "✅ Validation PASSED" -ForegroundColor Green

# ============================================================================
# Step 5: What-If Analysis
# ============================================================================

Write-Host "Starting What-If analysis..." -ForegroundColor Cyan

$whatIfResult = az deployment sub what-if `
    --template-file "infra/main.bicep" `
    --parameters "parameters-prod.json" `
    --location "eastus" `
    --output json | ConvertFrom-Json

Write-Host "Resources to be created:"
$whatIfResult.changes | Where-Object { $_.changeType -eq "Create" } | ForEach-Object {
    Write-Host "  ✓ $($_.resourceId)" -ForegroundColor Green
}

Write-Host "Resources to be modified:"
$whatIfResult.changes | Where-Object { $_.changeType -eq "Modify" } | ForEach-Object {
    Write-Host "  ~ $($_.resourceId)" -ForegroundColor Yellow
}

Write-Host "Resources to be deleted:"
$whatIfResult.changes | Where-Object { $_.changeType -eq "Delete" } | ForEach-Object {
    Write-Host "  ✗ $($_.resourceId)" -ForegroundColor Red
}
```

### Phase 3: Deployment

```powershell
# ============================================================================
# Step 6: Deploy Infrastructure
# ============================================================================

$deploymentName = "erp-deployment-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

Write-Host "Starting deployment: $deploymentName" -ForegroundColor Cyan

$deployment = az deployment sub create `
    --template-file "infra/main.bicep" `
    --parameters "parameters-prod.json" `
    --name $deploymentName `
    --location "eastus" `
    --output json | ConvertFrom-Json

if ($deployment.properties.provisioningState -ne "Succeeded") {
    Write-Host "❌ Deployment FAILED" -ForegroundColor Red
    Write-Host $deployment.properties.error
    exit 1
}

Write-Host "✅ Deployment SUCCEEDED" -ForegroundColor Green

# Save deployment outputs
$outputs = $deployment.properties.outputs
$outputs | ConvertTo-Json | Out-File "deployment-outputs-prod.json"

Write-Host "Outputs saved to deployment-outputs-prod.json"

# ============================================================================
# Step 7: Extract Important Outputs
# ============================================================================

$outputs = $deployment.properties.outputs

$apiGatewayFqdn = $outputs.API_GATEWAY_FQDN.value
$keyVaultName = $outputs.AZURE_KEY_VAULT_NAME.value
$sqlServerName = $outputs.AZURE_SQL_SERVER_NAME.value
$redisHostName = $outputs.AZURE_REDIS_CACHE_HOST.value

Write-Host "
========================================
 Deployment Complete - Important URLs
========================================

API Gateway:       https://$apiGatewayFqdn
Key Vault:         $keyVaultName.vault.azure.net
SQL Server:        $sqlServerName.database.windows.net
Redis Cache:       $redisHostName:6380
App Config:        (search in Azure Portal)

Save these for reference!
========================================
"
```

---

## Post-Deployment Verification

### Phase 4: Verify Deployment

```powershell
# ============================================================================
# Step 8: Verify Resources Created
# ============================================================================

$resourceGroup = "rg-prod"

Write-Host "Verifying resource creation..." -ForegroundColor Cyan

# Verify resource group
$rg = az group show -n $resourceGroup -o json | ConvertFrom-Json
Write-Host "✓ Resource Group: $($rg.name)" -ForegroundColor Green

# Verify container apps
$containerApps = az containerapp list -g $resourceGroup -o json | ConvertFrom-Json
Write-Host "✓ Container Apps: $($containerApps.Count) services"
$containerApps | ForEach-Object {
    Write-Host "  - $($_.name) (Replicas: $($_.properties.template.scale.maxReplicas))"
}

# Verify database
$sqlServer = az sql server list -g $resourceGroup -o json | ConvertFrom-Json
if ($sqlServer) {
    Write-Host "✓ SQL Server: $($sqlServer[0].name)"
    $databases = az sql db list -g $resourceGroup -s $sqlServer[0].name -o json | ConvertFrom-Json
    Write-Host "  Databases: $($databases.Count)"
}

# Verify Key Vault
$keyVault = az keyvault list -g $resourceGroup -o json | ConvertFrom-Json
if ($keyVault) {
    Write-Host "✓ Key Vault: $($keyVault[0].name)"
    $secrets = az keyvault secret list --vault-name $keyVault[0].name -o json | ConvertFrom-Json
    Write-Host "  Secrets: $($secrets.Count)"
}

# Verify App Configuration
$appConfig = az appconfig list -g $resourceGroup -o json | ConvertFrom-Json
if ($appConfig) {
    Write-Host "✓ App Configuration: $($appConfig[0].name)"
}

# ============================================================================
# Step 9: Verify RBAC Assignments
# ============================================================================

Write-Host "Verifying RBAC assignments..." -ForegroundColor Cyan

# List all role assignments in resource group
$roleAssignments = az role assignment list `
    --resource-group $resourceGroup `
    --output json | ConvertFrom-Json

Write-Host "Total RBAC assignments: $($roleAssignments.Count)"

# Count by role
$roleAssignments | Group-Object { $_.roleDefinitionName } | ForEach-Object {
    Write-Host "  - $($_.Name): $($_.Count)"
}

# ============================================================================
# Step 10: Verify Secrets in Key Vault
# ============================================================================

$keyVaultName = (az keyvault list -g $resourceGroup --query "[0].name" -o tsv)

Write-Host "Verifying secrets in Key Vault: $keyVaultName" -ForegroundColor Cyan

$secrets = az keyvault secret list --vault-name $keyVaultName -o json | ConvertFrom-Json

$expectedSecrets = @(
    "jwt-secret-key"
    "redis-connection"
    "redis-cache-password"
    "sql-connection-authdb"
    "sql-connection-billingdb"
    "sql-connection-inventorydb"
    "sql-connection-ordersdb"
    "sql-connection-purchasingdb"
    "sql-connection-salesdb"
)

foreach ($secretName in $expectedSecrets) {
    $secret = $secrets | Where-Object { $_.name -eq $secretName }
    if ($secret) {
        Write-Host "  ✓ $secretName" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $secretName" -ForegroundColor Red
    }
}

# ============================================================================
# Step 11: Test Service Connectivity
# ============================================================================

Write-Host "Testing service connectivity..." -ForegroundColor Cyan

# Get API Gateway FQDN
$apiGatewayFqdn = az containerapp show `
    -g $resourceGroup `
    -n "api-gateway" `
    --query "properties.configuration.ingress.fqdn" `
    -o tsv

Write-Host "API Gateway: https://$apiGatewayFqdn"

# Test API Gateway endpoint
$response = Invoke-WebRequest -Uri "https://$apiGatewayFqdn/health" `
    -ErrorAction SilentlyContinue

if ($response.StatusCode -eq 200) {
    Write-Host "  ✓ API Gateway is responding" -ForegroundColor Green
} else {
    Write-Host "  ⚠ API Gateway returned: $($response.StatusCode)" -ForegroundColor Yellow
}
```

---

## Operations & Monitoring

### Real-Time Logs

```powershell
# Get logs from a specific service (real-time)
az containerapp logs show `
    -g rg-prod `
    -n auth-service `
    --tail 50 `
    --follow

# Get logs for all services
$services = @("auth-service", "billing-service", "inventory-service", "orders-service", "purchasing-service", "sales-service", "api-gateway")

foreach ($service in $services) {
    Write-Host "Logs for $service:" -ForegroundColor Cyan
    az containerapp logs show -g rg-prod -n $service --tail 10
    Write-Host ""
}
```

### KQL Monitoring Queries

```kusto
// Query 1: Service Error Rate (last 24h)
let duration = 24h;
traces
| where timestamp > ago(duration)
| where severityLevel >= 2  // Warning or Error
| summarize ErrorCount = count() by name, severityLevel
| sort by ErrorCount desc

// Query 2: API Response Times (percentiles)
customMetrics
| where name == "RequestDuration"
| where timestamp > ago(1h)
| summarize 
    P50 = percentile(value, 50),
    P95 = percentile(value, 95),
    P99 = percentile(value, 99)
    by operation_Name

// Query 3: External Dependencies Health
dependencies
| where timestamp > ago(1h)
| summarize 
    TotalCalls = count(),
    FailedCalls = count(success == false)
    by name, type
| extend SuccessRate = (TotalCalls - FailedCalls) * 100.0 / TotalCalls
| where SuccessRate < 99

// Query 4: Service-to-Service Call Latency
customEvents
| where name == "ServiceInvocation"
| where timestamp > ago(1h)
| summarize 
    AvgLatency = avg(todouble(customMeasurements.latency_ms)),
    MaxLatency = max(todouble(customMeasurements.latency_ms))
    by customDimensions.source_service, customDimensions.target_service
```

### Health Check Endpoints

```bash
# Check API Gateway health
curl -X GET https://{api-gateway-fqdn}/health

# Check service health
curl -X GET https://{api-gateway-fqdn}/auth/health
curl -X GET https://{api-gateway-fqdn}/billing/health
curl -X GET https://{api-gateway-fqdn}/inventory/health
# etc.

# Expected Response
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "redis": "Healthy",
    "keyvault": "Healthy"
  }
}
```

---

## Troubleshooting

### Common Issues & Solutions

| Issue | Symptoms | Solution |
|-------|----------|----------|
| **Services won't start** | Pods keep crashing | Check container logs: `az containerapp logs show` |
| **Key Vault access denied** | 403 Forbidden errors | Verify RBAC: `az role assignment list` |
| **Database connection failed** | App crashes at startup | Check connection string in App Config |
| **Redis connection timeout** | Cache operations fail | Verify Redis password, check firewall |
| **JWT validation fails** | 401 Unauthorized on API calls | Verify JWT secret in Key Vault matches signing key |
| **Configuration not updating** | Old config persists | Clear service cache, check App Config labels |
| **High memory usage** | Service memory exceeds limits | Check for memory leaks, scale up memory allocation |
| **Slow API responses** | Request latency > 500ms | Check database query performance, Redis cache hit rate |

### Debug Commands

```powershell
# 1. Check service status
az containerapp show -g rg-prod -n auth-service `
    --query "properties.runningStatus" -o table

# 2. View provisioning errors
az containerapp show -g rg-prod -n auth-service `
    --query "properties.deploymentStatus" -o json

# 3. Check container registry access
az acr check-health -n {acrName} --ignore-errors

# 4. Verify managed identity
az identity show -g rg-prod -n auth-service-mi

# 5. Test Key Vault access
az keyvault secret show `
    --vault-name {vaultName} `
    --name jwt-secret-key

# 6. Check App Configuration values
az appconfig kv list `
    --name {configStoreName} `
    --all

# 7. Monitor scaling events
az monitor metrics list `
    --resource-group rg-prod `
    --resource-type "Microsoft.App/containerApps" `
    --resource auth-service `
    --metric ReplicaCount `
    --start-time (Get-Date).AddHours(-1) `
    --interval PT1M
```

---

## Maintenance & Updates

### Regular Maintenance Tasks

```powershell
# ============================================================================
# Weekly Tasks
# ============================================================================

# Review error logs
$errorCount = az monitor metrics list `
    --resource-group rg-prod `
    --resource-type "Microsoft.App/containerApps" `
    --metric ErrorCount `
    --start-time (Get-Date).AddDays(-7) `
    --aggregation Total

# Check Key Vault secret expiration (manual tracking)
Write-Host "Check Key Vault secrets last updated:"
az keyvault secret list `
    --vault-name {vaultName} `
    --query "[].attributes.updated"

# ============================================================================
# Monthly Tasks
# ============================================================================

# Rotate secrets (manual process)
# 1. Generate new JWT secret
$newJwtSecret = [Convert]::ToBase64String(
    [System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48)
)

# 2. Update Key Vault
az keyvault secret set `
    --vault-name {vaultName} `
    --name jwt-secret-key `
    --value $newJwtSecret

# 3. Services auto-refresh from App Config

# Review performance metrics
az monitor metrics list `
    --resource-group rg-prod `
    --metric "AverageResponseTime" `
    --start-time (Get-Date).AddMonths(-1)

# ============================================================================
# Quarterly Tasks
# ============================================================================

# Review RBAC assignments
az role assignment list --resource-group rg-prod

# Update Azure CLI and Bicep
az upgrade
az bicep upgrade

# Review backup/disaster recovery
az backup vault list -g rg-prod
```

### Updating Services

```powershell
# ============================================================================
# Update Container Image
# ============================================================================

# 1. Build new image
docker build -t {acrName}.azurecr.io/auth-service:v2.0 .

# 2. Push to ACR
docker push {acrName}.azurecr.io/auth-service:v2.0

# 3. Update container app
az containerapp update `
    -g rg-prod `
    -n auth-service `
    --image {acrName}.azurecr.io/auth-service:v2.0

# 4. Monitor rollout
az containerapp logs show -g rg-prod -n auth-service --follow
```

### Updating Infrastructure (Bicep)

```powershell
# ============================================================================
# Update Bicep Templates
# ============================================================================

# 1. Modify Bicep files
# Example: Update redis capacity in redis.bicep

# 2. Validate changes
az bicep build --file infra/main.bicep
az deployment sub validate `
    --template-file infra/main.bicep `
    --parameters parameters-prod.json `
    --location eastus

# 3. Preview What-If
az deployment sub what-if `
    --template-file infra/main.bicep `
    --parameters parameters-prod.json `
    --location eastus

# 4. Deploy updates
az deployment sub create `
    --template-file infra/main.bicep `
    --parameters parameters-prod.json `
    --name "erp-update-$(Get-Date -Format 'yyyyMMdd-HHmmss')" `
    --location eastus
```

---

## Appendix: Quick Reference

### Common Deployment Scenarios

```powershell
# Scenario 1: Fresh Production Deployment
.\scripts\deploy-prod.ps1

# Scenario 2: Deploy to Staging
.\scripts\deploy-staging.ps1

# Scenario 3: Rolling Update of Single Service
.\scripts\update-service.ps1 -Service "auth-service" -ImageTag "v2.0"

# Scenario 4: Scale Up During Peak Hours
az containerapp update -g rg-prod -n auth-service `
    --min-replicas 3 --max-replicas 20

# Scenario 5: Rollback to Previous Deployment
az deployment group deployment-operation list `
    -g rg-prod `
    --deployment-name {previous-deployment-name}
```

### Emergency Commands

```powershell
# Stop a malfunctioning service
az containerapp update -g rg-prod -n {service-name} `
    --min-replicas 0 --max-replicas 1

# Restart a service
az containerapp revision deactivate -g rg-prod -n {service-name}

# Clear application cache
az redis-cli -g rg-prod -n {redis-name} FLUSHALL

# Force sync App Configuration
# (Services check every 30s by default)

# Drain connections before maintenance
# Update API Gateway routing rules temporarily
```

---

**Document Version:** 1.0  
**Last Updated:** 2024-10-27  
**Status:** ✅ Production Ready

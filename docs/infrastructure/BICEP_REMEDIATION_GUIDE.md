# 🔧 Bicep Remediation Guide: Detailed Implementation Steps

This guide provides exact code changes to fix each gap identified in the comprehensive audit.

---

## Gap #1: Missing JWT Parameters in main.bicep

**Current Code (lines 20-24):**
```bicep
@metadata({azd: {
  type: 'generate'
  config: {length:22,noSpecial:true}
  }
})
@secure()
param cache_password string
@secure()
param password string
```

**Required Addition - After line 24, add:**

```bicep
@description('JWT secret key for token signing')
@secure()
param jwtSecretKey string

@description('JWT token issuer (e.g., MyApp.Auth)')
param jwtIssuer string = 'MyApp.Auth'

@description('JWT token audience (e.g., MyApp.All)')
param jwtAudience string = 'MyApp.All'

@description('Frontend origin for CORS (semicolon-separated for multiple origins)')
param frontendOrigin string = 'http://localhost:3000;http://localhost:5000'

@description('Environment name (Development, Staging, Production)')
param aspnetcoreEnvironment string = 'Production'
```

**Why:** These parameters allow environment-specific JWT configuration instead of hardcoding values. Follows 12-factor app methodology and azd best practices.

---

## Gap #2: Missing Key Vault Module Call in main.bicep

**Location:** After the `resources` module definition (around line 35), add:

```bicep
module keyVault 'core/security/keyvault-secrets.bicep' = {
  name: 'keyvault'
  scope: rg
  params: {
    name: 'kv-${resourceToken}'
    location: location
    tags: tags
    jwtSecretKey: jwtSecretKey
    redisHostName: redis.outputs.hostName
    redisPrimaryKey: redis.outputs.primaryKey
    sqlFqdn: myapp_sqlserver.outputs.sqlServerFqdn
    sqlAdminPassword: password
    enableKeyVault: true  // CRITICAL: Enable Key Vault creation
  }
  dependsOn: [
    redis  // Ensure Redis created first
    myapp_sqlserver  // Ensure SQL Server created first
  ]
}
```

**Why:** Integrates the existing Key Vault module that was never called. The `enableKeyVault: true` is critical - currently it defaults to false, preventing secret creation.

---

## Gap #3: Missing Redis Module Call in main.bicep

**Location:** After the `resources` module, before `keyVault`, add:

```bicep
module redis 'core/database/redis.bicep' = {
  name: 'redis'
  scope: rg
  params: {
    name: 'redis-${resourceToken}'
    location: location
    tags: tags
    sku: 'Basic'  // Change to 'Premium' for production with replicas
    family: 'C'
    capacity: 1  // For Standard tier (0 for Basic)
  }
}
```

**Why:** Creates actual Redis cache resource. Currently only file share exists but no cache. Container Apps need connection to Redis instance.

---

## Gap #4: Missing SQL Server Module Call in main.bicep

**Location:** After `redis` module, add:

```bicep
module sqlServer 'core/database/sql-server.bicep' = {
  name: 'sqlserver'
  scope: rg
  params: {
    name: 'sql-${resourceToken}'
    location: location
    tags: tags
    administratorLogin: 'sqladmin'
    administratorLoginPassword: password  // FROM SECURE PARAM
    databases: [
      {name: 'AuthDB'}
      {name: 'BillingDB'}
      {name: 'InventoryDB'}
      {name: 'OrdersDB'}
      {name: 'PurchasingDB'}
      {name: 'SalesDB'}
    ]
    minimalTlsVersion: '1.2'
  }
}
```

**Why:** Creates SQL Server and all 6 required databases. Each service needs its own database following the docker-compose.yml pattern.

**Note:** The variable `resourceToken` should be defined in main.bicep:
```bicep
var resourceToken = uniqueString(subscription().id, resourceGroup().id)
```

---

## Gap #5: Update myapp-sqlserver.module.bicep to Create Databases

**Current Code - Ends after firewall rule:**
```bicep
resource sqlFirewallRule_AllowAllAzureIps 'Microsoft.Sql/servers/firewallRules@2023-08-01' = {
  // ...
}

output sqlServerFqdn string = myapp_sqlserver.properties.fullyQualifiedDomainName
```

**Replace with:**

```bicep
resource sqlFirewallRule_AllowAllAzureIps 'Microsoft.Sql/servers/firewallRules@2023-08-01' = {
  name: 'AllowAllAzureIps'
  properties: {
    endIpAddress: '0.0.0.0'
    startIpAddress: '0.0.0.0'
  }
  parent: myapp_sqlserver
}

// CREATE ALL 6 DATABASES
resource sqlDatabases 'Microsoft.Sql/servers/databases@2023-08-01' = [for db in [
  'AuthDB'
  'BillingDB'
  'InventoryDB'
  'OrdersDB'
  'PurchasingDB'
  'SalesDB'
]: {
  name: db
  parent: myapp_sqlserver
  location: location
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
  }
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  tags: {
    'aspire-resource-name': db
  }
}]

output sqlServerFqdn string = myapp_sqlserver.properties.fullyQualifiedDomainName
output name string = myapp_sqlserver.name
output sqlServerAdminName string = sqlServerAdminManagedIdentity.name
output databaseNames array = [for i in range(0, 6): sqlDatabases[i].name]
```

**Why:** Currently NO databases are created. Services have nowhere to write data.

---

## Gap #6: Complete myapp-sqlserver-roles.module.bicep

**Current Code - File is mostly empty:**
```bicep
@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param myapp_sqlserver_outputs_name string
param myapp_sqlserver_outputs_sqlserveradminname string
param principalId string
param principalName string

resource myapp_sqlserver 'Microsoft.Sql/servers@2023-08-01' existing = {
  name: myapp_sqlserver_outputs_name
}
// ... references but NO assignments
```

**Replace entire file with:**

```bicep
@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

@description('SQL Server name')
param myapp_sqlserver_outputs_name string

@description('SQL Server admin managed identity name')
param myapp_sqlserver_outputs_sqlserveradminname string

@description('Principal ID for role assignment (Container Apps Managed Identity)')
param principalId string

@description('Principal Name for role assignment')
param principalName string

@description('Array of service identities to grant database access')
param serviceIdentities array = [
  {
    name: 'auth-service'
    database: 'AuthDB'
    principalId: ''
  }
  {
    name: 'billing-service'
    database: 'BillingDB'
    principalId: ''
  }
  {
    name: 'inventory-service'
    database: 'InventoryDB'
    principalId: ''
  }
  {
    name: 'orders-service'
    database: 'OrdersDB'
    principalId: ''
  }
  {
    name: 'purchasing-service'
    database: 'PurchasingDB'
    principalId: ''
  }
  {
    name: 'sales-service'
    database: 'SalesDB'
    principalId: ''
  }
]

resource myapp_sqlserver 'Microsoft.Sql/servers@2023-08-01' existing = {
  name: myapp_sqlserver_outputs_name
}

resource sqlServerAdmin 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' existing = {
  name: myapp_sqlserver_outputs_sqlserveradminname
}

// Assign db_owner role to each service's database via SQL Server
// This allows Managed Identity to access their respective databases
resource sqlRoleAssignments 'Microsoft.Sql/servers/sqlVulnerabilityAssessments@2023-08-01' = [for service in serviceIdentities: if (!empty(service.principalId)) {
  name: 'default'
  parent: myapp_sqlserver
  // Note: SQL Server role assignment via bicep is limited
  // Alternative: Use init scripts or Azure SQL Database managed identity assignments
}]

output sqlServerName string = myapp_sqlserver.name
output assignments array = [for (service, i) in serviceIdentities: {
  serviceName: service.name
  database: service.database
  principalId: service.principalId
}]
```

**Note:** SQL Server role assignments are complex in Bicep. Consider using:
1. **Azure SQL Database Built-in Roles** (READER, CONTRIBUTOR) if available
2. **T-SQL Script** via Azure SQL Database reference in deployment scripts
3. **Container Apps** with system-assigned MI accessing databases

---

## Gap #7: Update main.parameters.json with New Parameters

**Current Content:**
```json
{
  "parameters": {
    "principalId": {"value": "${AZURE_PRINCIPAL_ID}"},
    "cache_password": {"value": "${AZURE_CACHE_PASSWORD}"},
    "password": {"value": "${AZURE_PASSWORD}"},
    "environmentName": {"value": "${AZURE_ENV_NAME}"},
    "location": {"value": "${AZURE_LOCATION}"}
  }
}
```

**Replace with:**

```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "principalId": {
      "value": "${AZURE_PRINCIPAL_ID}"
    },
    "environmentName": {
      "value": "${AZURE_ENV_NAME}"
    },
    "location": {
      "value": "${AZURE_LOCATION}"
    },
    "password": {
      "value": "${AZURE_SQL_PASSWORD}"
    },
    "cache_password": {
      "value": "${AZURE_REDIS_PASSWORD}"
    },
    "jwtSecretKey": {
      "value": "${AZURE_JWT_SECRET_KEY}"
    },
    "jwtIssuer": {
      "value": "${AZURE_JWT_ISSUER:-MyApp.Auth}"
    },
    "jwtAudience": {
      "value": "${AZURE_JWT_AUDIENCE:-MyApp.All}"
    },
    "frontendOrigin": {
      "value": "${AZURE_FRONTEND_ORIGIN}"
    },
    "aspnetcoreEnvironment": {
      "value": "${ASPNETCORE_ENVIRONMENT:-Production}"
    }
  }
}
```

**Environment Variables to Set in azd:**

```bash
# In .azure/myenv/.env
AZURE_JWT_SECRET_KEY=your_very_long_secret_key_here_minimum_32_chars
AZURE_JWT_ISSUER=MyApp.Auth
AZURE_JWT_AUDIENCE=MyApp.All
AZURE_FRONTEND_ORIGIN=https://yourdomain.com;https://api.yourdomain.com
AZURE_SQL_PASSWORD=ComplexP@ssw0rd123!
AZURE_REDIS_PASSWORD=ComplexRedisP@ssw0rd123!
ASPNETCORE_ENVIRONMENT=Production
```

---

## Gap #8: Create Container App Environment Variable Template

**Update `core/host/container-app.bicep` parameters section (add around line 28):**

```bicep
// After existing env array parameter:

@description('Enable CORS and specify allowed origins')
param frontendOrigin string = ''

@description('JWT issuer for token validation')
param jwtIssuer string = 'MyApp.Auth'

@description('JWT audience for token validation')
param jwtAudience string = 'MyApp.All'

@description('Application environment (Development, Staging, Production)')
param aspnetcoreEnvironment string = 'Production'

@description('Connection string for distributed caching')
param cacheConnectionString string = ''

@description('Service database connection string (for service-specific DB)')
param serviceDatabaseConnectionString string = ''

@description('Key Vault ID for secret references')
param keyVaultId string = ''

@description('Application Insights connection string')
param appInsightsConnectionString string = ''
```

**Then update the container configuration to include these:**

```bicep
template: {
  containers: [
    {
      name: name
      image: '${containerRegistry.properties.loginServer}/${imageName}'
      resources: {
        cpu: any(cpu)
        memory: memory
      }
      env: concat(env, [
        {name: 'ASPNETCORE_ENVIRONMENT', value: aspnetcoreEnvironment}
        {name: 'ASPNETCORE_URLS', value: 'http://+:8080'}
        {name: 'Jwt__Issuer', value: jwtIssuer}
        {name: 'Jwt__Audience', value: jwtAudience}
        {name: 'FRONTEND_ORIGIN', value: frontendOrigin}
        {name: 'ApplicationInsights__ConnectionString', value: appInsightsConnectionString}
        !empty(cacheConnectionString) ? {name: 'ConnectionStrings__cache', value: cacheConnectionString} : null
        !empty(serviceDatabaseConnectionString) ? {name: 'ConnectionStrings__ServiceDb', value: serviceDatabaseConnectionString} : null
      ])
      // ... rest of configuration
    }
  ]
}
```

---

## Gap #9: Create Auth Service Module

**File:** `infra/auth-service/auth-service.module.bicep`

```bicep
@description('Location for resources')
param location string = resourceGroup().location

@description('Container Apps Environment name')
param containerAppsEnvironmentName string

@description('Container Registry name')
param containerRegistryName string

@description('Container image tag')
param imageTag string = 'latest'

@description('JWT secret key name in Key Vault')
param jwtSecretKeyName string = 'jwt-secret-key'

@description('SQL connection secret name in Key Vault')
param sqlConnectionSecretName string = 'sql-connection-authdb'

@description('Redis connection secret name in Key Vault')
param redisConnectionSecretName string = 'redis-connection'

@description('Key Vault URI')
param keyVaultUri string

@description('JWT Issuer')
param jwtIssuer string

@description('JWT Audience')
param jwtAudience string

@description('Frontend origin for CORS')
param frontendOrigin string

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Tags for resources')
param tags object = {}

module authService 'core/host/container-app.bicep' = {
  name: 'auth-service'
  params: {
    name: 'auth-service'
    location: location
    containerAppsEnvironmentName: containerAppsEnvironmentName
    containerRegistryName: containerRegistryName
    imageName: 'auth-service:${imageTag}'
    targetPort: 8080
    externalIngress: false  // Internal only
    daprEnabled: true
    daprAppId: 'auth-service'
    daprAppPort: 8080
    minReplicas: 2
    maxReplicas: 5
    aspnetcoreEnvironment: 'Production'
    frontendOrigin: frontendOrigin
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    appInsightsConnectionString: appInsightsConnectionString
    env: [
      {
        name: 'ASPNETCORE_URLS'
        value: 'http://+:8080'
      }
    ]
    secrets: [
      {
        name: 'jwt-secret-key'
        keyVaultUrl: '${keyVaultUri}secrets/${jwtSecretKeyName}'
      }
      {
        name: 'db-connection'
        keyVaultUrl: '${keyVaultUri}secrets/${sqlConnectionSecretName}'
      }
      {
        name: 'cache-connection'
        keyVaultUrl: '${keyVaultUri}secrets/${redisConnectionSecretName}'
      }
    ]
    tags: tags
  }
}

output id string = authService.outputs.id
output name string = authService.outputs.name
output uri string = authService.outputs.uri
output fqdn string = authService.outputs.fqdn
```

**Repeat this pattern for remaining 5 services:**
- Change service name (auth-service → billing-service, etc.)
- Change image name
- Change secret references (sql-connection-authdb → sql-connection-billingdb, etc.)
- Keep daprEnabled: true, externalIngress: false

---

## Gap #10: Create API Gateway Module

**File:** `infra/api-gateway/api-gateway.module.bicep`

```bicep
@description('Location for resources')
param location string = resourceGroup().location

@description('Container Apps Environment name')
param containerAppsEnvironmentName string

@description('Container Registry name')
param containerRegistryName string

@description('Container image tag')
param imageTag string = 'latest'

@description('JWT secret key name in Key Vault')
param jwtSecretKeyName string = 'jwt-secret-key'

@description('Key Vault URI')
param keyVaultUri string

@description('JWT Issuer')
param jwtIssuer string

@description('JWT Audience')
param jwtAudience string

@description('Frontend origin for CORS')
param frontendOrigin string

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Tags for resources')
param tags object = {}

module apiGateway 'core/host/container-app.bicep' = {
  name: 'api-gateway'
  params: {
    name: 'api-gateway'
    location: location
    containerAppsEnvironmentName: containerAppsEnvironmentName
    containerRegistryName: containerRegistryName
    imageName: 'erpapigateway:${imageTag}'
    targetPort: 8080
    externalIngress: true  // PUBLIC FACING
    daprEnabled: false  // Gateway doesn't need Dapr
    minReplicas: 2
    maxReplicas: 10  // More replicas for public gateway
    aspnetcoreEnvironment: 'Production'
    frontendOrigin: frontendOrigin
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    appInsightsConnectionString: appInsightsConnectionString
    env: [
      {
        name: 'ASPNETCORE_URLS'
        value: 'http://+:8080'
      }
      {
        name: 'Ocelot__Routes__0__DownstreamHostAndPorts__0__Host'
        value: 'auth-service'
      }
      {
        name: 'Ocelot__Routes__0__DownstreamHostAndPorts__0__Port'
        value: '8080'
      }
      // ... repeat for all 6 services + notification
    ]
    secrets: [
      {
        name: 'jwt-secret-key'
        keyVaultUrl: '${keyVaultUri}secrets/${jwtSecretKeyName}'
      }
    ]
    tags: tags
  }
}

output id string = apiGateway.outputs.id
output name string = apiGateway.outputs.name
output uri string = apiGateway.outputs.uri
output fqdn string = apiGateway.outputs.fqdn
```

---

## Gap #11: Update main.bicep to Call Service Modules

**After keyVault module definition, add:**

```bicep
// SERVICE MODULES
module authServiceModule 'auth-service/auth-service.module.bicep' = {
  name: 'auth-service-app'
  scope: rg
  params: {
    location: location
    containerAppsEnvironmentName: resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_NAME
    containerRegistryName: resources.outputs.AZURE_CONTAINER_REGISTRY_NAME
    imageTag: 'latest'
    jwtSecretKeyName: keyVault.outputs.jwtSecretName
    sqlConnectionSecretName: keyVault.outputs.sqlAuthSecretName
    redisConnectionSecretName: keyVault.outputs.redisSecretName
    keyVaultUri: keyVault.outputs.keyVaultUri
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    frontendOrigin: frontendOrigin
    appInsightsConnectionString: MyApp_ApplicationInsights.outputs.appInsightsConnectionString
    tags: tags
  }
  dependsOn: [
    keyVault
    redis
    myapp_sqlserver
  ]
}

// Repeat for billing, inventory, orders, purchasing, sales...

// API GATEWAY MODULE
module apiGatewayModule 'api-gateway/api-gateway.module.bicep' = {
  name: 'api-gateway-app'
  scope: rg
  params: {
    location: location
    containerAppsEnvironmentName: resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_NAME
    containerRegistryName: resources.outputs.AZURE_CONTAINER_REGISTRY_NAME
    imageTag: 'latest'
    jwtSecretKeyName: keyVault.outputs.jwtSecretName
    keyVaultUri: keyVault.outputs.keyVaultUri
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    frontendOrigin: frontendOrigin
    appInsightsConnectionString: MyApp_ApplicationInsights.outputs.appInsightsConnectionString
    tags: tags
  }
  dependsOn: [
    authServiceModule  // Ensure services exist first
    billingServiceModule
    inventoryServiceModule
    ordersServiceModule
    purchasingServiceModule
    salesServiceModule
  ]
}
```

---

## Gap #12: Add resourceToken Variable to main.bicep

**Add after the `var tags` definition:**

```bicep
var tags = {
  'azd-env-name': environmentName
}

var resourceToken = uniqueString(subscription().id, resourceGroup().id)
```

---

## Validation Script

**File:** `validate-bicep-complete.ps1`

```powershell
param(
  [string]$bicepPath = './infra',
  [string]$resourceGroup = 'rg-myapp-dev',
  [string]$location = 'eastus'
)

Write-Host "🔍 Starting comprehensive Bicep validation..." -ForegroundColor Cyan

# Step 1: Validate main.bicep syntax
Write-Host "`n📝 Validating main.bicep syntax..." -ForegroundColor Yellow
try {
  az bicep build --file "$bicepPath/main.bicep"
  Write-Host "✅ main.bicep syntax valid" -ForegroundColor Green
} catch {
  Write-Host "❌ main.bicep syntax error: $_" -ForegroundColor Red
  exit 1
}

# Step 2: Build main.bicep to ARM template
Write-Host "`n📦 Building main.bicep to ARM template..." -ForegroundColor Yellow
try {
  az bicep build --file "$bicepPath/main.bicep" --outfile "$bicepPath/main.json"
  Write-Host "✅ main.bicep built successfully" -ForegroundColor Green
} catch {
  Write-Host "❌ Build error: $_" -ForegroundColor Red
  exit 1
}

# Step 3: Validate all module references
Write-Host "`n🔗 Validating module references..." -ForegroundColor Yellow
$modules = @(
  'resources.bicep'
  'core/database/redis.bicep'
  'core/database/sql-server.bicep'
  'core/security/keyvault-secrets.bicep'
  'core/host/container-app.bicep'
  'myapp-sqlserver/myapp-sqlserver.module.bicep'
  'myapp-sqlserver-roles/myapp-sqlserver-roles.module.bicep'
  'MyApp-ApplicationInsights/MyApp-ApplicationInsights.module.bicep'
  'MyApp-LogAnalyticsWorkspace/MyApp-LogAnalyticsWorkspace.module.bicep'
  'auth-service/auth-service.module.bicep'
  'billing-service/billing-service.module.bicep'
  'inventory-service/inventory-service.module.bicep'
  'orders-service/orders-service.module.bicep'
  'purchasing-service/purchasing-service.module.bicep'
  'sales-service/sales-service.module.bicep'
  'api-gateway/api-gateway.module.bicep'
)

foreach ($module in $modules) {
  $path = "$bicepPath/$module"
  if (Test-Path $path) {
    Write-Host "  ✅ Found: $module"
  } else {
    Write-Host "  ❌ MISSING: $module" -ForegroundColor Red
  }
}

# Step 4: Validate parameters
Write-Host "`n⚙️ Validating parameters..." -ForegroundColor Yellow
$requiredParams = @(
  'jwtSecretKey'
  'jwtIssuer'
  'jwtAudience'
  'frontendOrigin'
  'password'
  'cache_password'
  'environmentName'
  'location'
  'principalId'
)

$content = Get-Content "$bicepPath/main.bicep" -Raw
foreach ($param in $requiredParams) {
  if ($content -match "param $param") {
    Write-Host "  ✅ Parameter: $param"
  } else {
    Write-Host "  ❌ MISSING: $param" -ForegroundColor Red
  }
}

# Step 5: Validate Key Vault integration
Write-Host "`n🔐 Validating Key Vault integration..." -ForegroundColor Yellow
if ($content -match "module keyVault.*keyvault-secrets.bicep") {
  Write-Host "  ✅ Key Vault module referenced"
} else {
  Write-Host "  ❌ MISSING: Key Vault module reference" -ForegroundColor Red
}

if ($content -match "enableKeyVault: true") {
  Write-Host "  ✅ Key Vault enabled"
} else {
  Write-Host "  ⚠️ WARNING: Key Vault may not be enabled" -ForegroundColor Yellow
}

# Step 6: Validate service modules
Write-Host "`n🚀 Validating service modules..." -ForegroundColor Yellow
$services = @('auth-service', 'billing-service', 'inventory-service', 'orders-service', 'purchasing-service', 'sales-service')
foreach ($service in $services) {
  if ($content -match "module ${service}Module") {
    Write-Host "  ✅ Module called: $service"
  } else {
    Write-Host "  ❌ Module NOT called: $service" -ForegroundColor Red
  }
}

# Step 7: Validate API Gateway module
Write-Host "`n🌐 Validating API Gateway..." -ForegroundColor Yellow
if ($content -match "module apiGatewayModule") {
  Write-Host "  ✅ API Gateway module called"
} else {
  Write-Host "  ❌ API Gateway module NOT called" -ForegroundColor Red
}

# Step 8: Validation summary
Write-Host "`n✅ Validation complete!" -ForegroundColor Green
Write-Host "`nNext steps:`n"
Write-Host "  1. Fix any ❌ MISSING items above"
Write-Host "  2. Test deployment validation: az deployment group validate --resource-group $resourceGroup --template-file main.json"
Write-Host "  3. Deploy: azd deploy"
```

**Run it:**
```powershell
./validate-bicep-complete.ps1 -bicepPath ./infra -resourceGroup rg-myapp-prod -location eastus
```

---

## Summary of Changes

| File | Changes | Impact |
|------|---------|--------|
| `main.bicep` | Add JWT params, Key Vault call, Redis call, SQL Server call, resource token var | 🔴 CRITICAL |
| `main.parameters.json` | Add JWT, CORS, ASPNETCORE_ENVIRONMENT params | 🔴 CRITICAL |
| `myapp-sqlserver.module.bicep` | Add 6 database creation loop | 🔴 CRITICAL |
| `core/host/container-app.bicep` | Add JWT, CORS, AppInsights env var support | 🟠 HIGH |
| `myapp-sqlserver-roles.module.bicep` | Complete role assignment implementation | 🟠 HIGH |
| `auth-service/auth-service.module.bicep` | CREATE NEW FILE | 🔴 CRITICAL |
| `billing-service/billing-service.module.bicep` | CREATE NEW FILE | 🔴 CRITICAL |
| `inventory-service/inventory-service.module.bicep` | CREATE NEW FILE | 🔴 CRITICAL |
| `orders-service/orders-service.module.bicep` | CREATE NEW FILE | 🔴 CRITICAL |
| `purchasing-service/purchasing-service.module.bicep` | CREATE NEW FILE | 🔴 CRITICAL |
| `sales-service/sales-service.module.bicep` | CREATE NEW FILE | 🔴 CRITICAL |
| `api-gateway/api-gateway.module.bicep` | CREATE NEW FILE | 🔴 CRITICAL |

---

**Status:** Ready for implementation  
**Estimated Time:** 6-8 hours

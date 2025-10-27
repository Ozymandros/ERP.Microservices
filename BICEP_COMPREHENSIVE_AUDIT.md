# 🔍 Bicep Infrastructure Comprehensive Audit Report

**Date:** October 27, 2025  
**Focus:** Alignment with `azd` best practices & `docker-compose.yml` pattern  
**Scope:** All `.bicep` files in `/infra` directory

---

## Executive Summary

After systematic review of the Bicep infrastructure against `azd` best practices and the `docker-compose.yml` reference pattern, **11 critical gaps** have been identified. The infrastructure requires significant updates to achieve production-readiness.

### 🔴 Critical Findings:
- **Zero (0) service modules** exist for 6 microservices (Auth, Billing, Inventory, Orders, Purchasing, Sales)
- **Zero (0) API Gateway** module exists
- **Key Vault is NOT integrated** into main.bicep (defined but unused)
- **Missing JWT secret parameters** in main.bicep and main.parameters.json
- **Missing database initialization** for microservice databases
- **No Dapr sidecar configuration** in container app template
- **Missing service-to-service communication** setup
- **No CORS/FRONTEND_ORIGIN** configuration support
- **Incomplete secret management** strategy
- **No managed identity** setup for database access

---

## 📋 File-by-File Analysis

### 1. `main.bicep` - ❌ INCOMPLETE

**Current State:**
```bicep
targetScope = 'subscription'
param environmentName string
param location string
param principalId string = ''
@secure()
param cache_password string
@secure()
param password string
```

**Issues Identified:**

| Issue | Severity | Details |
|-------|----------|---------|
| Missing JWT secret parameter | 🔴 CRITICAL | `Jwt__SecretKey` not defined as secure parameter |
| Missing frontend origin parameter | 🔴 CRITICAL | `FRONTEND_ORIGIN` env var not supported |
| Missing JWT issuer/audience params | 🔴 CRITICAL | `Jwt__Issuer`, `Jwt__Audience` hardcoded in docker-compose |
| Key Vault not referenced | 🔴 CRITICAL | `keyvault-secrets.bicep` defined but never called |
| No service modules | 🔴 CRITICAL | 6 services + gateway have no modules |
| Missing database list | 🟠 HIGH | No way to specify which databases to create |
| Parameter naming inconsistent | 🟡 MEDIUM | `cache_password` vs `password` vs best practice naming |

**Comparison with `docker-compose.yml` requirements:**

| docker-compose.yml Pattern | Bicep Implementation | Status |
|---------------------------|----------------------|--------|
| `Jwt__SecretKey` | ❌ Missing parameter | Missing |
| `Jwt__Issuer` | ❌ Hardcoded in keyvault-secrets.bicep | Missing |
| `Jwt__Audience` | ❌ Hardcoded in keyvault-secrets.bicep | Missing |
| `FRONTEND_ORIGIN` | ❌ Not in main.bicep | Missing |
| `ConnectionStrings__cache` | ❌ Not passed to services | Missing |
| Service-specific databases | ❌ No service modules | Missing |
| `ASPNETCORE_ENVIRONMENT` | ❌ Not set in container app template | Missing |

**What `azd` does (reference pattern):**
```bicep
// azd includes parameters for EVERY environment variable
@secure()
param jwtSecretKey string
param jwtIssuer string
param jwtAudience string
param frontendOrigin string
// Then passes them via module outputs to service modules
```

---

### 2. `main.parameters.json` - ❌ INCOMPLETE

**Current State:**
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

**Issues Identified:**

| Issue | Severity | Details |
|-------|----------|---------|
| **Missing JWT secret** | 🔴 CRITICAL | `jwtSecretKey` not in parameters |
| **Missing JWT issuer** | 🔴 CRITICAL | `jwtIssuer` not in parameters |
| **Missing JWT audience** | 🔴 CRITICAL | `jwtAudience` not in parameters |
| **Missing FRONTEND_ORIGIN** | 🔴 CRITICAL | Not configurable via azd |
| **Missing SQL database list** | 🔴 CRITICAL | No way to specify 6 service databases |
| **Parameter naming inconsistent** | 🟡 MEDIUM | Should be `azure_jwt_secret_key` or similar |
| **Missing environment identifier** | 🟡 MEDIUM | No way to distinguish Dev/Staging/Prod secrets |

**azd Best Practice Pattern:**
```json
{
  "parameters": {
    "jwtSecretKey": {"value": "${AZURE_JWT_SECRET_KEY}"},
    "jwtIssuer": {"value": "${AZURE_JWT_ISSUER}"},
    "jwtAudience": {"value": "${AZURE_JWT_AUDIENCE}"},
    "frontendOrigin": {"value": "${AZURE_FRONTEND_ORIGIN}"},
    "sqlServerAdminPassword": {"value": "${AZURE_SQL_PASSWORD}"},
    "serviceDatabaseConfigs": {
      "value": [
        {"name": "AuthDB", "collation": "SQL_Latin1_General_CP1_CI_AS"},
        {"name": "BillingDB", ...},
        // ... 6 databases total
      ]
    }
  }
}
```

---

### 3. `resources.bicep` - 🟡 PARTIAL

**Current State:**
```bicep
// Creates:
- Managed Identity
- Container Registry
- Log Analytics Workspace
- Storage Volume (for Cache Redis)
- Container App Environment
- Cache Redis File Share
```

**Issues Identified:**

| Issue | Severity | Details |
|-------|----------|---------|
| **No Key Vault resource** | 🔴 CRITICAL | Should be created here with Managed Identity access |
| **No Redis Cache instance** | 🔴 CRITICAL | File share created but no actual Redis resource |
| **No SQL Server resource** | 🔴 CRITICAL | Module calls it but resources.bicep doesn't create it |
| **No database initialization** | 🟠 HIGH | No script to create 6 service databases |
| **Managed Identity has no Key Vault access** | 🟠 HIGH | `Get` secret permission not granted |
| **No Application Insights reference** | 🟠 HIGH | Should wire MI to AppInsights |
| **Hard-coded resource tokens** | 🟡 MEDIUM | `uniqueString(resourceGroup().id)` used directly (not parameterized) |

**What should be here (following `azd`):**
```bicep
// Should include:
- Key Vault with Managed Identity access policy
- Redis Cache (not just file share)
- SQL Server (delegated to module, but referenced)
- Role assignments for MI → Key Vault Get secrets
- Role assignments for MI → Container Registry pull
```

---

### 4. `core/security/keyvault-secrets.bicep` - ❌ NOT INTEGRATED

**Current State:**
```bicep
param enableKeyVault bool = false
// Conditionally creates Key Vault with secrets:
- redis-connection
- jwt-secret-key
- sql-connection-authdb
- sql-connection-billingdb
- sql-connection-inventorydb
- sql-connection-ordersdb
- sql-connection-purchasingdb
- sql-connection-salesdb
```

**Critical Issues:**

| Issue | Severity | Details |
|-------|----------|---------|
| **NOT CALLED FROM main.bicep** | 🔴 CRITICAL | Module exists but never invoked |
| **enableKeyVault parameter always false** | 🔴 CRITICAL | Secrets never created |
| **Hardcoded connection strings** | 🟠 HIGH | Should accept sqlFqdn as parameter |
| **No access policies** | 🟠 HIGH | Managed Identity can't read these secrets |
| **Missing Ocelot routing secrets** | 🟠 HIGH | No `ocelot-config` secret for API Gateway |
| **No schema validation** | 🟡 MEDIUM | Should validate secret names match Container App references |

**Correct Pattern (azd):**
```bicep
// main.bicep MUST call this:
module keyVault 'core/security/keyvault-secrets.bicep' = {
  name: 'keyvault'
  scope: rg
  params: {
    name: 'kv-${resourceToken}'
    location: location
    jwtSecretKey: jwtSecretKey  // FROM MAIN PARAMETER
    redisHostName: redis.outputs.hostName
    redisPrimaryKey: redis.outputs.primaryKey
    sqlFqdn: sqlServer.outputs.fqdn
    sqlAdminPassword: password
    enableKeyVault: true  // MUST BE TRUE
  }
}

// Then pass to services:
output kvJwtSecretName string = keyVault.outputs.jwtSecretName
```

---

### 5. `core/database/redis.bicep` - 🟡 PARTIAL

**Current State:**
```bicep
// Generic Redis template with:
- SKU, family, capacity parameterized
- TLS 1.2 enforced
- Outputs: id, name, hostName, sslPort, primaryKey
```

**Issues Identified:**

| Issue | Severity | Details |
|-------|----------|---------|
| **NOT CALLED FROM main.bicep** | 🔴 CRITICAL | Module exists but never invoked |
| **Public network access enabled** | 🟠 HIGH | Should restrict to Container App ENV only |
| **No Private Endpoint support** | 🟠 HIGH | Production should use private link |
| **Naming not parameterized** | 🟡 MEDIUM | Should accept name prefix parameter |
| **No monitoring reference** | 🟡 MEDIUM | Should accept diagnosticSettingsName |

**How it should be called:**
```bicep
module redis 'core/database/redis.bicep' = {
  name: 'redis'
  scope: rg
  params: {
    name: 'redis-${resourceToken}'
    location: location
    sku: 'Standard'  // or parameterized
    family: 'C'
    capacity: 1  // For Standard tier
    tags: tags
  }
}
```

---

### 6. `core/database/sql-server.bicep` - 🟡 PARTIAL

**Current State:**
```bicep
// Generic SQL Server template with:
- Admin login parameterized
- Firewall rules (AllowAllAzureIps)
- Database array support
- TLS 1.2 enforced
```

**Issues Identified:**

| Issue | Severity | Details |
|-------|----------|---------|
| **NOT CALLED FROM main.bicep** | 🔴 CRITICAL | Should be called with database list |
| **Admin password passed in plaintext** | 🟠 HIGH | Should be @secure() parameter |
| **No audit logging** | 🟠 HIGH | Should enable server audit settings |
| **No firewall restrictions** | 🟠 HIGH | Opens to `0.0.0.0/0` for Azure services only (ok) but should be more restrictive |
| **No RBAC-based access** | 🟠 HIGH | Should use SQL Managed Identity instead of SQL admin |
| **Database loop not tested** | 🟡 MEDIUM | The `[for db in databases: {...}]` syntax works but untested |

**What it should look like:**
```bicep
module sqlServer 'core/database/sql-server.bicep' = {
  name: 'sqlserver'
  scope: rg
  params: {
    name: 'sql-${resourceToken}'
    location: location
    administratorLogin: 'sqladmin'
    administratorLoginPassword: password  // FROM SECURE PARAM
    databases: [
      {name: 'AuthDB'},
      {name: 'BillingDB'},
      {name: 'InventoryDB'},
      {name: 'OrdersDB'},
      {name: 'PurchasingDB'},
      {name: 'SalesDB'}
    ]
    tags: tags
  }
}
```

---

### 7. `core/host/container-app.bicep` - 🟡 PARTIAL

**Current State:**
```bicep
// Generic Container App template supporting:
- External/internal ingress
- Dapr sidecars
- Health checks (liveness & readiness)
- Environment variables
- Secrets management
- Min/max replicas
- CPU/Memory limits
```

**Issues Identified:**

| Issue | Severity | Details |
|-------|----------|---------|
| **No JWT environment variable support** | 🔴 CRITICAL | Needs params for all JWT-related envvars |
| **No connection string handling** | 🔴 CRITICAL | Missing params for service-specific database connections |
| **No Key Vault secret reference** | 🔴 CRITICAL | Should support `secretRef` with Key Vault references |
| **Dapr configuration minimal** | 🟠 HIGH | Missing `stateStores`, `pubsub` component bindings |
| **Health check endpoint hardcoded** | 🟠 HIGH | Should parameterize `/health` path |
| **No CORS configuration** | 🟠 HIGH | Should support `FRONTEND_ORIGIN` env var |
| **ACR pull role assignment wrong** | 🟠 HIGH | Role ID `7f951dda-4ed3-4680-a7ca-43fe172d538d` is `AcrPull` but should verify ACR context |
| **No Application Insights instrumentation** | 🟡 MEDIUM | Should auto-inject AppInsights connection string |

**How services should use this:**
```bicep
module authService 'core/host/container-app.bicep' = {
  name: 'auth-service'
  scope: rg
  params: {
    name: 'auth-service'
    containerAppsEnvironmentName: containerAppsEnvironmentName
    containerRegistryName: containerRegistryName
    imageName: 'auth-service:latest'
    daprEnabled: true
    daprAppId: 'auth-service'
    externalIngress: false  // Not public
    env: [
      {name: 'ASPNETCORE_ENVIRONMENT', value: 'Production'}
      {name: 'ASPNETCORE_URLS', value: 'http://+:8080'}
      {name: 'Jwt__Issuer', value: jwtIssuer}
      {name: 'Jwt__Audience', value: jwtAudience}
      {name: 'FRONTEND_ORIGIN', value: frontendOrigin}
      {name: 'ConnectionStrings__cache', value: 'redis-connection'}  // Reference secret
      {name: 'ConnectionStrings__AuthDb', value: 'sql-connection-authdb'}  // Reference secret
    ]
    secrets: [
      {name: 'jwt-secret-key', keyVaultUrl: keyVault.outputs.kvJwtSecretUri}
      {name: 'redis-connection', keyVaultUrl: keyVault.outputs.kvRedisSecretUri}
      {name: 'sql-connection-authdb', keyVaultUrl: keyVault.outputs.kvSqlAuthSecretUri}
    ]
  }
}
```

---

### 8-13. **Service Module Files - ❌ ALL MISSING**

**Missing modules:**
- `infra/auth-service/auth-service.module.bicep` ❌
- `infra/billing-service/billing-service.module.bicep` ❌
- `infra/inventory-service/inventory-service.module.bicep` ❌
- `infra/orders-service/orders-service.module.bicep` ❌
- `infra/purchasing-service/purchasing-service.module.bicep` ❌
- `infra/sales-service/sales-service.module.bicep` ❌

**Impact:** 🔴 **DEPLOYMENT BLOCKED** - No services can be deployed to Azure

---

### 14. **API Gateway Module - ❌ MISSING**

**Missing module:**
- `infra/api-gateway/api-gateway.module.bicep` ❌

**Impact:** 🔴 **DEPLOYMENT BLOCKED** - No public entry point to microservices

---

### 15. `myapp-sqlserver.module.bicep` - 🟠 INCOMPLETE

**Current State:**
```bicep
// Creates SQL Server with Managed Identity admin but NO DATABASES
resource myapp_sqlserver 'Microsoft.Sql/servers@2023-08-01' = {
  administrators: {
    administratorType: 'ActiveDirectory'
    login: sqlServerAdminManagedIdentity.name
    ...
  }
}
```

**Issues Identified:**

| Issue | Severity | Details |
|-------|----------|---------|
| **No databases created** | 🔴 CRITICAL | 6 databases missing - must create all 6 |
| **No parameters for database list** | 🔴 CRITICAL | Should accept array of database configs |
| **No Managed Identity setup for services** | 🟠 HIGH | Services should use MI for SQL auth, not password |
| **No output for connection strings** | 🟠 HIGH | Should export connection string templates |
| **Firewall rule name incorrect** | 🟡 MEDIUM | Should be parameterized |

**Should be updated to:**
```bicep
param databases array = [
  {name: 'AuthDB'},
  {name: 'BillingDB'},
  {name: 'InventoryDB'},
  {name: 'OrdersDB'},
  {name: 'PurchasingDB'},
  {name: 'SalesDB'}
]

resource sqlDatabases 'Microsoft.Sql/servers/databases@2023-05-01-preview' = [for db in databases: {
  name: db.name
  parent: myapp_sqlserver
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648
  }
}]
```

---

### 16. `myapp-sqlserver-roles.module.bicep` - ❌ INCOMPLETE

**Current State:**
```bicep
// References existing resources but does NOTHING
resource myapp_sqlserver 'Microsoft.Sql/servers@2023-08-01' existing = {
  name: myapp_sqlserver_outputs_name
}
resource sqlServerAdmin 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' existing = {
  name: myapp_sqlserver_outputs_sqlserveradminname
}
// ... but no role assignments!
```

**Issues Identified:**

| Issue | Severity | Details |
|-------|----------|---------|
| **No role assignments created** | 🔴 CRITICAL | File is empty - just references resources |
| **No purpose statement** | 🔴 CRITICAL | Unclear what this module should do |
| **Should create SQL roles** | 🟠 HIGH | Each service MI should have db_owner on their database |

---

### 17. `MyApp-ApplicationInsights.module.bicep` - 🟡 PARTIAL

**Current State:**
```bicep
resource MyApp_ApplicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  // Works but minimal
}
```

**Issues Identified:**

| Issue | Severity | Details |
|-------|----------|---------|
| **No sampling rules** | 🟠 HIGH | Should configure adaptive sampling |
| **No log retention** | 🟠 HIGH | Should set retention policy (30 days recommended) |
| **No export configuration** | 🟡 MEDIUM | Should wire to Log Analytics for long-term retention |
| **Hardcoded kind='web'** | 🟡 MEDIUM | Should be parameterized |

---

### 18. `MyApp-LogAnalyticsWorkspace.module.bicep` - 🟡 PARTIAL

**Current State:**
```bicep
resource MyApp_LogAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2025-02-01' = {
  sku: {name: 'PerGB2018'}
}
```

**Issues Identified:**

| Issue | Severity | Details |
|-------|----------|---------|
| **No retention configured** | 🟠 HIGH | Should set `retentionInDays` (default: 30) |
| **No daily quota** | 🟡 MEDIUM | Should set daily ingestion quota to prevent runaway costs |
| **No solutions installed** | 🟡 MEDIUM | Missing Container Monitoring, SQL Analytics solutions |

---

## 🆚 Comparison Matrix: docker-compose.yml vs Bicep

| Configuration | docker-compose.yml | Bicep | Gap | Severity |
|---------------|-------------------|-------|-----|----------|
| **JWT Secret** | `Jwt__SecretKey` ✅ | ❌ Missing param | ❌ | 🔴 CRITICAL |
| **JWT Issuer** | `Jwt__Issuer: MyApp.Auth` ✅ | ❌ Only in KeyVault | ❌ | 🔴 CRITICAL |
| **JWT Audience** | `Jwt__Audience: MyApp.All` ✅ | ❌ Only in KeyVault | ❌ | 🔴 CRITICAL |
| **Frontend Origin** | `FRONTEND_ORIGIN` ✅ | ❌ Missing | ❌ | 🔴 CRITICAL |
| **Cache Connection** | `ConnectionStrings__cache: redis:6379` ✅ | ✅ In template but not used | ⚠️ | 🟠 HIGH |
| **SQL Connections** | `ConnectionStrings__AuthDb` etc ✅ | ✅ In template but hardcoded | ⚠️ | 🟠 HIGH |
| **ASPNETCORE_ENVIRONMENT** | `Development` ✅ | ❌ Missing from container-app.bicep | ❌ | 🔴 CRITICAL |
| **ASPNETCORE_URLS** | `http://+:8080` ✅ | ✅ Assumed in template | ✅ | ✅ |
| **Service Count** | 6 services ✅ | ❌ Zero service modules | ❌ | 🔴 CRITICAL |
| **Database Count** | 6 databases ✅ | ❌ No initialization | ❌ | 🔴 CRITICAL |
| **Dapr Sidecars** | ✅ Configured per service | ⚠️ Generic template only | ⚠️ | 🟠 HIGH |
| **Health Checks** | ✅ SQL Server | ✅ `/health` endpoint | ✅ | ✅ |
| **Key Vault Secrets** | ❌ Not used (local only) | ✅ Defined but not integrated | ⚠️ | 🔴 CRITICAL |
| **Network** | Docker network | Container Apps Environment | ✅ | ✅ |
| **Registry** | Docker Hub | ACR | ✅ | ✅ |

---

## 🛠️ Root Cause Analysis

### Why are 10 modules missing?

**Theory:** The infrastructure was scaffolded from an `azd` template but services were NOT deployed, so the service-specific modules were never created. The `main.bicep` file references modules (`MyApp-ApplicationInsights`, `MyApp-LogAnalyticsWorkspace`, `myapp-sqlserver-roles`) that exist, but 6 critical service modules + gateway module were never added.

### Why isn't Key Vault integrated?

**Theory:** The `keyvault-secrets.bicep` template was created as a reference implementation but never connected to `main.bicep`. The `enableKeyVault` parameter defaults to `false`, so secrets are never created. This is likely intentional (for dev/test), but production deployment requires this to be enabled.

### Why is JWT configuration split?

**Theory:** JWT configuration is hard-coded in:
- `keyvault-secrets.bicep` (as "MyApp.Auth" issuer)
- `docker-compose.yml` (as environment variables)
- But NOT parameterized in `main.bicep`

This violates the 12-factor app principle: configuration should be environment-specific, not hard-coded.

### Why are services not created?

**Theory:** The solution was scaffolded but the service deployment pipeline was never completed. Each service needs a module that:
1. Pulls image from ACR
2. Configures all environment variables
3. Sets up database connections (from Key Vault)
4. Configures Dapr
5. Sets up ingress (internal for services, external for gateway)

---

## ✅ Recommendations: 7-Step Fix Plan

### **Priority 1: Enable Key Vault Integration** (1-2 hours)

```bicep
// main.bicep - ADD THIS:
@secure()
param jwtSecretKey string

@description('JWT token issuer')
param jwtIssuer string = 'MyApp.Auth'

@description('JWT token audience')
param jwtAudience string = 'MyApp.All'

@description('Frontend origin for CORS')
param frontendOrigin string = 'http://localhost:3000'

// Then call the module:
module keyVault 'core/security/keyvault-secrets.bicep' = {
  name: 'keyvault'
  scope: rg
  params: {
    name: 'kv-${resourceToken}'
    location: location
    jwtSecretKey: jwtSecretKey  // FROM PARAMETER
    redisHostName: redis.outputs.hostName
    redisPrimaryKey: redis.outputs.primaryKey
    sqlFqdn: sqlServer.outputs.fqdn
    sqlAdminPassword: password
    enableKeyVault: true  // ENABLE IT
  }
}
```

### **Priority 2: Create Service Modules** (4-6 hours)

Create each of:
- `infra/auth-service/auth-service.module.bicep`
- `infra/billing-service/billing-service.module.bicep`
- `infra/inventory-service/inventory-service.module.bicep`
- `infra/orders-service/orders-service.module.bicep`
- `infra/purchasing-service/purchasing-service.module.bicep`
- `infra/sales-service/sales-service.module.bicep`
- `infra/api-gateway/api-gateway.module.bicep`

Each should follow this pattern:
```bicep
module serviceName 'core/host/container-app.bicep' = {
  name: 'service-name'
  scope: rg
  params: {
    name: 'service-name'
    containerAppsEnvironmentName: containerAppsEnvironmentName
    imageName: 'service-name:${imageTag}'
    daprEnabled: true
    daprAppId: 'service-name'
    externalIngress: false  // true only for gateway
    env: [
      // All environment variables from docker-compose.yml
    ]
    secrets: [
      // References to Key Vault secrets
    ]
  }
}
```

### **Priority 3: Update SQL Server Module** (1 hour)

Add database creation:
```bicep
param databases array = [
  {name: 'AuthDB'},
  {name: 'BillingDB'},
  {name: 'InventoryDB'},
  {name: 'OrdersDB'},
  {name: 'PurchasingDB'},
  {name: 'SalesDB'}
]

resource sqlDatabases 'Microsoft.Sql/servers/databases@2023-05-01-preview' = [for db in databases: {
  name: db.name
  parent: myapp_sqlserver
  // ...
}]
```

### **Priority 4: Call Redis & SQL Modules** (30 minutes)

In `main.bicep`, add:
```bicep
module redis 'core/database/redis.bicep' = {
  name: 'redis'
  scope: rg
  params: {
    name: 'redis-${resourceToken}'
    location: location
    sku: 'Standard'
    family: 'C'
    capacity: 1
    tags: tags
  }
}

module sqlServer 'core/database/sql-server.bicep' = {
  name: 'sqlserver'
  scope: rg
  params: {
    name: 'sql-${resourceToken}'
    location: location
    administratorLogin: 'sqladmin'
    administratorLoginPassword: password
    databases: [
      {name: 'AuthDB'},
      {name: 'BillingDB'},
      // ...
    ]
    tags: tags
  }
}
```

### **Priority 5: Update main.parameters.json** (15 minutes)

```json
{
  "parameters": {
    "jwtSecretKey": {"value": "${AZURE_JWT_SECRET_KEY}"},
    "jwtIssuer": {"value": "${AZURE_JWT_ISSUER:-MyApp.Auth}"},
    "jwtAudience": {"value": "${AZURE_JWT_AUDIENCE:-MyApp.All}"},
    "frontendOrigin": {"value": "${AZURE_FRONTEND_ORIGIN}"},
    "password": {"value": "${AZURE_SQL_PASSWORD}"},
    "cache_password": {"value": "${AZURE_REDIS_PASSWORD}"}
  }
}
```

### **Priority 6: Implement SQL Roles Module** (1 hour)

Complete `myapp-sqlserver-roles.module.bicep`:
```bicep
resource dbOwnerRole 'Microsoft.Sql/servers/databases/roleAssignments@2021-02-01' = [for service in serviceIdentities: {
  // Assign each service MI to its database with db_owner role
}]
```

### **Priority 7: Validate & Test** (1-2 hours)

```powershell
# Validate main.bicep
az bicep validate --file main.bicep

# Validate parameters
az deployment group validate \
  --template-file main.bicep \
  --parameters main.parameters.json
```

---

## 📊 Dependency Graph

```
main.bicep
├── resources.bicep (core infra)
│   ├── Managed Identity
│   ├── Container Registry
│   ├── Log Analytics Workspace
│   └── Container App Environment
├── redis (core/database/redis.bicep) ⬅️ NEEDS CALL
├── sqlServer (core/database/sql-server.bicep) ⬅️ NEEDS CALL
├── keyVault (core/security/keyvault-secrets.bicep) ⬅️ NEEDS CALL
├── MyApp-ApplicationInsights (uses LogAnalytics) ✅
├── MyApp-LogAnalyticsWorkspace ✅
├── myapp_sqlserver (SQL server + firewall rules)
├── myapp_sqlserver_roles (SQL role assignments) ⬅️ INCOMPLETE
├── auth-service (core/host/container-app.bicep) ⬅️ MISSING MODULE
├── billing-service ⬅️ MISSING MODULE
├── inventory-service ⬅️ MISSING MODULE
├── orders-service ⬅️ MISSING MODULE
├── purchasing-service ⬅️ MISSING MODULE
├── sales-service ⬅️ MISSING MODULE
└── api-gateway ⬅️ MISSING MODULE
```

---

## 🔒 Security Best Practices Alignment

| azd Best Practice | Current Status | Required Action |
|-------------------|----------------|-----------------|
| Secrets in Key Vault | ⚠️ Defined but not used | Enable and integrate |
| Managed Identities for Azure services | ✅ Partially done | Complete SQL access setup |
| RBAC for role assignments | ✅ Partially done | Implement SQL role assignments |
| TLS 1.2 enforcement | ✅ Enabled | ✅ Already configured |
| Public network access restricted | ❌ SQL/Redis open to Azure | ⚠️ Add private endpoints for prod |
| Secure parameter handling | ❌ Missing JWT params | Add @secure() params |
| No hardcoded secrets | ⚠️ Partially done | Complete Key Vault integration |
| Container security scanning | ❌ Not configured | Enable ACR scanning |

---

## 🎯 Success Criteria

After implementing these fixes, the infrastructure should:

✅ **Parameter Management:**
- All secrets passed as @secure() parameters
- JWT configuration environment-specific
- No hardcoded values in bicep files

✅ **Service Deployment:**
- All 6 services deployable to Container Apps
- API Gateway publicly accessible
- Internal services not exposed

✅ **Configuration:**
- All environment variables from docker-compose.yml mapped to Bicep
- Connection strings sourced from Key Vault
- CORS properly configured

✅ **Security:**
- Managed Identities used for all resource access
- Key Vault secrets for all sensitive data
- SQL role assignments in place

✅ **Validation:**
- `az bicep validate` passes
- `az deployment group validate` passes
- No unresolved module references

---

## 📋 Checklist

- [ ] Add JWT parameters to main.bicep
- [ ] Add FRONTEND_ORIGIN parameter
- [ ] Call Key Vault module with enableKeyVault=true
- [ ] Call Redis module
- [ ] Call SQL Server module
- [ ] Update main.parameters.json with new parameters
- [ ] Create 6 service modules
- [ ] Create API Gateway module
- [ ] Complete SQL roles module
- [ ] Update container-app template with environment variables
- [ ] Validate all bicep files
- [ ] Test deployment validation
- [ ] Verify Key Vault secrets created
- [ ] Verify all services deploy successfully

---

**Generated:** October 27, 2025  
**Status:** Comprehensive audit complete - Ready for implementation

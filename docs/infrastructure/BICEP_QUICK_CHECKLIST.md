# ⚡ Quick Implementation Checklist

**Status:** Ready for implementation  
**Estimated Time:** 6-8 hours  
**Priority:** 🔴 CRITICAL - Blocks Azure deployment

---

## Phase 1: Core Infrastructure (2 hours)

### Step 1.1: Update `main.bicep` with Parameters
- [ ] Add JWT secret parameter (line after `cache_password`)
- [ ] Add JWT issuer parameter  
- [ ] Add JWT audience parameter
- [ ] Add FRONTEND_ORIGIN parameter
- [ ] Add ASPNETCORE_ENVIRONMENT parameter
- [ ] Add `resourceToken` variable

**File:** `infra/main.bicep`  
**Lines to add:** ~15 lines after existing parameters

### Step 1.2: Call Infrastructure Modules
- [ ] Add redis module call
- [ ] Add sqlServer module call  
- [ ] Add keyVault module call (with enableKeyVault: true)
- [ ] Set module dependencies with `dependsOn`

**File:** `infra/main.bicep`  
**Lines to add:** ~30 lines after resources module  
**Critical:** Set `enableKeyVault: true`

### Step 1.3: Update `main.parameters.json`
- [ ] Add jwtSecretKey parameter
- [ ] Add jwtIssuer parameter
- [ ] Add jwtAudience parameter
- [ ] Add frontendOrigin parameter
- [ ] Add aspnetcoreEnvironment parameter

**File:** `infra/main.parameters.json`  
**Lines to add:** ~20 lines

### Step 1.4: Test Phase 1
```powershell
az bicep build --file infra/main.bicep
az bicep build --file infra/main.bicep --outfile infra/main.json
```

---

## Phase 2: Database Configuration (1 hour)

### Step 2.1: Update `myapp-sqlserver.module.bicep`
- [ ] Add 6-database creation loop
- [ ] Add database outputs
- [ ] Update tags

**File:** `infra/myapp-sqlserver/myapp-sqlserver.module.bicep`  
**Lines to add:** ~25 lines  
**Critical:** Loop creates: AuthDB, BillingDB, InventoryDB, OrdersDB, PurchasingDB, SalesDB

### Step 2.2: Test Phase 2
```powershell
az bicep build --file infra/myapp-sqlserver/myapp-sqlserver.module.bicep
```

---

## Phase 3: Container App Template Updates (1 hour)

### Step 3.1: Update `core/host/container-app.bicep` Parameters
- [ ] Add frontendOrigin parameter
- [ ] Add jwtIssuer parameter
- [ ] Add jwtAudience parameter
- [ ] Add aspnetcoreEnvironment parameter
- [ ] Add appInsightsConnectionString parameter

**File:** `infra/core/host/container-app.bicep`  
**Lines to add:** ~20 lines (after line 28)

### Step 3.2: Update Container Configuration  
- [ ] Add env variable mapping for JWT config
- [ ] Add env variable for ASPNETCORE_ENVIRONMENT
- [ ] Add env variable for FRONTEND_ORIGIN
- [ ] Add env variable for Application Insights

**File:** `infra/core/host/container-app.bicep`  
**Lines to modify:** `template.containers[0].env` section

### Step 3.3: Test Phase 3
```powershell
az bicep build --file infra/core/host/container-app.bicep
```

---

## Phase 4: Service Modules (3-4 hours)

### Step 4.1: Create Auth Service Module
- [ ] Create file: `infra/auth-service/auth-service.module.bicep`
- [ ] Copy template from remediation guide
- [ ] Update service name references
- [ ] Update secret references (use sql-connection-authdb)
- [ ] Set daprEnabled: true, externalIngress: false

**File:** `infra/auth-service/auth-service.module.bicep` (**NEW**)

### Step 4.2: Create Billing Service Module
- [ ] Create file: `infra/billing-service/billing-service.module.bicep`
- [ ] Copy from auth-service template
- [ ] Change service name → `billing-service`
- [ ] Change secret name → `sql-connection-billingdb`

**File:** `infra/billing-service/billing-service.module.bicep` (**NEW**)

### Step 4.3: Create Inventory Service Module
- [ ] Create file: `infra/inventory-service/inventory-service.module.bicep`
- [ ] Change service name → `inventory-service`
- [ ] Change secret name → `sql-connection-inventorydb`

**File:** `infra/inventory-service/inventory-service.module.bicep` (**NEW**)

### Step 4.4: Create Orders Service Module
- [ ] Create file: `infra/orders-service/orders-service.module.bicep`
- [ ] Change service name → `orders-service`
- [ ] Change secret name → `sql-connection-ordersdb`

**File:** `infra/orders-service/orders-service.module.bicep` (**NEW**)

### Step 4.5: Create Purchasing Service Module
- [ ] Create file: `infra/purchasing-service/purchasing-service.module.bicep`
- [ ] Change service name → `purchasing-service`
- [ ] Change secret name → `sql-connection-purchasingdb`

**File:** `infra/purchasing-service/purchasing-service.module.bicep` (**NEW**)

### Step 4.6: Create Sales Service Module
- [ ] Create file: `infra/sales-service/sales-service.module.bicep`
- [ ] Change service name → `sales-service`
- [ ] Change secret name → `sql-connection-salesdb`

**File:** `infra/sales-service/sales-service.module.bicep` (**NEW**)

### Step 4.7: Create API Gateway Module
- [ ] Create file: `infra/api-gateway/api-gateway.module.bicep` (**NEW**)
- [ ] Copy from service template
- [ ] Set externalIngress: true (public facing)
- [ ] Set daprEnabled: false (gateway doesn't need Dapr)
- [ ] Add Ocelot route environment variables
- [ ] Set maxReplicas: 10 (higher for public endpoint)

**File:** `infra/api-gateway/api-gateway.module.bicep` (**NEW**)

### Step 4.8: Test Phase 4
```powershell
foreach ($service in @('auth', 'billing', 'inventory', 'orders', 'purchasing', 'sales')) {
  az bicep build --file "infra/$service-service/$service-service.module.bicep"
}
az bicep build --file infra/api-gateway/api-gateway.module.bicep
```

---

## Phase 5: Main.bicep Service Module Calls (30 min)

### Step 5.1: Add Auth Service Module Call
- [ ] Call auth-service module from main.bicep
- [ ] Pass parameters: containerAppsEnvironmentName, containerRegistryName, keyVaultUri
- [ ] Set dependencies: keyVault, redis, myapp_sqlserver

**File:** `infra/main.bicep`  
**Location:** After keyVault module definition

### Step 5.2: Add Billing Service Module Call
- [ ] Call billing-service module
- [ ] Repeat parameter passing pattern

### Step 5.3: Add Inventory Service Module Call
- [ ] Call inventory-service module

### Step 5.4: Add Orders Service Module Call
- [ ] Call orders-service module

### Step 5.5: Add Purchasing Service Module Call
- [ ] Call purchasing-service module

### Step 5.6: Add Sales Service Module Call
- [ ] Call sales-service module

### Step 5.7: Add API Gateway Module Call
- [ ] Call api-gateway module
- [ ] Set dependencies on all 6 service modules
- [ ] Ensure gateway deploys after services

**File:** `infra/main.bicep`  
**Location:** After all service modules  
**Critical:** API Gateway depends on services for service discovery

### Step 5.8: Test Phase 5
```powershell
az bicep build --file infra/main.bicep
az bicep build --file infra/main.bicep --outfile infra/main.json
```

---

## Phase 6: Validation & Debugging (30 min)

### Step 6.1: Validate All Bicep Files
```powershell
$files = @(
  'infra/main.bicep'
  'infra/resources.bicep'
  'infra/myapp-sqlserver/myapp-sqlserver.module.bicep'
  'infra/core/host/container-app.bicep'
  'infra/core/database/redis.bicep'
  'infra/core/database/sql-server.bicep'
  'infra/core/security/keyvault-secrets.bicep'
  'infra/auth-service/auth-service.module.bicep'
  'infra/billing-service/billing-service.module.bicep'
  'infra/inventory-service/inventory-service.module.bicep'
  'infra/orders-service/orders-service.module.bicep'
  'infra/purchasing-service/purchasing-service.module.bicep'
  'infra/sales-service/sales-service.module.bicep'
  'infra/api-gateway/api-gateway.module.bicep'
)

foreach ($file in $files) {
  Write-Host "Validating $file..."
  az bicep build --file $file
}
```

- [ ] All bicep validate passes
- [ ] No syntax errors
- [ ] No undefined variables
- [ ] No missing module references

### Step 6.2: Validate Parameters
```powershell
az deployment group validate `
  --template-file infra/main.json `
  --parameters infra/main.parameters.json
```

- [ ] Parameter types correct
- [ ] All required parameters provided
- [ ] No duplicate parameter names

### Step 6.3: Check Module References
- [ ] Each service module references correct container-app template
- [ ] Each service module references correct Key Vault secrets
- [ ] API Gateway references all services
- [ ] All module paths resolve correctly

### Step 6.4: Verify Environment Variables
- [ ] All 6 services have JWT parameters
- [ ] All 6 services have database connection reference
- [ ] All services have cache connection reference
- [ ] API Gateway has Ocelot routes configured
- [ ] All services have Dapr sidecar enabled (except gateway)

### Step 6.5: Test Phase 6
```powershell
# Run validation script
./validate-bicep-complete.ps1 -bicepPath ./infra
```

---

## Phase 7: Configuration Setup (30 min)

### Step 7.1: Create `.azure/myenv/.env` File
```bash
AZURE_ENV_NAME=myapp-dev
AZURE_LOCATION=eastus
AZURE_PRINCIPAL_ID=00000000-0000-0000-0000-000000000000  # Your AAD principal ID
AZURE_SQL_PASSWORD=ComplexP@ssw0rd123!
AZURE_REDIS_PASSWORD=ComplexRedisP@ssw0rd123!
AZURE_JWT_SECRET_KEY=your_very_long_secret_key_minimum_32_chars_exactly
AZURE_JWT_ISSUER=MyApp.Auth
AZURE_JWT_AUDIENCE=MyApp.All
AZURE_FRONTEND_ORIGIN=https://yourdomain.com
ASPNETCORE_ENVIRONMENT=Production
```

- [ ] Set strong passwords (20+ chars)
- [ ] Generate secure JWT secret
- [ ] Set correct principal ID
- [ ] Set correct frontend origin

### Step 7.2: Container Registry Setup
- [ ] Build and push auth-service image to ACR
- [ ] Build and push billing-service image to ACR
- [ ] Build and push inventory-service image to ACR
- [ ] Build and push orders-service image to ACR
- [ ] Build and push purchasing-service image to ACR
- [ ] Build and push sales-service image to ACR
- [ ] Build and push api-gateway image to ACR

**Example:**
```bash
az acr build --registry $ACR_NAME --image auth-service:latest -f MyApp.Auth/MyApp.Auth.API/Dockerfile .
```

---

## Phase 8: Deployment (1 hour)

### Step 8.1: Validate Deployment
```bash
azd env set AZURE_ENV_NAME myapp-prod
azd validate
```

- [ ] No validation errors
- [ ] All resources have correct dependencies
- [ ] All parameters have values

### Step 8.2: Deploy to Azure
```bash
azd deploy
```

- [ ] Deployment starts successfully
- [ ] Resource group created
- [ ] Container Apps deployed
- [ ] Key Vault secrets created
- [ ] Databases created

### Step 8.3: Verify Deployment
```bash
# Check Container Apps
az containerapp list --resource-group rg-myapp-prod

# Check Key Vault secrets
az keyvault secret list --vault-name <vault-name>

# Check SQL databases
az sql db list --server <server-name> --resource-group rg-myapp-prod

# Get API Gateway FQDN
az containerapp show --name api-gateway --resource-group rg-myapp-prod --query properties.configuration.ingress.fqdn
```

- [ ] 7 Container Apps created (6 services + gateway)
- [ ] 8 Key Vault secrets created
- [ ] 6 SQL databases created
- [ ] API Gateway has FQDN
- [ ] Services have internal FQDNs

---

## Critical Success Criteria

✅ **All Bicep files validate without errors**
```powershell
az bicep build --file infra/main.bicep
```

✅ **Parameter validation passes**
```powershell
az deployment group validate --template-file infra/main.json --parameters infra/main.parameters.json
```

✅ **All required modules exist and are called**
- 6 service modules
- 1 API gateway module
- 1 Key Vault module (with enableKeyVault: true)
- 1 Redis module
- 1 SQL Server module

✅ **Environment variables properly configured**
- JWT parameters in main.bicep
- FRONTEND_ORIGIN in main.bicep
- All parameters in main.parameters.json
- Service environment variables in container-app-bicep

✅ **Secrets properly referenced**
- JWT secret from Key Vault
- SQL connections from Key Vault
- Redis connection from Key Vault
- All services access secrets via secretRef

✅ **Key Vault integrated**
- Module called from main.bicep
- enableKeyVault parameter set to true
- Managed Identity has Get secret permission
- All 8 secrets created

---

## Troubleshooting Guide

### Issue: "Module not found" error
**Solution:** Check file paths are relative to `main.bicep`
```bicep
// Correct:
module redis 'core/database/redis.bicep' = { ... }

// Incorrect:
module redis './core/database/redis.bicep' = { ... }
module redis 'infra/core/database/redis.bicep' = { ... }
```

### Issue: "Parameter not defined" error
**Solution:** Ensure parameter is defined before module call
```bicep
// Must be at top of main.bicep with other params
@secure()
param jwtSecretKey string

// Then can be used in module call
params: {
  jwtSecretKey: jwtSecretKey
}
```

### Issue: "enableKeyVault: true doesn't work"
**Solution:** Check main.bicep module call
```bicep
// CORRECT:
module keyVault 'core/security/keyvault-secrets.bicep' = {
  params: {
    enableKeyVault: true  // ✅ Must be true
  }
}

// WRONG:
module keyVault 'core/security/keyvault-secrets.bicep' = {
  params: {
    enableKeyVault: false  // ❌ Secrets won't be created
  }
}
```

### Issue: "Module dependencies not working"
**Solution:** Use explicit dependsOn
```bicep
module authService 'auth-service/auth-service.module.bicep' = {
  name: 'auth-service'
  params: { ... }
  dependsOn: [
    keyVault      // ✅ Explicit dependency
    redis         // ✅ Explicit dependency
    myapp_sqlserver  // ✅ Explicit dependency
  ]
}
```

---

## Quick Reference Commands

```powershell
# Validate all bicep files
Get-ChildItem -Path ./infra -Filter "*.bicep" -Recurse | ForEach-Object {
  Write-Host "Validating $_"
  az bicep build --file $_.FullName
}

# Build main.bicep
az bicep build --file infra/main.bicep --outfile infra/main.json

# Validate parameters
az deployment group validate `
  --template-file infra/main.json `
  --parameters infra/main.parameters.json `
  --resource-group rg-myapp-dev

# Test deployment (no actual deployment)
azd validate

# Deploy to Azure
azd deploy

# Check deployment status
az deployment group show --name main --resource-group rg-myapp-prod

# View created resources
az resource list --resource-group rg-myapp-prod --output table
```

---

## Files to Create (Summary)

| File | Type | Status |
|------|------|--------|
| `infra/auth-service/auth-service.module.bicep` | NEW | 🔴 Priority |
| `infra/billing-service/billing-service.module.bicep` | NEW | 🔴 Priority |
| `infra/inventory-service/inventory-service.module.bicep` | NEW | 🔴 Priority |
| `infra/orders-service/orders-service.module.bicep` | NEW | 🔴 Priority |
| `infra/purchasing-service/purchasing-service.module.bicep` | NEW | 🔴 Priority |
| `infra/sales-service/sales-service.module.bicep` | NEW | 🔴 Priority |
| `infra/api-gateway/api-gateway.module.bicep` | NEW | 🔴 Priority |

## Files to Modify (Summary)

| File | Changes | Status |
|------|---------|--------|
| `infra/main.bicep` | Add parameters, module calls | 🔴 Priority |
| `infra/main.parameters.json` | Add parameter definitions | 🔴 Priority |
| `infra/myapp-sqlserver/myapp-sqlserver.module.bicep` | Add 6-database loop | 🔴 Priority |
| `infra/core/host/container-app.bicep` | Add env vars, JWT config | 🟠 High |

---

**Status:** Ready to execute  
**Estimated Time:** 6-8 hours  
**Success Rate:** High (detailed templates provided)  
**Blockers:** None identified - all information provided

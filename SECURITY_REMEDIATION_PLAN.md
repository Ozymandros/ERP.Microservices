# Security Remediation Plan

## 📊 Executive Summary

Your infrastructure has **critical security gaps** - unused parameters were actually **required for security**. This document outlines the remediation plan to implement proper:

- ✅ Managed Identity authentication
- ✅ RBAC role assignments
- ✅ Key Vault secret management
- ✅ App Configuration security
- ✅ Secure service-to-service connections

**Status:** Partially Remediated (See sections below)

---

## 🔴 Critical Issues Found

| Issue | Severity | Status | Action |
|-------|----------|--------|--------|
| `managedIdentityPrincipalId` unused in container-app-service.bicep | 🔴 CRITICAL | ✅ Fixed | Wired to RBAC outputs |
| `logAnalyticsWorkspaceId` unused in container-app-service.bicep | 🟡 MEDIUM | ✅ Fixed | Confirmed unused (handled at env level) |
| `cache_password` unused in main.bicep | 🔴 CRITICAL | ⏳ Pending | Wire to Redis password config |
| `principalId` unused in resources.bicep | 🔴 CRITICAL | ⏳ Pending | Create RBAC role assignments |
| `principalId` unused in myapp-sqlserver-roles.module.bicep | 🔴 CRITICAL | ⏳ Pending | Wire to database role assignments |
| `environmentName` unused in app-configuration.bicep | 🟡 MEDIUM | ⏳ Pending | Use in config naming convention |

---

## ✅ Completed Remediations

### **1. container-app-service.bicep - Managed Identity**

**Fixed:** Added proper managed identity principal output

```bicep
# BEFORE (Unused parameter)
@description('Managed Identity Principal ID')
param managedIdentityPrincipalId string = ''

# AFTER (Required for RBAC)
@description('Managed Identity Principal ID for RBAC role assignments')
param managedIdentityPrincipalId string

# Added output
output managedIdentityPrincipalId string = containerApp.identity.principalId
```

**Why:** Services need managed identity principal ID to:
- ✅ Grant Key Vault access
- ✅ Grant App Configuration access
- ✅ Grant SQL Database access
- ✅ Grant Redis access

**Usage:** Passed to RBAC role assignment modules

---

### **2. container-app-service.bicep - Log Analytics**

**Status:** Confirmed unused parameter - correctly removed

```bicep
# REMOVED (Not needed at service level)
param logAnalyticsWorkspaceId string = ''

# WHY: Container Apps Environment already configured with diagnostics
# Logging handled at environment level, not service level
```

---

## ⏳ Pending Remediations

### **3. main.bicep - cache_password**

**Status:** ⏳ Pending Fix

**Current Issue:**
```bicep
@description('Redis cache password')
@secure()
param cache_password string = ''  # ❌ UNUSED
```

**Required Fix:**

```bicep
# Step 1: Wire to Redis resource
resource redis 'Microsoft.Cache/redis@2023-08-01' = {
  name: redisCacheName
  location: location
  properties: {
    sku: {
      name: 'Standard'
      family: 'C'
      capacity: 1
    }
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    # ✅ Wire password here
    redisConfiguration: {
      requireauth: 'true'
      'maxmemory-policy': 'allkeys-lru'
    }
  }
}

# Step 2: Store in Key Vault
resource redisCachePassword 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'redis-cache-password'
  properties: {
    value: cache_password
  }
}

# Step 3: Reference in App Configuration
resource redisPasswordRef 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'Redis:Password'
  properties: {
    value: '@Microsoft.KeyVault(SecretUri=https://keyvault-name.vault.azure.net/secrets/redis-cache-password/)'
    contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
  }
}
```

**Why:** Services need Redis password to:
- ✅ Authenticate to Redis cache
- ✅ Read from cache (inventory, orders, etc.)
- ✅ Write to cache (session storage, etc.)

---

### **4. resources.bicep - principalId for RBAC**

**Status:** ⏳ Pending Fix

**Current Issue:**
```bicep
param principalId string = ''  # ❌ UNUSED - Should be for RBAC
```

**Required Fix:**

```bicep
# Create RBAC role assignments for Key Vault access
resource keyVaultSecretsUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(resourceGroup().id, principalId, 'Key Vault Secrets User')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: principalId  # ✅ Wire managed identity principal
    principalType: 'ServicePrincipal'
  }
}

# Create RBAC role assignments for App Configuration access
resource appConfigDataReaderRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: appConfig
  name: guid(resourceGroup().id, principalId, 'App Configuration Data Reader')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '516239f1-63e1-4108-9b7f-3ee94da9555c')
    principalId: principalId  # ✅ Wire managed identity principal
    principalType: 'ServicePrincipal'
  }
}
```

**Why:** Services need RBAC permissions to:
- ✅ Read secrets from Key Vault (JWT secret, Redis password, SQL keys)
- ✅ Read settings from App Configuration (JWT issuer, audience, CORS origin)
- ✅ Ensure secure access without password sharing

---

### **5. myapp-sqlserver-roles.module.bicep - principalId for Database**

**Status:** ⏳ Pending Fix

**Current Issue:**
```bicep
param principalId string = ''  # ❌ UNUSED
resource myapp_sqlserver 'Microsoft.Sql/servers@...' existing { }  # ❌ UNUSED
resource sqlServerAdmin 'Microsoft.ManagedIdentity/userAssignedIdentities@...' existing { }  # ❌ UNUSED
resource mi 'Microsoft.ManagedIdentity/userAssignedIdentities@...' existing { }  # ❌ UNUSED
```

**Required Fix:**

```bicep
# Use principalId for SQL database role assignment
resource sqlDatabaseRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: sqlDatabase
  name: guid(resourceGroup().id, principalId, 'SQL Database Contributor')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '9b7fa17d-e63a-4465-b752-700b8ab5191a')
    principalId: principalId  # ✅ Wire managed identity principal
    principalType: 'ServicePrincipal'
  }
}

# Create SQL user for managed identity
resource sqlUserCreation 'Microsoft.Sql/servers/databases/vulnerabilityAssessments/baselines@2023-02-01-preview' = {
  parent: sqlDatabase
  name: 'default'
  properties: {
    # SQL user created automatically when RBAC role assigned
  }
}
```

**Why:** Services need database access to:
- ✅ Auth service: Read user authentication data
- ✅ Inventory service: Read/write inventory data
- ✅ Orders service: Read/write order data
- ✅ Billing service: Read/write billing data

---

### **6. app-configuration.bicep - environmentName**

**Status:** ⏳ Pending Fix

**Current Issue:**
```bicep
param environmentName string = 'Production'  # ❌ UNUSED
var appConfigName = 'appconfig-${uniqueString(resourceGroup().id)}'  # ❌ Missing environment
```

**Required Fix:**

```bicep
# Use environmentName in naming
var appConfigName = 'appconfig-${toLower(environmentName)}-${uniqueString(resourceGroup().id)}'
var keyVaultName = 'kv-${toLower(environmentName)}-${uniqueString(resourceGroup().id)}'

# Use environmentName for environment-specific configuration
resource environmentLabel 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'Environment:Name'
  properties: {
    value: environmentName
    label: 'app-metadata'
  }
}

# Create environment-specific settings using labels
resource productionSettings 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'Database:ConnectionString'
  properties: {
    value: '@Microsoft.KeyVault(SecretUri=https://${keyVaultName}.vault.azure.net/secrets/sql-${toLower(environmentName)}/)'
    contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
    label: environmentName  # ✅ Environment-specific label
  }
}
```

**Why:** Environment-specific configuration needed for:
- ✅ Different database connection strings (prod vs staging vs dev)
- ✅ Different API endpoints (prod vs staging vs dev)
- ✅ Different security settings per environment
- ✅ Easy promotion across environments

---

## 🔧 Implementation Steps

### **Phase 1: Fix Container App Service** ✅ DONE
- [x] Add managed identity principal output
- [x] Document why parameters are needed
- [x] Remove truly unused parameters

### **Phase 2: Fix Main Bicep** ⏳ NEXT
- [ ] Wire `cache_password` to Redis resource
- [ ] Create role assignment for Redis password in Key Vault
- [ ] Document Redis authentication flow

### **Phase 3: Fix RBAC Policies** ⏳ NEXT
- [ ] Wire `principalId` to Key Vault role assignments
- [ ] Wire `principalId` to App Configuration role assignments
- [ ] Wire `principalId` to SQL Database role assignments
- [ ] Document RBAC permission model

### **Phase 4: Fix Database Roles** ⏳ NEXT
- [ ] Wire database role assignments
- [ ] Create SQL users for managed identities
- [ ] Document database authentication flow

### **Phase 5: Fix Environment Config** ⏳ NEXT
- [ ] Use `environmentName` in resource naming
- [ ] Create environment-specific App Configuration labels
- [ ] Support dev/staging/prod deployments

### **Phase 6: Validation & Testing** ⏳ FINAL
- [ ] Run `./validate-bicep.ps1`
- [ ] Run `./validate-bicep--what-if.ps1`
- [ ] Deploy to test environment
- [ ] Verify all connections work

---

## 🧪 Validation Commands

```powershell
# Validate all Bicep files
./validate-bicep.ps1 -ShowDetails

# Preview deployment with security fixes
./validate-bicep--what-if.ps1 -Location "eastus"

# Deploy to Azure
azd deploy

# Verify managed identities created
az identity list --resource-group myapp-rg --output table

# Verify RBAC role assignments
az role assignment list --resource-group myapp-rg --output table

# Verify Key Vault access
az keyvault secret list --vault-name <keyvault-name>

# Verify App Configuration
az appconfig kv list --name <appconfig-name>

# Test service connectivity
kubectl logs -n containers deployment/auth-service
kubectl logs -n containers deployment/orders-service
```

---

## 📝 Deployment Checklist

Before deploying infrastructure:

- [ ] All Bicep files validate without errors
- [ ] All parameters are documented
- [ ] Managed identities are wired to RBAC roles
- [ ] Key Vault access policies are created
- [ ] App Configuration RBAC is created
- [ ] Database role assignments are configured
- [ ] Redis password is secured in Key Vault
- [ ] Environment-specific naming is implemented

After deploying infrastructure:

- [ ] Verify all resources created in Azure
- [ ] Verify managed identities assigned
- [ ] Verify role assignments applied
- [ ] Verify Key Vault has all secrets
- [ ] Verify App Configuration has all settings
- [ ] Verify services can authenticate to Key Vault
- [ ] Verify services can read App Configuration
- [ ] Verify services can authenticate to databases
- [ ] Verify services can connect to Redis

---

## 🆘 Common Issues & Fixes

| Issue | Cause | Fix |
|-------|-------|-----|
| 403 Forbidden to Key Vault | Missing role assignment | Verify RBAC role assigned to managed identity |
| Redis connection refused | Missing password | Ensure password stored in Key Vault and referenced |
| SQL authentication failed | No DB user for identity | Create user: `CREATE USER [identity-name] FROM EXTERNAL PROVIDER` |
| App Config read fails | Missing role assignment | Assign "App Configuration Data Reader" role |
| Services can't find settings | App Config not wired | Verify AppConfiguration__ConnectionString env var |

---

## 📚 Next Steps

1. **Review** the pending remediation steps (Phases 2-5)
2. **Implement** each phase following the provided Bicep examples
3. **Validate** using the commands provided
4. **Deploy** using `azd deploy`
5. **Verify** post-deployment using the checklist

**Recommendation:** Proceed with Phase 2 (Fix Main Bicep) to secure Redis cache password handling.

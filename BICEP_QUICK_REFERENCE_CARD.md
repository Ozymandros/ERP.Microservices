# 🎯 Bicep Audit - Quick Reference Card

**Print this page or bookmark it during implementation**

---

## 📑 Document Quick Links

| Need | Document | Section |
|------|----------|---------|
| **Overview** | `BICEP_AUDIT_VISUAL_SUMMARY.md` | Dashboard (top) |
| **Executive Brief** | `BICEP_INFRASTRUCTURE_AUDIT_SUMMARY.md` | Key Findings |
| **Technical Deep-Dive** | `BICEP_COMPREHENSIVE_AUDIT.md` | File-by-File Analysis |
| **Implementation** | `BICEP_QUICK_CHECKLIST.md` | Phase 1 → Phase 8 |
| **Fix Code** | `BICEP_REMEDIATION_GUIDE.md` | Gaps #1-12 |
| **Config Mapping** | `BICEP_DOCKER_COMPOSE_MAPPING.md` | Global Configuration |
| **Index** | `BICEP_AUDIT_INDEX.md` | This Index |

---

## 🔴 Critical Gaps (Must Fix)

| # | Gap | File | Fix Time |
|---|-----|------|----------|
| 1 | JWT secret parameter | `main.bicep` | 5 min |
| 2 | Key Vault module call | `main.bicep` | 15 min |
| 3 | Redis module call | `main.bicep` | 10 min |
| 4 | SQL Server module call | `main.bicep` | 10 min |
| 5 | Database creation | `myapp-sqlserver.module.bicep` | 20 min |
| 6-11 | Service modules (6x) | New files | 90 min |
| 12 | API Gateway module | New file | 20 min |
| 13 | JWT in env vars | `container-app.bicep` | 15 min |

**Total: 6-8 hours**

---

## 🎯 Implementation Phases

### Phase 1: Core Infrastructure (2 hrs)
- [ ] Add JWT parameters to main.bicep
- [ ] Add resourceToken variable
- [ ] Call redis, sqlServer, keyVault modules
- [ ] Update main.parameters.json
- **Reference:** `BICEP_QUICK_CHECKLIST.md` → Phase 1

### Phase 2: Database Setup (1 hr)
- [ ] Add 6-database loop to SQL Server module
- **Reference:** `BICEP_REMEDIATION_GUIDE.md` → Gap #5

### Phase 3: Template Updates (1 hr)
- [ ] Add JWT+CORS parameters to container-app.bicep
- **Reference:** `BICEP_REMEDIATION_GUIDE.md` → Gap #8

### Phase 4: Service Modules (3-4 hrs)
- [ ] Create 6 service modules
- [ ] Create API Gateway module
- **Reference:** `BICEP_REMEDIATION_GUIDE.md` → Gaps #9-12

### Phase 5: Integration (30 min)
- [ ] Add all 7 module calls to main.bicep
- **Reference:** `BICEP_QUICK_CHECKLIST.md` → Phase 5

### Phase 6: Validation (30 min)
- [ ] Validate all bicep files
- [ ] Check parameters
- **Reference:** `BICEP_REMEDIATION_GUIDE.md` → Validation Script

### Phase 7: Configuration (30 min)
- [ ] Set up .azure/myenv/.env
- **Reference:** `BICEP_QUICK_CHECKLIST.md` → Phase 7

### Phase 8: Deployment (1 hr)
- [ ] Push images to ACR
- [ ] Run azd deploy
- **Reference:** `BICEP_QUICK_CHECKLIST.md` → Phase 8

---

## 💻 Essential Commands

```powershell
# Validate single file
az bicep validate --file infra/main.bicep

# Build to ARM template
az bicep build --file infra/main.bicep --outfile infra/main.json

# Validate parameters
az deployment group validate `
  --template-file infra/main.json `
  --parameters infra/main.parameters.json

# Validate entire deployment
azd validate

# Deploy to Azure
azd deploy

# List deployed services
az containerapp list --resource-group rg-myapp-prod

# Check Key Vault secrets
az keyvault secret list --vault-name <vault-name>

# Get API Gateway FQDN
az containerapp show --name api-gateway --resource-group rg-myapp-prod `
  --query properties.configuration.ingress.fqdn
```

---

## 📋 Files to Create (7 New Files)

```
✨ NEW FILES TO CREATE:

infra/auth-service/auth-service.module.bicep
infra/billing-service/billing-service.module.bicep
infra/inventory-service/inventory-service.module.bicep
infra/orders-service/orders-service.module.bicep
infra/purchasing-service/purchasing-service.module.bicep
infra/sales-service/sales-service.module.bicep
infra/api-gateway/api-gateway.module.bicep
```

**Get templates from:** `BICEP_REMEDIATION_GUIDE.md` → Gaps #9-12

---

## ✏️ Files to Modify (5 Files)

```
📝 FILES TO MODIFY:

infra/main.bicep
  - Add JWT parameters
  - Add module calls
  - Add service module calls

infra/main.parameters.json
  - Add JWT parameters
  - Add CORS parameter

infra/myapp-sqlserver/myapp-sqlserver.module.bicep
  - Add 6-database loop

infra/core/host/container-app.bicep
  - Add JWT+CORS parameters
  - Add env variable mapping

infra/myapp-sqlserver-roles/myapp-sqlserver-roles.module.bicep
  - Add role assignments
```

**Get exact changes from:** `BICEP_REMEDIATION_GUIDE.md` → Gaps #1-8

---

## 🔑 Key Parameters to Add

```bicep
// Add to main.bicep:

@secure()
param jwtSecretKey string

param jwtIssuer string = 'MyApp.Auth'

param jwtAudience string = 'MyApp.All'

param frontendOrigin string = 'http://localhost:3000;http://localhost:5000'

param aspnetcoreEnvironment string = 'Production'

var resourceToken = uniqueString(subscription().id, resourceGroup().id)
```

---

## 🔐 Key Vault Setup

```bicep
// Add to main.bicep:

module keyVault 'core/security/keyvault-secrets.bicep' = {
  name: 'keyvault'
  scope: rg
  params: {
    name: 'kv-${resourceToken}'
    location: location
    tags: tags
    jwtSecretKey: jwtSecretKey  // ← CRITICAL
    redisHostName: redis.outputs.hostName
    redisPrimaryKey: redis.outputs.primaryKey
    sqlFqdn: myapp_sqlserver.outputs.sqlServerFqdn
    sqlAdminPassword: password
    enableKeyVault: true  // ← MUST BE TRUE
  }
  dependsOn: [
    redis
    myapp_sqlserver
  ]
}
```

**Critical:** `enableKeyVault: true` - if false, secrets won't be created!

---

## 🎁 Module Call Template

```bicep
// Use this pattern for all 6 services + gateway:

module authService 'auth-service/auth-service.module.bicep' = {
  name: 'auth-service'
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
```

---

## ✅ Success Checklist

**Before deploying:**

- [ ] All bicep validate passes
- [ ] All 7 service modules created
- [ ] All module calls in main.bicep
- [ ] JWT parameters in main.bicep
- [ ] Key Vault module called (enableKeyVault: true)
- [ ] Redis module called
- [ ] SQL Server module called
- [ ] 6-database loop in myapp-sqlserver.module.bicep
- [ ] main.parameters.json updated
- [ ] container-app.bicep updated with env vars
- [ ] No hardcoded secrets found
- [ ] azd validate passes
- [ ] Deployment validates without errors

---

## 🐛 Troubleshooting Quick Fixes

### Error: "Module not found"
```bicep
// ❌ WRONG:
module redis './core/database/redis.bicep' = { ... }

// ✅ RIGHT:
module redis 'core/database/redis.bicep' = { ... }
```

### Error: "Parameter not defined"
```bicep
// Add parameter at top of main.bicep
@secure()
param jwtSecretKey string

// Then use in module
params: {
  jwtSecretKey: jwtSecretKey
}
```

### Error: "enableKeyVault is not recognized"
```bicep
// Check parameter exists in keyvault-secrets.bicep:
param enableKeyVault bool = false

// Set to true in main.bicep call:
enableKeyVault: true  // ← CRITICAL
```

### Error: "Missing dependency"
```bicep
// Add explicit dependsOn:
dependsOn: [
  keyVault
  redis
  myapp_sqlserver
]
```

---

## 📊 Verification Checklist

**After each phase:**

Phase 1 ✅
```powershell
az bicep validate --file infra/main.bicep
```

Phase 4 ✅
```powershell
foreach ($service in @('auth', 'billing', 'inventory', 'orders', 'purchasing', 'sales')) {
  az bicep validate --file "infra/$service-service/$service-service.module.bicep"
}
```

Phase 6 ✅
```powershell
az bicep build --file infra/main.bicep --outfile infra/main.json
az deployment group validate --template-file infra/main.json --parameters infra/main.parameters.json
```

Phase 8 ✅
```bash
azd deploy
```

---

## 🎯 Time Breakdown

```
Phase 1: Core Infrastructure ......... 2 hours
Phase 2: Database Setup .............. 1 hour
Phase 3: Template Updates ............ 1 hour
Phase 4: Service Modules ............ 3-4 hours
Phase 5: Integration ................. 30 min
Phase 6: Validation .................. 30 min
Phase 7: Configuration ............... 30 min
Phase 8: Deployment .................. 1 hour
─────────────────────────────────────────────
TOTAL ................................. 6-8 HOURS
```

---

## 🚀 Quick Start

```
1. Read this card (5 min)
2. Open BICEP_QUICK_CHECKLIST.md
3. Open BICEP_REMEDIATION_GUIDE.md
4. Start Phase 1
5. Follow checklist
6. Reference guide as needed
7. Validate each phase
8. Deploy ✅
```

---

## 🔗 Document Links

- `BICEP_AUDIT_VISUAL_SUMMARY.md` - Visual overview
- `BICEP_INFRASTRUCTURE_AUDIT_SUMMARY.md` - Executive summary
- `BICEP_COMPREHENSIVE_AUDIT.md` - Technical deep-dive
- `BICEP_QUICK_CHECKLIST.md` - Implementation guide ⭐
- `BICEP_REMEDIATION_GUIDE.md` - Code fixes ⭐
- `BICEP_DOCKER_COMPOSE_MAPPING.md` - Configuration reference
- `BICEP_AUDIT_INDEX.md` - Document index

---

## 📞 When Stuck

| Problem | Solution |
|---------|----------|
| Don't understand a gap | → Comprehensive Audit |
| Need exact code to fix | → Remediation Guide |
| Need to verify progress | → Quick Checklist |
| Validation errors | → Troubleshooting section above |
| General questions | → Executive Summary |

---

## ✨ Key Takeaways

✅ **All gaps identified and documented**  
✅ **All solutions provided with code**  
✅ **All validation steps included**  
✅ **High confidence implementation**  
✅ **6-8 hour timeline realistic**

🚀 **Ready to execute!**

---

**Bookmark this for quick reference during implementation**

*Last Updated: October 27, 2025*

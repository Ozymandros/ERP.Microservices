# RBAC Role IDs: Refactored from Hardcoded to Dynamic

## Summary of Changes

This refactoring eliminates all hardcoded Azure role definition IDs from the infrastructure code. Instead, role IDs are now fetched dynamically at deployment time from the Azure subscription, ensuring compatibility across environments and preventing deployment failures due to incorrect role definitions.

## What Changed

### Problem: Hardcoded Role IDs ❌

Previously, role definition IDs were embedded directly in Bicep templates:

```bicep
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86d0e6e'  // ❌ Hardcoded
var acrPullRoleDefinitionId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'   // ❌ Hardcoded
```

**Issues with hardcoding:**
- Role IDs might change or vary by region
- No validation that the role exists
- Deployment breaks silently if ID is wrong
- Difficult to maintain across multiple environments
- No single source of truth

### Solution: Dynamic Parameters ✅

Role IDs are now passed as deployment parameters, fetched from the actual Azure subscription:

```bicep
@description('Key Vault Secrets User role definition ID')
param keyVaultSecretsUserRoleDefinitionId string

@description('AcrPull role definition ID')
param acrPullRoleDefinitionId string
```

**Benefits:**
- Role IDs are fetched from live Azure subscription
- Validates role exists before deployment
- Works across different Azure environments
- Clear deployment-time validation
- Single source of truth in Azure RBAC

## Files Changed

### 1. **main.bicep** - Main Orchestration Template
- **Added:** 4 new parameters for role definition IDs:
  - `keyVaultSecretsUserRoleDefinitionId`
  - `acrPullRoleDefinitionId`
  - `appConfigurationDataReaderRoleDefinitionId`
  - `sqlDbContributorRoleDefinitionId`
- **Updated:** Module calls to pass role IDs to child modules

### 2. **core/security/keyvault-rbac.bicep** - Key Vault RBAC Module
- **Removed:** Hardcoded `keyVaultSecretsUserRoleId` variable
- **Removed:** Import from constants.bicep
- **Added:** `roleDefinitionId` parameter
- **Updated:** Role assignment to use parameter instead of hardcoded value

### 3. **resources.bicep** - Core Resources
- **Removed:** Import of `acrPullRoleDefinitionId` from constants.bicep
- **Added:** `acrPullRoleDefinitionId` parameter
- **Updated:** Role assignment to use parameter instead of hardcoded value

### 4. **config/constants.bicep** - Configuration Constants
- **Removed:** Hardcoded role definition IDs:
  - ~~`var acrPullRoleDefinitionId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'`~~
  - ~~`var keyVaultSecretsUserRoleDefinitionId = '4633458b-17de-408a-b874-0445c86d0e6e'`~~
- **Retained:** Non-ID configuration (SKUs, names, etc.)

### 5. **core/security/role-definitions.bicep** (NEW)
- **Purpose:** Documents Azure built-in roles and their purposes
- **Contains:** Role names, descriptions, and references to Azure documentation
- **Includes:** Migration path and best practices

### 6. **scripts/get-role-definitions.ps1** (NEW)
- **Purpose:** PowerShell utility to fetch role IDs dynamically
- **Supports:** Both Azure CLI and Azure PowerShell
- **Usage:** Deployed as part of deployment process

### 7. **DEPLOYMENT_ROLES.md** (NEW)
- **Purpose:** Complete guide for deploying with dynamic role IDs
- **Includes:** Step-by-step instructions for fetching and deploying roles
- **Provides:** Troubleshooting guide and best practices

## Migration Path

### For Development/Testing

#### Option 1: Using Azure CLI (Quickest)

```bash
# Fetch role IDs
KEYVAULT_ROLE=$(az role definition list --query "[?roleName=='Key Vault Secrets User'].id" -o tsv)
ACR_ROLE=$(az role definition list --query "[?roleName=='AcrPull'].id" -o tsv)
APPCONFIG_ROLE=$(az role definition list --query "[?roleName=='App Configuration Data Reader'].id" -o tsv)
SQLDB_ROLE=$(az role definition list --query "[?roleName=='SQL DB Contributor'].id" -o tsv)

# Deploy
azd deploy \
  --parameter keyVaultSecretsUserRoleDefinitionId=$KEYVAULT_ROLE \
  --parameter acrPullRoleDefinitionId=$ACR_ROLE \
  --parameter appConfigurationDataReaderRoleDefinitionId=$APPCONFIG_ROLE \
  --parameter sqlDbContributorRoleDefinitionId=$SQLDB_ROLE
```

#### Option 2: Using Azure PowerShell

```powershell
$roles = @{
    keyVaultSecretsUserRoleDefinitionId = (Get-AzRoleDefinition -Name 'Key Vault Secrets User').Id
    acrPullRoleDefinitionId = (Get-AzRoleDefinition -Name 'AcrPull').Id
    appConfigurationDataReaderRoleDefinitionId = (Get-AzRoleDefinition -Name 'App Configuration Data Reader').Id
    sqlDbContributorRoleDefinitionId = (Get-AzRoleDefinition -Name 'SQL DB Contributor').Id
}

# Pass to azd
foreach ($role in $roles.GetEnumerator()) {
    azd env set $role.Key $role.Value
}

azd deploy
```

#### Option 3: Using the Provided Script

```powershell
cd infra/scripts
& ./get-role-definitions.ps1 -RoleNames @(
    'Key Vault Secrets User',
    'AcrPull',
    'App Configuration Data Reader',
    'SQL DB Contributor'
) -UseCommand cli
```

### For Production Deployment

1. **Before deployment:** Run the role lookup script in your CI/CD pipeline
2. **Store results:** Save role IDs in secure parameter store (Azure Key Vault, etc.)
3. **At deployment:** Fetch from parameter store and pass to `azd deploy`
4. **Validation:** Log fetched role IDs for audit trail

Example GitHub Actions workflow:

```yaml
- name: Fetch role definitions
  id: roles
  run: |
    KEYVAULT_ROLE=$(az role definition list --query "[?roleName=='Key Vault Secrets User'].id" -o tsv)
    echo "keyvault-role=$KEYVAULT_ROLE" >> $GITHUB_OUTPUT
    # ... repeat for other roles

- name: Deploy Infrastructure
  run: |
    azd deploy \
      --parameter keyVaultSecretsUserRoleDefinitionId=${{ steps.roles.outputs.keyvault-role }} \
      # ... other parameters
```

## Technical Details

### Role Definition IDs Reference

These are the **typical** Azure built-in role IDs (may vary by cloud/region):

| Role | Typical ID | Purpose |
|------|-----------|---------|
| Key Vault Secrets User | `4633458b-17de-408a-b874-0445c86d0e6e` | Read Key Vault secrets |
| AcrPull | `7f951dda-4ed3-4680-a7ca-43fe172d538d` | Pull container images |
| App Configuration Data Reader | `516239f1-63e1-4108-9233-9e7f68e97ce3` | Read App Configuration |
| SQL DB Contributor | `9b7fa17d-e63e-47b0-bb0a-15c516ac86ec` | Manage SQL databases |

**⚠️ Important:** Always fetch from your actual subscription - don't use these as defaults!

### Module Dependencies

```
main.bicep (accepts 4 role ID params)
├── resources.bicep
│   ├── Accepts: acrPullRoleDefinitionId
│   └── Creates: ACR role assignment
├── core/security/keyvault-rbac.bicep
│   ├── Accepts: keyVaultSecretsUserRoleDefinitionId
│   └── Creates: Key Vault role assignment
└── Other modules (unaffected)
```

## Validation & Testing

### Pre-Deployment Validation

```bash
# Validate that all roles exist
az role definition list --query "[?roleName==('Key Vault Secrets User'||'AcrPull'||'App Configuration Data Reader'||'SQL DB Contributor')].{name:roleName, id:id}"
```

### Post-Deployment Validation

```bash
# Verify role assignments were created
az role assignment list --resource-group "<your-rg>" --query "[].{principal:principalName, role:roleDefinitionName}"
```

## Breaking Changes

⚠️ **Breaking Change:** Existing deployments using hardcoded role IDs will fail

**Fix:** Pass role definition IDs as parameters during deployment

## Rollback Plan

If issues occur:

1. Revert changes to `main.bicep`, `resources.bicep`, and `keyvault-rbac.bicep`
2. Restore hardcoded role IDs in `config/constants.bicep`
3. Re-import from constants in modules
4. Redeploy

## Next Steps

1. **Test in dev:** Deploy with dynamic role IDs in development environment
2. **Validate RBAC:** Confirm all role assignments created successfully
3. **Test functionality:** Verify microservices can access Key Vault and ACR
4. **Update CI/CD:** Add role ID fetching to deployment pipelines
5. **Document:** Update team runbooks with new deployment process

## Questions?

Refer to:
- `DEPLOYMENT_ROLES.md` - Deployment instructions
- `core/security/role-definitions.bicep` - Role documentation
- Azure RBAC documentation: https://learn.microsoft.com/en-us/azure/role-based-access-control/

# Azure RBAC Best Practices: Why Dynamic Role IDs Matter

## The Problem with Hardcoding Role IDs

### ❌ Anti-Pattern: Hardcoded IDs

```bicep
// BAD: Role IDs hardcoded directly in templates
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86d0e6e'
var acrPullRoleDefinitionId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86d0e6e')
  }
}
```

**Why this is problematic:**

1. **No Validation** - Nothing stops you from using a wrong ID
2. **Environment Variation** - Role IDs might differ across regions or subscriptions
3. **Maintenance Nightmare** - Changing a role ID requires code changes and testing
4. **Audit Trail Missing** - No record of where the role ID came from
5. **Copy-Paste Errors** - Easy to accidentally use wrong ID (we found one!)
6. **Scope Creep** - Difficult to add new roles without code changes

### Example of the Problem We Fixed

We discovered a typo in `constants.bicep`:
```bicep
// Line 90 had this (WRONG):
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b8b7-0445c86d0e6e'  // ❌ b8b7

// But keyvault-rbac.bicep had the correct value:
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86d0e6e'  // ✅ b874
```

This single-character typo caused the entire deployment to fail with:
```
RoleDefinitionDoesNotExist: The specified role definition with ID 
'4633458b17de408ab8740445c86d0e6e' does not exist
```

---

## ✅ Solution: Dynamic Role ID Lookup

### Pattern 1: Deployment-Time Parameter Passing

```bicep
// ✅ GOOD: Role IDs passed as parameters

@description('Key Vault Secrets User role definition ID')
param keyVaultSecretsUserRoleDefinitionId string

@description('AcrPull role definition ID')
param acrPullRoleDefinitionId string

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleDefinitionId)
  }
}
```

**Benefits:**
- Role IDs fetched from live Azure subscription before deployment
- No possibility of typos - validation happens at fetch time
- Single source of truth: the Azure RBAC service
- Clear parameter documentation
- Easy to test different role IDs

### Pattern 2: Fetch Role IDs in Deployment Script

```bash
# Step 1: Fetch role IDs from subscription
KEYVAULT_ROLE=$(az role definition list --query "[?roleName=='Key Vault Secrets User'].id" -o tsv)
ACR_ROLE=$(az role definition list --query "[?roleName=='AcrPull'].id" -o tsv)

# Step 2: Pass to deployment
azd deploy \
  --parameter keyVaultSecretsUserRoleDefinitionId=$KEYVAULT_ROLE \
  --parameter acrPullRoleDefinitionId=$ACR_ROLE
```

**Benefits:**
- Role fetched by name, not ID
- Role names are stable and well-documented
- If role doesn't exist, fetch fails loudly before deployment
- Audit trail: you can see which role was fetched

### Pattern 3: Runtime Role Resolution (Advanced)

For maximum flexibility, use Azure REST API or CLI in deployment orchestrator:

```powershell
# PowerShell approach
function Get-RoleDefinitionId {
    param([string]$RoleName, [string]$SubscriptionId)
    
    az account set --subscription $SubscriptionId
    $roleId = az role definition list --query "[?roleName=='$RoleName'].id" -o tsv
    
    if (-not $roleId) {
        throw "Role '$RoleName' not found in subscription '$SubscriptionId'"
    }
    
    return $roleId
}

# Usage
$keyVaultRoleId = Get-RoleDefinitionId -RoleName 'Key Vault Secrets User' -SubscriptionId $subscriptionId
```

---

## Implementation Comparison

| Aspect | Hardcoded | Parameter | Dynamic Lookup |
|--------|-----------|-----------|-----------------|
| **Validation** | ❌ None | ✅ Type checking | ✅✅ Runtime validation |
| **Flexibility** | ❌ Requires code change | ✅ Parameter file | ✅✅ Script + lookup |
| **Audit Trail** | ❌ None | ⚠️ Deploy logs | ✅ Full trace |
| **Maintenance** | ❌ High | ✅ Medium | ✅✅ Low |
| **Error Detection** | ❌ Runtime failure | ⚠️ Compile-time | ✅ Pre-deployment |
| **Environment Support** | ⚠️ Manual updates | ✅ Per-environment | ✅✅ Automatic |
| **Complexity** | ✅ Simple | ✅ Medium | ⚠️ Higher |

---

## Microsoft Recommendations

From [Azure RBAC Best Practices](https://learn.microsoft.com/en-us/azure/role-based-access-control/best-practices):

> "Use role names instead of role IDs when possible. Role IDs may change, but role names are stable."

From [Bicep Best Practices](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/best-practices):

> "Avoid hardcoding values. Use parameters, variables, and outputs for flexibility."

---

## Deployment Scenarios

### Scenario 1: Development/Testing
```bash
# Quick deployment with parameter passing
azd deploy \
  --parameter keyVaultSecretsUserRoleDefinitionId=$(az role definition list --query "[?roleName=='Key Vault Secrets User'].id" -o tsv)
```

### Scenario 2: CI/CD Pipeline
```yaml
- name: Fetch role definitions
  run: |
    echo "KEYVAULT_ROLE=$(az role definition list --query "[?roleName=='Key Vault Secrets User'].id" -o tsv)" >> $GITHUB_ENV

- name: Deploy
  run: azd deploy --parameter keyVaultSecretsUserRoleDefinitionId=${{ env.KEYVAULT_ROLE }}
```

### Scenario 3: Multi-Environment
```bash
#!/bin/bash
# Deploy to multiple environments with role validation

for ENV in dev staging prod; do
    echo "Deploying to $ENV..."
    
    # Fetch roles for this environment's subscription
    SUBSCRIPTION=$(get-subscription-for-env $ENV)
    KEYVAULT_ROLE=$(az role definition list --subscription $SUBSCRIPTION --query "[?roleName=='Key Vault Secrets User'].id" -o tsv)
    
    # Deploy
    azd deploy --environment $ENV --parameter keyVaultSecretsUserRoleDefinitionId=$KEYVAULT_ROLE
done
```

---

## Validation Checklist

- [ ] Role IDs are NOT hardcoded in Bicep templates
- [ ] Role IDs are accepted as parameters in modules
- [ ] Main template accepts role IDs as parameters
- [ ] Deployment script fetches role IDs by name before deployment
- [ ] Role names are used in documentation (e.g., "Key Vault Secrets User")
- [ ] Parameter descriptions explain what each role does
- [ ] Deployment instructions include role ID fetching steps
- [ ] Error messages guide users if role not found
- [ ] Audit trail shows which role IDs were deployed

---

## Common Mistakes to Avoid

### ❌ Mistake 1: Hardcoding in a "constants" file

```bicep
// ❌ Still hardcoded, just in a different file
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86d0e6e'

import { keyVaultSecretsUserRoleId } from './constants.bicep'
```

**Fix:** Pass role IDs as parameters, not constants

### ❌ Mistake 2: Assuming role IDs are stable

```bicep
// ❌ Role IDs might be different in different Azure environments
// This ID works in one subscription but might not in another
var acrPullRoleId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'
```

**Fix:** Fetch role IDs from the subscription being deployed to

### ❌ Mistake 3: No validation of role existence

```bash
# ❌ If role doesn't exist, deployment fails mysteriously
az role definition list --query "[?roleName=='NonExistentRole'].id" -o tsv | \
  xargs azd deploy --parameter someRole=
```

**Fix:** Validate role exists before deployment:

```bash
ROLE_ID=$(az role definition list --query "[?roleName=='KeyVaultSecretsUser'].id" -o tsv)
if [ -z "$ROLE_ID" ]; then
    echo "ERROR: Role 'Key Vault Secrets User' not found"
    exit 1
fi
```

### ❌ Mistake 4: Same role IDs across all roles

```bicep
// ❌ Copy-paste error - same ID for different roles
var role1 = '7f951dda-4ed3-4680-a7ca-43fe172d538d'
var role2 = '7f951dda-4ed3-4680-a7ca-43fe172d538d'  // Same ID!
```

**Fix:** Always fetch by role name, eliminating copy-paste errors

---

## Migration from Hardcoded to Dynamic

### Step 1: Identify hardcoded role IDs
```bash
grep -r "var.*RoleId\|var.*roleId" infra/
grep -r "[0-9a-f]{8}-[0-9a-f]{4}" infra/*.bicep
```

### Step 2: Create parameters in main template
```bicep
param keyVaultSecretsUserRoleDefinitionId string
param acrPullRoleDefinitionId string
```

### Step 3: Update child modules to accept parameters
```bicep
param roleDefinitionId string
```

### Step 4: Update module calls
```bicep
module kv 'keyvault.bicep' = {
  params: {
    roleDefinitionId: keyVaultSecretsUserRoleDefinitionId
  }
}
```

### Step 5: Update deployment scripts
```bash
ROLE_ID=$(az role definition list --query "[?roleName=='RoleName'].id" -o tsv)
azd deploy --parameter roleId=$ROLE_ID
```

---

## References

- [Azure Built-in Roles](https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles)
- [Azure RBAC Best Practices](https://learn.microsoft.com/en-us/azure/role-based-access-control/best-practices)
- [Bicep Best Practices](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/best-practices)
- [Azure PowerShell: Get-AzRoleDefinition](https://learn.microsoft.com/en-us/powershell/module/az.resources/get-azroledefinition)
- [Azure CLI: az role definition list](https://learn.microsoft.com/en-us/cli/azure/role/definition#az-role-definition-list)

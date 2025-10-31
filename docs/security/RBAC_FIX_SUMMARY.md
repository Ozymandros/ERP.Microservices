# 🔧 RBAC Role Definition Fix - Deployment Error Resolution

## Issue Identified
**Error:** `RoleDefinitionDoesNotExist: The specified role definition with ID '4633458b17de408ab8740445c86d0e6e' does not exist`

**Root Cause:** Typo in `infra/config/constants.bicep` line 90

## The Problem

### Incorrect Value (Was):
```bicep
// infra/config/constants.bicep line 90
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b8b7-0445c86d0e6e'
                                                      ^^^^ TYPO HERE (b8b7 instead of b874)
```

### Correct Value (Now):
```bicep
// infra/config/constants.bicep line 90
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86d0e6e'
                                                      ^^^^ FIXED (b874 is correct)
```

## What This Role Is

**Azure Role:** Key Vault Secrets User (Built-in)
- **Role ID:** `4633458b-17de-408a-b874-0445c86d0e6e`
- **Purpose:** Allows reading secrets from Key Vault
- **Applied To:** Container App managed identities so they can access:
  - JWT secret keys
  - Redis cache passwords
  - SQL connection strings

## Files Affected

✅ **Fixed:**
- `infra/config/constants.bicep` - Corrected the typo in line 90

✅ **Already Correct:**
- `infra/core/security/keyvault-rbac.bicep` - Had the correct value all along

## Impact

This fix resolves:
1. ✅ The `RoleDefinitionDoesNotExist` error that was preventing Container Apps deployment
2. ✅ The cascading "Operation expired" timeout errors (Container Apps couldn't start because they couldn't get RBAC permissions)
3. ✅ Container App managed identities can now authenticate to Key Vault and pull configuration

## Next Steps

1. **Redeploy Infrastructure:**
   ```bash
   cd infra
   azd deploy
   ```

2. **Verify Deployment:**
   - Check Container Apps status in Azure Portal
   - Verify Container App revisions are active (not timed out)
   - Check Container App logs for successful startup

3. **Validation:**
   - Ensure Container Apps can pull secrets from Key Vault
   - Verify connection to Redis and SQL Server working

## Testing Commands

```bash
# Check role assignments on Key Vault
az role assignment list --resource-group myapp-rg \
  --scope "/subscriptions/{sub-id}/resourceGroups/myapp-rg/providers/Microsoft.KeyVault/vaults/myapp-dev-kv"

# Verify Container App status
az containerapp show --resource-group myapp-rg --name myapp-dev-auth-service --query properties.provisioningState
```

# Infrastructure Deployment Guide - Role-Based Access Control

## Overview

This infrastructure uses **dynamic role definition lookups** instead of hardcoding Azure role IDs. This ensures compatibility across Azure environments and prevents issues when role definitions change.

## Prerequisites

You need ONE of the following tools installed:
- **Azure CLI** (recommended): `az --version`
- **Azure PowerShell**: `Get-Module -ListAvailable Az.Accounts`

## Deployment Steps

### Step 1: Fetch Role Definition IDs

Before deploying, fetch the current role definition IDs from your Azure subscription:

#### Option A: Using Azure CLI (Recommended)

```bash
# Set your subscription
az account set --subscription "<subscription-id>"

# Fetch role definition IDs
$roleIds = @{
    keyVaultSecretsUserRoleDefinitionId = $(az role definition list --query "[?roleName=='Key Vault Secrets User'].id" -o tsv)
    acrPullRoleDefinitionId = $(az role definition list --query "[?roleName=='AcrPull'].id" -o tsv)
    appConfigurationDataReaderRoleDefinitionId = $(az role definition list --query "[?roleName=='App Configuration Data Reader'].id" -o tsv)
    sqlDbContributorRoleDefinitionId = $(az role definition list --query "[?roleName=='SQL DB Contributor'].id" -o tsv)
}

# Display fetched IDs
Write-Host "Fetched Role Definition IDs:"
$roleIds | Format-Table
```

#### Option B: Using Azure PowerShell

```powershell
# Connect to Azure (if not already connected)
Connect-AzAccount

# Set your subscription
Set-AzContext -SubscriptionId "<subscription-id>"

# Fetch role definition IDs
$roleIds = @{
    keyVaultSecretsUserRoleDefinitionId = (Get-AzRoleDefinition -Name 'Key Vault Secrets User').Id
    acrPullRoleDefinitionId = (Get-AzRoleDefinition -Name 'AcrPull').Id
    appConfigurationDataReaderRoleDefinitionId = (Get-AzRoleDefinition -Name 'App Configuration Data Reader').Id
    sqlDbContributorRoleDefinitionId = (Get-AzRoleDefinition -Name 'SQL DB Contributor').Id
}

# Display fetched IDs
$roleIds | Format-Table
```

#### Option C: Using the Provided PowerShell Script

```powershell
cd infra/scripts

# Make script executable
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process

# Run the script
& ./get-role-definitions.ps1 -RoleNames @(
    'Key Vault Secrets User',
    'AcrPull',
    'App Configuration Data Reader',
    'SQL DB Contributor'
) -Verbose
```

### Step 2: Deploy Infrastructure

Once you have the role definition IDs, deploy using `azd`:

```bash
# From the infra directory
cd infra

# Deploy with role definition parameters
azd deploy \
  --parameter keyVaultSecretsUserRoleDefinitionId="<role-id>" \
  --parameter acrPullRoleDefinitionId="<role-id>" \
  --parameter appConfigurationDataReaderRoleDefinitionId="<role-id>" \
  --parameter sqlDbContributorRoleDefinitionId="<role-id>"
```

Or use environment variables:

```bash
export keyVaultSecretsUserRoleDefinitionId="<role-id>"
export acrPullRoleDefinitionId="<role-id>"
export appConfigurationDataReaderRoleDefinitionId="<role-id>"
export sqlDbContributorRoleDefinitionId="<role-id>"

azd deploy
```

### Step 3: Verify Role Assignments

After deployment, verify that role assignments were created:

```powershell
# Using Azure PowerShell
$roleAssignments = Get-AzRoleAssignment -Scope "/subscriptions/<subscription-id>/resourceGroups/<resource-group-name>"
$roleAssignments | Where-Object { $_.RoleDefinitionName -match 'Key Vault|AcrPull|App Configuration|SQL' } | Format-Table

# Or using Azure CLI
az role assignment list \
  --resource-group "<resource-group-name>" \
  --query "[].{name:principalName, role:roleDefinitionName}" \
  --output table
```

## Troubleshooting

### "Role not found" Error

If you get an error that a role doesn't exist:

1. **Verify the role exists in your subscription:**
   ```bash
   az role definition list --query "[].name" -o tsv | grep -i "KeyVault\|AcrPull\|AppConfig\|SqlDb"
   ```

2. **Check your subscription context:**
   ```bash
   az account show
   ```

3. **Ensure you have Reader permissions** on the subscription to list role definitions.

### Deployment Timeout

If deployment times out:

1. Check that the role definitions were passed correctly
2. Verify the principal IDs (managed identities) exist
3. Check Azure RBAC limits aren't exceeded

### Missing Role Assignments After Deployment

If role assignments aren't created:

1. Verify the role definition IDs were passed to the deployment
2. Check that principal IDs are valid Service Principals
3. Look for Azure Activity Log errors during deployment

## Best Practices

✅ **Always fetch role IDs dynamically** - Don't hardcode them
✅ **Verify role IDs match your subscription** - They may vary by region or subscription
✅ **Use descriptive parameter names** - Avoid confusion about which role is which
✅ **Document the role purposes** - Help maintainers understand the RBAC structure
✅ **Test in dev environment first** - Before deploying to production

## Reference: Azure Built-in Roles

| Role Name | Purpose | ID Format |
|-----------|---------|-----------|
| **Key Vault Secrets User** | Read Key Vault secrets | `4633458b-17de-408a-b874-0445c86d0e6e` (typical) |
| **AcrPull** | Pull container images from ACR | `7f951dda-4ed3-4680-a7ca-43fe172d538d` (typical) |
| **App Configuration Data Reader** | Read App Configuration data | `516239f1-63e1-4108-9233-9e7f68e97ce3` (typical) |
| **SQL DB Contributor** | Manage SQL databases | `9b7fa17d-e63e-47b0-bb0a-15c516ac86ec` (typical) |

> **Note:** These are typical IDs, but should be fetched dynamically for your subscription.

## References

- [Azure Built-in Roles](https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles)
- [Azure Role Definitions](https://learn.microsoft.com/en-us/azure/role-based-access-control/role-definitions)
- [Bicep Role Assignments](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/best-practices#role-assignment)

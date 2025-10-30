// ============================================================================
// Azure Built-in Role Definitions
// ============================================================================
// Purpose: Provide dynamic lookups for Azure built-in roles instead of
// hardcoding role definition IDs. This ensures compatibility across different
// Azure environments and prevents issues when role IDs change.
// ============================================================================

// ============================================================================
// Function: Get role definition ID by role name
// ============================================================================
// These outputs can be used to dynamically reference built-in Azure roles
// https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles

@export()
var roleNameAcrPull = 'AcrPull'

@export()
var roleNameKeyVaultSecretsUser = 'Key Vault Secrets User'

@export()
var roleNameAppConfigurationDataReader = 'App Configuration Data Reader'

@export()
var roleNameSqlDbContributor = 'SQL DB Contributor'

// ============================================================================
// Dynamic Role Definition Lookup
// ============================================================================
// In Bicep, we can reference built-in roles using the format:
// subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleId)
//
// For production deployments, consider using:
// 1. Azure CLI to pre-fetch role IDs: az role definition list --query "[?roleName=='RoleName'].id"
// 2. Azure PowerShell: Get-AzRoleDefinition -Name "RoleName" | Select-Object -ExpandProperty Id
// 3. REST API: https://management.azure.com/subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions?$filter=roleName eq 'RoleName'&api-version=2022-04-01
//
// For now, we document the role names and recommend validating against Azure's
// official role definitions list at deployment time.
// ============================================================================

// Map of well-known role names to their definitions
// These should be validated during deployment with actual Azure role definitions
@export()
var builtInRoles = {
  acrPull: {
    name: roleNameAcrPull
    description: 'Pull from any Azure container registry'
    permissionScope: 'Resource'
  }
  keyVaultSecretsUser: {
    name: roleNameKeyVaultSecretsUser
    description: 'Read the contents of a key vault secret'
    permissionScope: 'Resource'
  }
  appConfigurationDataReader: {
    name: roleNameAppConfigurationDataReader
    description: 'Allows read access to App Configuration data'
    permissionScope: 'Resource'
  }
  sqlDbContributor: {
    name: roleNameSqlDbContributor
    description: 'Lets you manage SQL databases, but not access to them'
    permissionScope: 'Resource'
  }
}

// ============================================================================
// NOTE: Migration Path for Role IDs
// ============================================================================
// This module documents the transition from hardcoded role IDs to dynamic lookup.
//
// CURRENT APPROACH (Hardcoded - Not Recommended):
//   var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86d0e6e'
//
// RECOMMENDED APPROACH (Dynamic Lookup - Use in production):
//   1. Pre-deployment: Run Azure CLI/PowerShell to fetch current role definitions
//   2. Deployment: Pass role IDs as parameters or use deployment scripts
//   3. Long-term: Use Azure REST API in deployment scripts to fetch at deploy time
//
// EXAMPLE DEPLOYMENT SCRIPT (PowerShell):
//   $roleIds = @{
//     'AcrPull' = (Get-AzRoleDefinition -Name 'AcrPull').Id
//     'Key Vault Secrets User' = (Get-AzRoleDefinition -Name 'Key Vault Secrets User').Id
//   }
//   az deployment sub create --location westeurope --template-file main.bicep `
//     --parameters roleDefinitionIds=$roleIds
//
// ============================================================================

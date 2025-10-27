// ============================================================================
// Key Vault RBAC Role Assignments Module
// ============================================================================
// PHASE 3: RBAC Configuration
// 
// Purpose: Grant microservice managed identities read access to Key Vault secrets
// 
// Resources Created:
// - Key Vault role assignment (Key Vault Secrets User role)
// 
// Input: principalId of the microservice managed identity
// Output: Role assignment resource ID
// ============================================================================

@description('Key Vault resource ID to assign roles to')
param keyVaultId string

@description('Principal ID to grant access (microservice managed identity)')
param principalId string

// Key Vault Secrets User role (built-in Azure role)
// Allows: Read secrets
// Denies: Create, delete, modify secrets or access policies
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86d0e6e'

// ============================================================================
// Role Assignment: Grant microservice MI access to read Key Vault secrets
// ============================================================================
// This allows the microservice's managed identity to read:
// - JWT secret keys
// - Redis cache passwords
// - SQL connection strings
// ============================================================================

resource keyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVaultId, principalId, keyVaultSecretsUserRoleId)
  scope: resourceGroup()
  properties: {
    principalId: principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('Role assignment resource ID')
output roleAssignmentId string = keyVaultRoleAssignment.id

@description('Role definition ID granted')
output roleDefinitionId string = keyVaultSecretsUserRoleId

@description('Principal ID that received access')
output grantedPrincipalId string = principalId

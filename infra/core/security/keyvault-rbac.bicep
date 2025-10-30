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
// Input: principalId of the microservice managed identity, roleDefinitionId for the role
// Output: Role assignment resource ID
// ============================================================================

@description('Key Vault resource ID to assign roles to')
param keyVaultId string

@description('Principal ID to grant access (microservice managed identity)')
param principalId string

@description('Role Definition ID - Key Vault Secrets User role ID')
@minLength(36)
@maxLength(36)
param roleDefinitionId string

// Key Vault Secrets User role (built-in Azure role)
// Allows: Read secrets
// Denies: Create, delete, modify secrets or access policies
// Reference: https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#key-vault-secrets-user

// ============================================================================
// Role Assignment: Grant microservice MI access to read Key Vault secrets
// ============================================================================
// This allows the microservice's managed identity to read:
// - JWT secret keys
// - Redis cache passwords
// - SQL connection strings
// ============================================================================

resource keyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVaultId, principalId, roleDefinitionId)
  scope: resourceGroup()
  properties: {
    principalId: principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleDefinitionId)
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('Role assignment resource ID')
output roleAssignmentId string = keyVaultRoleAssignment.id

@description('Role definition ID granted')
output grantedRoleDefinitionId string = roleDefinitionId

@description('Principal ID that received access')
output grantedPrincipalId string = principalId

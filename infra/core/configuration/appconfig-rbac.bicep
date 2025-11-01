// ============================================================================
// App Configuration RBAC Role Assignments Module
// ============================================================================
// PHASE 3B: Centralized RBAC Configuration
// 
// Purpose: Grant microservice managed identities read access to App Configuration
// This is the SINGLE POINT OF ACCESS for all service secrets and settings
// 
// Resources Created:
// - App Configuration role assignment (App Configuration Data Reader role)
// 
// Input: principalId of the microservice managed identity, roleDefinitionId for the role
// Output: Role assignment resource ID
// ============================================================================

@description('App Configuration resource ID to assign roles to')
param appConfigId string

@description('Principal ID to grant access (microservice managed identity)')
param principalId string

@description('Role Definition ID - App Configuration Data Reader role ID')
@minLength(36)
@maxLength(36)
param roleDefinitionId string

// App Configuration Data Reader role (built-in Azure role)
// Allows: Read configuration keys and values (including Key Vault references)
// Denies: Create, delete, modify configuration or access policies
// Reference: https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#app-configuration-data-reader

// ============================================================================
// Role Assignment: Grant microservice MI access to read App Configuration
// ============================================================================
// This is the CENTRALIZED ACCESS POINT for services to get:
// - JWT secret keys (via Key Vault reference)
// - Redis cache password (via Key Vault reference)
// - SQL connection strings (via Key Vault reference)
// - CORS settings
// - Environment-specific configuration
// ============================================================================

resource appConfigRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(appConfigId, principalId, roleDefinitionId)
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
output roleAssignmentId string = appConfigRoleAssignment.id

@description('Role definition ID granted')
output grantedRoleDefinitionId string = roleDefinitionId

@description('Principal ID that received access')
output grantedPrincipalId string = principalId

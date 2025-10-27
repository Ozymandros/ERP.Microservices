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
// Input: principalId of the microservice managed identity
// Output: Role assignment resource ID
// ============================================================================

@description('App Configuration resource ID to assign roles to')
param appConfigId string

@description('Principal ID to grant access (microservice managed identity)')
param principalId string

// App Configuration Data Reader role (built-in Azure role)
// Allows: Read configuration keys and values (including Key Vault references)
// Denies: Create, delete, modify configuration or access policies
var appConfigDataReaderRoleId = '516239f1-63e1-4108-9233-9e7f68e97ce3'

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
  name: guid(appConfigId, principalId, appConfigDataReaderRoleId)
  scope: resourceGroup()
  properties: {
    principalId: principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', appConfigDataReaderRoleId)
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('Role assignment resource ID')
output roleAssignmentId string = appConfigRoleAssignment.id

@description('Role definition ID granted')
output roleDefinitionId string = appConfigDataReaderRoleId

@description('Principal ID that received access')
output grantedPrincipalId string = principalId

// ============================================================================
// RBAC Role Assignments Module (Resource Group Scope)
// This module handles all role assignments within the resource group
// Single service principal (shared by all 7 services via user-assigned identity)
// ============================================================================

import { azureRoleIdAppConfigurationDataReader, azureRoleIdKeyVaultSecretsUser } from 'config/constants.bicep'

@description('App Configuration store name')
param appConfigName string

@description('Key Vault name')
param keyVaultName string

@description('Service Principal ID (all 7 microservices + gateway share same user-assigned identity)')
param servicePrincipalId string

@description('App Configuration service principal ID (for Key Vault access)')
param appConfigPrincipalId string

// Built-in role definitions referenced by their well-known IDs
// Using subscriptionResourceId to construct the full resource ID
var appConfigurationDataReaderRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', azureRoleIdAppConfigurationDataReader)
var keyVaultSecretsUserRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', azureRoleIdKeyVaultSecretsUser)

// ============================================================================
// Service → App Configuration Role Assignment
// (Single assignment for shared identity used by all 7 services)
// ============================================================================

resource servicesAppConfigRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(appConfigName, servicePrincipalId, 'appconfig-reader')
  properties: {
    roleDefinitionId: appConfigurationDataReaderRoleId
    principalId: servicePrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ============================================================================
// Service → Key Vault Role Assignment
// (Allows services to resolve secrets referenced by App Configuration)
// ============================================================================

resource servicesKeyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVaultName, servicePrincipalId, 'keyvault-secrets-user-service')
  properties: {
    roleDefinitionId: keyVaultSecretsUserRoleId
    principalId: servicePrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ============================================================================
// App Configuration → Key Vault Role Assignment
// ============================================================================

resource appConfigKeyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVaultName, appConfigPrincipalId, 'keyvault-secrets-user-appconfig')
  properties: {
    roleDefinitionId: keyVaultSecretsUserRoleId
    principalId: appConfigPrincipalId
    principalType: 'ServicePrincipal'
  }
}

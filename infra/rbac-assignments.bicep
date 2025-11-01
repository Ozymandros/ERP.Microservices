// ============================================================================
// RBAC Role Assignments Module (Resource Group Scope)
// This module handles all role assignments within the resource group
// NO manual parameters, using direct references from calling context
// ============================================================================

import { azureRoleIdAppConfigurationDataReader, azureRoleIdKeyVaultSecretsUser } from 'config/constants.bicep'

param appConfigName string
param keyVaultName string
param authServicePrincipalId string
param billingServicePrincipalId string
param inventoryServicePrincipalId string
param ordersServicePrincipalId string
param purchasingServicePrincipalId string
param salesServicePrincipalId string
param apiGatewayPrincipalId string
param appConfigPrincipalId string

// Built-in role definitions referenced by their well-known IDs
resource appConfigurationDataReaderRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: subscription()
  name: azureRoleIdAppConfigurationDataReader
}

resource keyVaultSecretsUserRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: subscription()
  name: azureRoleIdKeyVaultSecretsUser
}

// ============================================================================
// Service → App Configuration Role Assignments
// ============================================================================

resource authServiceAppConfigRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(appConfigName, authServicePrincipalId, 'appconfig-reader')
  properties: {
    roleDefinitionId: appConfigurationDataReaderRole.id
    principalId: authServicePrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource billingServiceAppConfigRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(appConfigName, billingServicePrincipalId, 'appconfig-reader')
  properties: {
    roleDefinitionId: appConfigurationDataReaderRole.id
    principalId: billingServicePrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource inventoryServiceAppConfigRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(appConfigName, inventoryServicePrincipalId, 'appconfig-reader')
  properties: {
    roleDefinitionId: appConfigurationDataReaderRole.id
    principalId: inventoryServicePrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource ordersServiceAppConfigRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(appConfigName, ordersServicePrincipalId, 'appconfig-reader')
  properties: {
    roleDefinitionId: appConfigurationDataReaderRole.id
    principalId: ordersServicePrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource purchasingServiceAppConfigRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(appConfigName, purchasingServicePrincipalId, 'appconfig-reader')
  properties: {
    roleDefinitionId: appConfigurationDataReaderRole.id
    principalId: purchasingServicePrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource salesServiceAppConfigRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(appConfigName, salesServicePrincipalId, 'appconfig-reader')
  properties: {
    roleDefinitionId: appConfigurationDataReaderRole.id
    principalId: salesServicePrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource apiGatewayAppConfigRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(appConfigName, apiGatewayPrincipalId, 'appconfig-reader')
  properties: {
    roleDefinitionId: appConfigurationDataReaderRole.id
    principalId: apiGatewayPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ============================================================================
// App Configuration → Key Vault Role Assignment
// ============================================================================

resource appConfigKeyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVaultName, appConfigPrincipalId, 'keyvault-secrets-user')
  properties: {
    roleDefinitionId: keyVaultSecretsUserRole.id
    principalId: appConfigPrincipalId
    principalType: 'ServicePrincipal'
  }
}

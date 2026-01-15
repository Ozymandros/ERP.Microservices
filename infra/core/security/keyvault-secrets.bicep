@description('Name of the Key Vault')
param name string

@description('Location for Key Vault')
param location string = resourceGroup().location

@description('Tags to apply')
param tags object = {}

@description('Redis host name')
param redisHostName string

@description('Redis primary key')
@secure()
param redisPrimaryKey string

@description('Redis cache password for authentication')
@secure()
param redisCachePassword string = ''

@description('SQL Server FQDN')
param sqlFqdn string

@description('SQL admin password')
@secure()
param sqlAdminPassword string

@description('Database name for the Auth service')
param authDbName string

@description('Database name for the Billing service')
param billingDbName string

@description('Database name for the Inventory service')
param inventoryDbName string

@description('Database name for the Orders service')
param ordersDbName string

@description('Database name for the Purchasing service')
param purchasingDbName string

@description('Database name for the Sales service')
param salesDbName string

@description('JWT secret value')
@secure()
param jwtSecretKey string

@description('Application Insights connection string')
@secure()
param applicationInsightsConnectionString string = ''

@description('User-Assigned Managed Identity Principal ID for Key Vault access')
param userAssignedIdentityPrincipalId string

import { azureRoleIdKeyVaultSecretsUser } from '../../config/constants.bicep'

// Key Vault

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: name
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    accessPolicies: []
    enabledForTemplateDeployment: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    createMode: 'recover'  // Automatically recover if soft-deleted
  }
  tags: tags
}

// Grant User-Assigned Identity access to Key Vault secrets
resource keyVaultSecretsUserRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, userAssignedIdentityPrincipalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', azureRoleIdKeyVaultSecretsUser))
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', azureRoleIdKeyVaultSecretsUser)
    principalId: userAssignedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Redis secret
resource kvRedisSecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'redis-connection'
  properties: {
    value: '${redisHostName}:6380,password=${redisPrimaryKey},ssl=True,abortConnect=False'
  }
}

// ✅ Redis cache password secret (for authentication)
resource kvRedisCachePasswordSecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = if (!empty(redisCachePassword)) {
  parent: keyVault
  name: 'redis-cache-password'
  properties: {
    value: redisCachePassword
  }
}

// JWT secret
resource kvJwtSecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'jwt-secret-key'
  properties: {
    value: jwtSecretKey
  }
}

// Application Insights connection string
resource kvApplicationInsightsConnectionString 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = if (!empty(applicationInsightsConnectionString)) {
  parent: keyVault
  name: 'applicationinsights-connection-string'
  properties: {
    value: applicationInsightsConnectionString
  }
}

// SQL DB secrets
resource kvSqlSecretAuth 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'sql-connection-authdb'
  properties: {
    value: 'Server=${sqlFqdn};Database=${authDbName};User Id=sqladmin;Password=${sqlAdminPassword};TrustServerCertificate=True;'
  }
}

resource kvSqlSecretBilling 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'sql-connection-billingdb'
  properties: {
    value: 'Server=${sqlFqdn};Database=${billingDbName};User Id=sqladmin;Password=${sqlAdminPassword};TrustServerCertificate=True;'
  }
}

resource kvSqlSecretInventory 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'sql-connection-inventorydb'
  properties: {
    value: 'Server=${sqlFqdn};Database=${inventoryDbName};User Id=sqladmin;Password=${sqlAdminPassword};TrustServerCertificate=True;'
  }
}

resource kvSqlSecretOrders 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'sql-connection-ordersdb'
  properties: {
    value: 'Server=${sqlFqdn};Database=${ordersDbName};User Id=sqladmin;Password=${sqlAdminPassword};TrustServerCertificate=True;'
  }
}

resource kvSqlSecretPurchasing 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'sql-connection-purchasingdb'
  properties: {
    value: 'Server=${sqlFqdn};Database=${purchasingDbName};User Id=sqladmin;Password=${sqlAdminPassword};TrustServerCertificate=True;'
  }
}

resource kvSqlSecretSales 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'sql-connection-salesdb'
  properties: {
    value: 'Server=${sqlFqdn};Database=${salesDbName};User Id=sqladmin;Password=${sqlAdminPassword};TrustServerCertificate=True;'
  }
}

// Deployment script to wait for RBAC propagation to Azure AD
// This prevents Container Apps from starting before permissions are fully available
resource rbacPropagationWait 'Microsoft.Resources/deploymentScripts@2023-08-01' = {
  name: 'rbac-propagation-wait'
  location: location
  kind: 'AzurePowerShell'
  properties: {
    azPowerShellVersion: '11.0'
    retentionInterval: 'PT1H'
    timeout: 'PT5M'
    cleanupPreference: 'OnSuccess'
    scriptContent: '''
      Write-Host "Waiting 60 seconds for RBAC propagation to Azure AD..."
      Start-Sleep -Seconds 60
      Write-Host "RBAC propagation wait complete."
    '''
  }
  dependsOn: [
    keyVaultSecretsUserRoleAssignment
  ]
}

output keyVaultId string = keyVault.id
output keyVaultUri string = keyVault.properties.vaultUri
output keyVaultName string = keyVault.name
output rbacReady string = rbacPropagationWait.id

// Return secret NAMES (not values) so callers can reference secret names safely
output redisSecretName string = kvRedisSecret.name
output redisAuthSecretName string = 'redis-cache-password'
output jwtSecretName string = kvJwtSecret.name
output sqlAuthSecretName string = kvSqlSecretAuth.name
output sqlBillingSecretName string = kvSqlSecretBilling.name
output sqlInventorySecretName string = kvSqlSecretInventory.name
output sqlOrdersSecretName string = kvSqlSecretOrders.name
output sqlPurchasingSecretName string = kvSqlSecretPurchasing.name
output sqlSalesSecretName string = kvSqlSecretSales.name

@description('Location for App Configuration resource')
param location string = resourceGroup().location

@description('Environment name (dev, staging, prod)')
param environmentName string = 'Production'

@description('Resource tags')
param tags object

@description('Key Vault name for secret references')
param keyVaultName string

@description('JWT Issuer value')
param jwtIssuer string

@description('JWT Audience value')
param jwtAudience string

@description('Frontend origin for CORS')
param frontendOrigin string

@description('ASP.NET Core environment')
param aspnetcoreEnvironment string

var appConfigName = 'appconfig-${uniqueString(resourceGroup().id)}'

// ============================================================================
// Azure App Configuration Store
// ============================================================================
// Centralized configuration store for all microservices
// - Non-sensitive settings (JWT issuer, audience, CORS origins)
// - References to Key Vault for sensitive secrets
// - Environment-specific configurations via labels
// ============================================================================

resource appConfig 'Microsoft.AppConfiguration/configurationStores@2023-03-01' = {
  name: appConfigName
  location: location
  sku: {
    name: 'free'
  }
  tags: union(tags, {
    'environment-name': environmentName
  })
  identity: {
    type: 'SystemAssigned'
  }
}

// ============================================================================
// JWT Configuration Settings (Non-Sensitive)
// ============================================================================

resource jwtIssuerSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'Jwt:Issuer'
  properties: {
    value: jwtIssuer
    contentType: 'application/json'
  }
}

resource jwtAudienceSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'Jwt:Audience'
  properties: {
    value: jwtAudience
    contentType: 'application/json'
  }
}

// ============================================================================
// Frontend CORS Configuration
// ============================================================================
// Frontend origin is environment-specific and stored centrally
// Services pull this at runtime via App Configuration

resource frontendOriginSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'Frontend:Origin'
  properties: {
    value: frontendOrigin
    contentType: 'application/json'
  }
}

// ============================================================================
// ASP.NET Core Environment Setting
// ============================================================================
// Controls logging, error pages, and feature behavior across all services

resource environmentSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'ASPNETCORE_ENVIRONMENT'
  properties: {
    value: aspnetcoreEnvironment
    contentType: 'application/json'
  }
}

// ============================================================================
// Redis Cache Configuration
// ============================================================================
// Connection string for distributed cache used by all services
// Stored as Key Vault reference for security

resource redisCacheSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'Redis:Connection'
  properties: {
    value: '{"uri":"${keyVaultName}/secrets/redis-connection"}'
    contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
  }
}

// ============================================================================
// JWT Secret Key - Key Vault Reference
// ============================================================================
// Sensitive secret stored in Key Vault
// App Configuration holds only a reference to it
// Services will resolve the reference at runtime

resource jwtSecretKeyRef 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'Jwt:SecretKey'
  properties: {
    value: '{"uri":"${keyVaultName}/secrets/jwt-secret-key"}'
    contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
  }
}

// ============================================================================
// Redis Cache Password - Key Vault Reference
// ============================================================================
// Redis authentication password stored securely in Key Vault
// App Configuration holds only a reference to it
// Services will resolve the reference at runtime for cache authentication

resource redisCachePasswordRef 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'Redis:Password'
  properties: {
    value: '{"uri":"${keyVaultName}/secrets/redis-cache-password"}'
    contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
  }
}

// ============================================================================
// Database Connection Strings - Key Vault References
// ============================================================================
// Each service has a specific database connection string
// Stored in Key Vault for security

resource sqlAuthDbRef 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'Sql:ConnectionStrings:AuthDb'
  properties: {
    value: '{"uri":"${keyVaultName}/secrets/sql-connection-authdb"}'
    contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
  }
}

resource sqlBillingDbRef 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'Sql:ConnectionStrings:BillingDb'
  properties: {
    value: '{"uri":"${keyVaultName}/secrets/sql-connection-billingdb"}'
    contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
  }
}

resource sqlInventoryDbRef 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'Sql:ConnectionStrings:InventoryDb'
  properties: {
    value: '{"uri":"${keyVaultName}/secrets/sql-connection-inventorydb"}'
    contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
  }
}

resource sqlOrdersDbRef 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'Sql:ConnectionStrings:OrdersDb'
  properties: {
    value: '{"uri":"${keyVaultName}/secrets/sql-connection-ordersdb"}'
    contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
  }
}

resource sqlPurchasingDbRef 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'Sql:ConnectionStrings:PurchasingDb'
  properties: {
    value: '{"uri":"${keyVaultName}/secrets/sql-connection-purchasingdb"}'
    contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
  }
}

resource sqlSalesDbRef 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'Sql:ConnectionStrings:SalesDb'
  properties: {
    value: '{"uri":"${keyVaultName}/secrets/sql-connection-salesdb"}'
    contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
  }
}

// ============================================================================
// Grant App Configuration's Managed Identity Access to Key Vault
// ============================================================================
// This allows App Configuration to resolve Key Vault references at runtime

resource keyVaultResource 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
  name: keyVaultName
}

resource appConfigKeyVaultAccess 'Microsoft.KeyVault/vaults/accessPolicies@2022-07-01' = {
  parent: keyVaultResource
  name: 'add'
  properties: {
    accessPolicies: [
      {
        objectId: appConfig.identity.principalId
        tenantId: subscription().tenantId
        permissions: {
          secrets: ['get', 'list']
        }
      }
    ]
  }
}

// ============================================================================
// Outputs
// ============================================================================
// Connection string for services to connect to App Configuration
// Endpoint for direct queries
// Managed identity for RBAC assignments

@description('Endpoint URL for App Configuration')
output appConfigEndpoint string = appConfig.properties.endpoint

@description('Name of App Configuration resource')
output appConfigName string = appConfig.name

@description('Resource ID of App Configuration')
output appConfigResourceId string = appConfig.id

@description('Managed identity principal ID for RBAC')
output appConfigPrincipalId string = appConfig.identity.principalId

@description('Connection string for App Configuration - marked as secure')
@secure()
output appConfigConnectionString string = appConfig.listKeys().value[0].connectionString

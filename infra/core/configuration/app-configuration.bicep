// ============================================================================
// Basic Parameters
// ============================================================================

@description('Location for App Configuration resource')
param location string = resourceGroup().location

@description('Resource tags')
param tags object

@description('App Configuration resource name')
param appConfigName string

// ============================================================================
// Configuration Parameters
// ============================================================================

@description('Environment name (dev, staging, prod)')
param environmentName string = 'Production'

@description('Key Vault name for secret references')
param keyVaultName string

@description('JWT Issuer value')
param jwtIssuer string

@description('JWT Audience value')
param jwtAudience string

@description('Frontend origin for CORS')
param frontendOrigin string

@description('ASP.NET Core environment')
// Removed unused parameter for clean build

// ============================================================================
// Derived Variables
// ============================================================================

var keyVaultUri = 'https://${keyVaultName}.${environment().suffixes.keyvaultDns}'

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
// JWT Secret Key - Key Vault Reference
// ============================================================================
// Sensitive secret stored in Key Vault
// App Configuration holds only a reference to it
// Services will resolve the reference at runtime via MI + DefaultAzureCredential
// Key matches docker-compose: Jwt__SecretKey

resource jwtSecretKeyRef 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'Jwt:SecretKey'
  properties: {
    value: '{"uri":"${keyVaultUri}/secrets/jwt-secret-key"}'
    contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
  }
}

// ============================================================================
// Redis Cache Configuration - Direct Connection String via Key Vault
// ============================================================================
// Connection string for distributed cache (StackExchange.Redis)
// Stored as Key Vault reference; services access via App Config
// Key matches docker-compose: ConnectionStrings__cache

resource redisCacheSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'ConnectionStrings:cache'
  properties: {
    value: '{"uri":"${keyVaultUri}/secrets/redis-connection"}'
    contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
  }
}

// ============================================================================
// DAPR Configuration - Component Names
// ============================================================================
// Services use these to configure DAPR sidecars and state/pubsub access
// Matches DAPR component names defined in container-apps-environment.bicep

resource daprStateStoreSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'Dapr:StateStore'
  properties: {
    value: 'statestore'
    contentType: 'application/json'
  }
}

resource daprPubSubSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'Dapr:PubSub'
  properties: {
    value: 'pubsub'
    contentType: 'application/json'
  }
}

// ============================================================================
// Distributed Cache Configuration
// ============================================================================
// Services use this key to locate Redis for IDistributedCache
// Matches docker-compose env var name format
// Note: In Aspire, this is auto-resolved; in Azure, app code reads from App Config

// NOTE: redis-connection is already defined above in ConnectionStrings:cache
// This ConnectionStrings:Redis is redundant; kept for alternate cache implementations
resource cacheConnectionSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'ConnectionStrings:Redis'
  properties: {
    value: '{"uri":"${keyVaultUri}/secrets/redis-connection"}'
    contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
  }
}

// ============================================================================

// ============================================================================
// Database Connection Strings - Key Vault References
// ============================================================================
// Each service has a specific database connection string
// Stored in Key Vault for security
// Keys match docker-compose: ConnectionStrings__AuthDb, etc.

resource sqlAuthDbRef 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'ConnectionStrings:AuthDb'
  properties: {
    value: '{"uri":"${keyVaultUri}/secrets/sql-connection-authdb"}'
    contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
  }
}

resource sqlBillingDbRef 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'ConnectionStrings:BillingDb'
  properties: {
    value: '{"uri":"${keyVaultUri}/secrets/sql-connection-billingdb"}'
    contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
  }
}

resource sqlInventoryDbRef 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'ConnectionStrings:InventoryDb'
  properties: {
    value: '{"uri":"${keyVaultUri}/secrets/sql-connection-inventorydb"}'
    contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
  }
}

resource sqlOrdersDbRef 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'ConnectionStrings:OrdersDb'
  properties: {
    value: '{"uri":"${keyVaultUri}/secrets/sql-connection-ordersdb"}'
    contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
  }
}

resource sqlPurchasingDbRef 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'ConnectionStrings:PurchasingDb'
  properties: {
    value: '{"uri":"${keyVaultUri}/secrets/sql-connection-purchasingdb"}'
    contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
  }
}

resource sqlSalesDbRef 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'ConnectionStrings:SalesDb'
  properties: {
    value: '{"uri":"${keyVaultUri}/secrets/sql-connection-salesdb"}'
    contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
  }
}

// ============================================================================
// Application Insights Connection String - Key Vault Reference
// ============================================================================
// Centralized telemetry connection string for all services
// Services access via App Configuration, which resolves Key Vault reference
// Key matches ASP.NET Core: APPLICATIONINSIGHTS_CONNECTION_STRING

resource applicationInsightsConnectionStringRef 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'ApplicationInsights:ConnectionString'
  properties: {
    value: '{"uri":"${keyVaultUri}/secrets/applicationinsights-connection-string"}'
    contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
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

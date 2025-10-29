targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention, the name of the resource group for your application will use this name, prefixed with rg-')
param environmentName string = 'dev' // 👈 Valor per defecte segur

@minLength(1)
@description('The location used for all deployed resources')
param location string = 'westeurope' // 👈 Valor per defecte segur

// Secrets generats amb valor per defecte per evitar el xoc amb el parser
@secure()
param cache_password string = base64(newGuid()) 

@secure()
param password string = base64(newGuid())      

@description('JWT secret key for token signing (HS256 requires 32 bytes/256 bits)')
@secure()
param jwtSecretKey string = base64('${newGuid()}${newGuid()}') 

@description('JWT token issuer (e.g., MyApp.Auth)')
param jwtIssuer string = 'MyApp.Auth'

@description('JWT token audience (e.g., MyApp.All)')
param jwtAudience string = 'MyApp.All'

@description('Frontend origin for CORS (semicolon-separated for multiple origins)')
param frontendOrigin string = 'http://localhost:3000;http://localhost:5000'

@description('Environment name (Development, Staging, Production)')
param aspnetcoreEnvironment string = 'Production'

var envSlug = toLower(replace(environmentName, ' ', '-'))
var namePrefix = 'myapp-${envSlug}'
var flatPrefix = toLower(replace(namePrefix, '-', ''))

var resourceGroupName = 'rg-${namePrefix}-core'

var managedIdentityName = take('${namePrefix}-user-assigned-identity', 128)
var containerRegistryName = take('${flatPrefix}containerregistry', 50)
var logAnalyticsWorkspaceName = take('${namePrefix}-log-analytics-workspace', 63)
var applicationInsightsName = take('${namePrefix}-application-insights', 260)
var storageAccountName = take('${flatPrefix}storageaccount', 24)
var storageShareName = toLower(take('${namePrefix}-cache-fileshare', 63))
var containerEnvironmentStorageName = take('${flatPrefix}cachevolume', 32)
var containerAppsEnvironmentName = take('${namePrefix}-container-apps-environment', 63)
var redisCacheName = take('${namePrefix}-redis-cache', 63)
var sqlServerName = take('${namePrefix}-sql-server', 63)
var sqlAdminIdentityName = take('${namePrefix}-sql-admin-identity', 128)
var keyVaultName = take('${flatPrefix}keyvault', 24)
var appConfigurationName = take('${namePrefix}-app-configuration', 50)
var sqlDatabaseNames = {
  auth: toLower('${namePrefix}-auth-db')
  billing: toLower('${namePrefix}-billing-db')
  inventory: toLower('${namePrefix}-inventory-db')
  orders: toLower('${namePrefix}-orders-db')
  purchasing: toLower('${namePrefix}-purchasing-db')
  sales: toLower('${namePrefix}-sales-db')
}

var sqlDatabaseList = [
  sqlDatabaseNames.auth
  sqlDatabaseNames.billing
  sqlDatabaseNames.inventory
  sqlDatabaseNames.orders
  sqlDatabaseNames.purchasing
  sqlDatabaseNames.sales
]

var tags = {
  'azd-env-name': environmentName
}

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}
module resources 'resources.bicep' = {
  scope: rg
  name: 'resources'
  params: {
    location: location
    tags: tags
    namePrefix: namePrefix
    managedIdentityName: managedIdentityName
    containerRegistryName: containerRegistryName
    logAnalyticsWorkspaceName: logAnalyticsWorkspaceName
    applicationInsightsName: applicationInsightsName
    storageAccountName: storageAccountName
    storageShareName: storageShareName
    containerEnvironmentStorageName: containerEnvironmentStorageName
    containerAppsEnvironmentName: containerAppsEnvironmentName
  }
}

module redis 'core/database/redis.bicep' = {
  name: 'redis'
  scope: rg
  params: {
    name: redisCacheName
    location: location
    tags: tags
    sku: 'Basic'
    family: 'C'
    capacity: 0
  }
}

module keyVault 'core/security/keyvault-secrets.bicep' = {
  name: 'keyvault'
  scope: rg
  params: {
    name: keyVaultName
    location: location
    tags: tags
    jwtSecretKey: jwtSecretKey
    redisHostName: redis.outputs.hostName
    redisPrimaryKey: redis.outputs.primaryKey
    redisCachePassword: cache_password
    sqlFqdn: myapp_sqlserver.outputs.sqlServerFqdn
    sqlAdminPassword: password
    authDbName: sqlDatabaseNames.auth
    billingDbName: sqlDatabaseNames.billing
    inventoryDbName: sqlDatabaseNames.inventory
    ordersDbName: sqlDatabaseNames.orders
    purchasingDbName: sqlDatabaseNames.purchasing
    salesDbName: sqlDatabaseNames.sales
    enableKeyVault: true
  }
}

module appConfiguration 'core/configuration/app-configuration.bicep' = {
  name: 'app-configuration'
  scope: rg
  params: {
    appConfigName: appConfigurationName
    location: location
    environmentName: aspnetcoreEnvironment
    tags: tags
    keyVaultName: keyVault.outputs.keyVaultName
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    frontendOrigin: frontendOrigin
    aspnetcoreEnvironment: aspnetcoreEnvironment
  }
}

module myapp_sqlserver 'myapp-sqlserver/myapp-sqlserver.module.bicep' = {
  name: 'myapp-sqlserver'
  scope: rg
  params: {
    location: location
    sqlServerName: sqlServerName
    sqlAdminIdentityName: sqlAdminIdentityName
    databaseNames: sqlDatabaseList
  }
}
module myapp_sqlserver_roles 'myapp-sqlserver-roles/myapp-sqlserver-roles.module.bicep' = {
  name: 'myapp-sqlserver-roles'
  scope: rg
  params: {
    myapp_sqlserver_outputs_name: myapp_sqlserver.outputs.name
    principalName: resources.outputs.MANAGED_IDENTITY_NAME
    principalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
  }
}

// Service modules
module authServiceModule 'services/auth-service.bicep' = {
  name: 'auth-service-deployment'
  scope: rg
  params: {
    location: location
    tags: tags
    containerAppsEnvironmentId: resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
    containerRegistryEndpoint: resources.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
    keyVaultUri: keyVault.outputs.keyVaultUri
    logAnalyticsWorkspaceId: resources.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_ID
    jwtSecretKey: jwtSecretKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    frontendOrigin: frontendOrigin
    aspnetcoreEnvironment: aspnetcoreEnvironment
    managedIdentityPrincipalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    appConfigConnectionString: appConfiguration.outputs.appConfigConnectionString
    namePrefix: namePrefix
  }
}

module billingServiceModule 'services/billing-service.bicep' = {
  name: 'billing-service-deployment'
  scope: rg
  params: {
    location: location
    tags: tags
    containerAppsEnvironmentId: resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
    containerRegistryEndpoint: resources.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
    keyVaultUri: keyVault.outputs.keyVaultUri
    logAnalyticsWorkspaceId: resources.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_ID
    jwtSecretKey: jwtSecretKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    frontendOrigin: frontendOrigin
    aspnetcoreEnvironment: aspnetcoreEnvironment
    managedIdentityPrincipalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    appConfigConnectionString: appConfiguration.outputs.appConfigConnectionString
    namePrefix: namePrefix
  }
}

module inventoryServiceModule 'services/inventory-service.bicep' = {
  name: 'inventory-service-deployment'
  scope: rg
  params: {
    location: location
    tags: tags
    containerAppsEnvironmentId: resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
    containerRegistryEndpoint: resources.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
    keyVaultUri: keyVault.outputs.keyVaultUri
    logAnalyticsWorkspaceId: resources.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_ID
    jwtSecretKey: jwtSecretKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    frontendOrigin: frontendOrigin
    aspnetcoreEnvironment: aspnetcoreEnvironment
    managedIdentityPrincipalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    appConfigConnectionString: appConfiguration.outputs.appConfigConnectionString
    namePrefix: namePrefix
  }
}

module ordersServiceModule 'services/orders-service.bicep' = {
  name: 'orders-service-deployment'
  scope: rg
  params: {
    location: location
    tags: tags
    containerAppsEnvironmentId: resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
    containerRegistryEndpoint: resources.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
    keyVaultUri: keyVault.outputs.keyVaultUri
    logAnalyticsWorkspaceId: resources.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_ID
    jwtSecretKey: jwtSecretKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    frontendOrigin: frontendOrigin
    aspnetcoreEnvironment: aspnetcoreEnvironment
    managedIdentityPrincipalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    appConfigConnectionString: appConfiguration.outputs.appConfigConnectionString
    namePrefix: namePrefix
  }
}

module purchasingServiceModule 'services/purchasing-service.bicep' = {
  name: 'purchasing-service-deployment'
  scope: rg
  params: {
    location: location
    tags: tags
    containerAppsEnvironmentId: resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
    containerRegistryEndpoint: resources.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
    keyVaultUri: keyVault.outputs.keyVaultUri
    logAnalyticsWorkspaceId: resources.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_ID
    jwtSecretKey: jwtSecretKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    frontendOrigin: frontendOrigin
    aspnetcoreEnvironment: aspnetcoreEnvironment
    managedIdentityPrincipalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    appConfigConnectionString: appConfiguration.outputs.appConfigConnectionString
    namePrefix: namePrefix
  }
}

module salesServiceModule 'services/sales-service.bicep' = {
  name: 'sales-service-deployment'
  scope: rg
  params: {
    location: location
    tags: tags
    containerAppsEnvironmentId: resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
    containerRegistryEndpoint: resources.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
    keyVaultUri: keyVault.outputs.keyVaultUri
    logAnalyticsWorkspaceId: resources.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_ID
    jwtSecretKey: jwtSecretKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    frontendOrigin: frontendOrigin
    aspnetcoreEnvironment: aspnetcoreEnvironment
    managedIdentityPrincipalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    appConfigConnectionString: appConfiguration.outputs.appConfigConnectionString
    namePrefix: namePrefix
  }
}

module apiGatewayModule 'services/api-gateway.bicep' = {
  name: 'api-gateway-deployment'
  scope: rg
  params: {
    location: location
    tags: tags
    containerAppsEnvironmentId: resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
    containerRegistryEndpoint: resources.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
    keyVaultUri: keyVault.outputs.keyVaultUri
    logAnalyticsWorkspaceId: resources.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_ID
    jwtSecretKey: jwtSecretKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    frontendOrigin: frontendOrigin
    aspnetcoreEnvironment: aspnetcoreEnvironment
    managedIdentityPrincipalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    appConfigConnectionString: appConfiguration.outputs.appConfigConnectionString
    namePrefix: namePrefix
  }
}

// ============================================================================
// PHASE 3B: Centralized RBAC - Services Access via App Configuration
// ============================================================================
// ARCHITECTURE: Centralized secret access through App Configuration
//
// Services → RBAC → App Configuration (Data Reader)
//                       ↓
//                   App Configuration → RBAC → Key Vault (Secrets User)
//
// Benefits:
// - Single point of access control
// - Simplified permission management
// - Centralized audit logging
// - Better security posture
// ============================================================================

// RBAC: Grant each microservice access to App Configuration
module authServiceAppConfigRbac 'core/configuration/appconfig-rbac.bicep' = {
  name: 'auth-service-appconfig-rbac'
  scope: rg
  params: {
    appConfigId: appConfiguration.outputs.appConfigResourceId
    principalId: authServiceModule.outputs.managedIdentityPrincipalId
  }
}

module billingServiceAppConfigRbac 'core/configuration/appconfig-rbac.bicep' = {
  name: 'billing-service-appconfig-rbac'
  scope: rg
  params: {
    appConfigId: appConfiguration.outputs.appConfigResourceId
    principalId: billingServiceModule.outputs.managedIdentityPrincipalId
  }
}

module inventoryServiceAppConfigRbac 'core/configuration/appconfig-rbac.bicep' = {
  name: 'inventory-service-appconfig-rbac'
  scope: rg
  params: {
    appConfigId: appConfiguration.outputs.appConfigResourceId
    principalId: inventoryServiceModule.outputs.managedIdentityPrincipalId
  }
}

module ordersServiceAppConfigRbac 'core/configuration/appconfig-rbac.bicep' = {
  name: 'orders-service-appconfig-rbac'
  scope: rg
  params: {
    appConfigId: appConfiguration.outputs.appConfigResourceId
    principalId: ordersServiceModule.outputs.managedIdentityPrincipalId
  }
}

module purchasingServiceAppConfigRbac 'core/configuration/appconfig-rbac.bicep' = {
  name: 'purchasing-service-appconfig-rbac'
  scope: rg
  params: {
    appConfigId: appConfiguration.outputs.appConfigResourceId
    principalId: purchasingServiceModule.outputs.managedIdentityPrincipalId
  }
}

module salesServiceAppConfigRbac 'core/configuration/appconfig-rbac.bicep' = {
  name: 'sales-service-appconfig-rbac'
  scope: rg
  params: {
    appConfigId: appConfiguration.outputs.appConfigResourceId
    principalId: salesServiceModule.outputs.managedIdentityPrincipalId
  }
}

module apiGatewayAppConfigRbac 'core/configuration/appconfig-rbac.bicep' = {
  name: 'api-gateway-appconfig-rbac'
  scope: rg
  params: {
    appConfigId: appConfiguration.outputs.appConfigResourceId
    principalId: apiGatewayModule.outputs.managedIdentityPrincipalId
  }
}

// RBAC: Grant App Configuration access to Key Vault (centralized secret management)
// This is the SINGLE POINT where App Configuration reads secrets from Key Vault
module appConfigKeyVaultRbac 'core/security/keyvault-rbac.bicep' = {
  name: 'appconfig-keyvault-rbac'
  scope: rg
  params: {
    keyVaultId: keyVault.outputs.keyVaultId
    principalId: appConfiguration.outputs.appConfigPrincipalId
  }
}

output MANAGED_IDENTITY_CLIENT_ID string = resources.outputs.MANAGED_IDENTITY_CLIENT_ID
output MANAGED_IDENTITY_NAME string = resources.outputs.MANAGED_IDENTITY_NAME
output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = resources.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_NAME
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = resources.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = resources.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID
output AZURE_CONTAINER_REGISTRY_NAME string = resources.outputs.AZURE_CONTAINER_REGISTRY_NAME
output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_NAME
output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN
output SERVICE_CACHE_VOLUME_REDISCACHE_NAME string = resources.outputs.SERVICE_CACHE_VOLUME_REDISCACHE_NAME
output AZURE_VOLUMES_STORAGE_ACCOUNT string = resources.outputs.AZURE_VOLUMES_STORAGE_ACCOUNT
output MYAPP_APPLICATIONINSIGHTS_APPINSIGHTSCONNECTIONSTRING string = resources.outputs.AZURE_APPLICATION_INSIGHTS_CONNECTION_STRING
output MYAPP_SQLSERVER_SQLSERVERFQDN string = myapp_sqlserver.outputs.sqlServerFqdn
output AZURE_REDIS_CACHE_NAME string = redis.outputs.name
output AZURE_REDIS_CACHE_HOST string = redis.outputs.hostName
output AZURE_REDIS_CACHE_PORT int = redis.outputs.sslPort
output AZURE_SQL_SERVER_NAME string = myapp_sqlserver.outputs.name
output AZURE_SQL_SERVER_FQDN string = myapp_sqlserver.outputs.sqlServerFqdn
output AZURE_KEY_VAULT_NAME string = keyVault.outputs.keyVaultUri

// Service outputs
output AUTH_SERVICE_FQDN string = authServiceModule.outputs.fqdn
output BILLING_SERVICE_FQDN string = billingServiceModule.outputs.fqdn
output INVENTORY_SERVICE_FQDN string = inventoryServiceModule.outputs.fqdn
output ORDERS_SERVICE_FQDN string = ordersServiceModule.outputs.fqdn
output PURCHASING_SERVICE_FQDN string = purchasingServiceModule.outputs.fqdn
output SALES_SERVICE_FQDN string = salesServiceModule.outputs.fqdn
output API_GATEWAY_FQDN string = apiGatewayModule.outputs.fqdn
output API_GATEWAY_URI string = apiGatewayModule.outputs.uri

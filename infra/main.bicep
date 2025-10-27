targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention, the name of the resource group for your application will use this name, prefixed with rg-')
param environmentName string

@minLength(1)
@description('The location used for all deployed resources')
param location string

@description('Id of the user or app to assign application roles')
param principalId string = ''

@metadata({azd: {
  type: 'generate'
  config: {length:22,noSpecial:true}
  }
})
@secure()
param cache_password string
@secure()
param password string

@description('JWT secret key for token signing')
@secure()
param jwtSecretKey string

@description('JWT token issuer (e.g., MyApp.Auth)')
param jwtIssuer string = 'MyApp.Auth'

@description('JWT token audience (e.g., MyApp.All)')
param jwtAudience string = 'MyApp.All'

@description('Frontend origin for CORS (semicolon-separated for multiple origins)')
param frontendOrigin string = 'http://localhost:3000;http://localhost:5000'

@description('Environment name (Development, Staging, Production)')
param aspnetcoreEnvironment string = 'Production'

var tags = {
  'azd-env-name': environmentName
}
var resourceToken = uniqueString(subscription().id, environmentName)

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}
module resources 'resources.bicep' = {
  scope: rg
  name: 'resources'
  params: {
    location: location
    tags: tags
    principalId: principalId
  }
}

module redis 'core/database/redis.bicep' = {
  name: 'redis'
  scope: rg
  params: {
    name: 'redis-${resourceToken}'
    location: location
    tags: tags
    sku: 'Standard'
    family: 'C'
    capacity: 1
  }
}

module sqlServer 'core/database/sql-server.bicep' = {
  name: 'sqlserver'
  scope: rg
  params: {
    name: 'sql-${resourceToken}'
    location: location
    tags: tags
    administratorLogin: 'sqladmin'
    administratorLoginPassword: password
    databases: [
      {name: 'AuthDB'}
      {name: 'BillingDB'}
      {name: 'InventoryDB'}
      {name: 'OrdersDB'}
      {name: 'PurchasingDB'}
      {name: 'SalesDB'}
    ]
    minimalTlsVersion: '1.2'
  }
}

module keyVault 'core/security/keyvault-secrets.bicep' = {
  name: 'keyvault'
  scope: rg
  params: {
    name: 'kv-${resourceToken}'
    location: location
    tags: tags
    jwtSecretKey: jwtSecretKey
    redisHostName: redis.outputs.hostName
    redisPrimaryKey: redis.outputs.primaryKey
    sqlFqdn: sqlServer.outputs.fqdn
    sqlAdminPassword: password
    enableKeyVault: true
  }
}

module appConfiguration 'core/configuration/app-configuration.bicep' = {
  name: 'app-configuration'
  scope: rg
  params: {
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

module MyApp_ApplicationInsights 'MyApp-ApplicationInsights/MyApp-ApplicationInsights.module.bicep' = {
  name: 'MyApp-ApplicationInsights'
  scope: rg
  params: {
    location: location
    myapp_loganalyticsworkspace_outputs_loganalyticsworkspaceid: MyApp_LogAnalyticsWorkspace.outputs.logAnalyticsWorkspaceId
  }
}
module MyApp_LogAnalyticsWorkspace 'MyApp-LogAnalyticsWorkspace/MyApp-LogAnalyticsWorkspace.module.bicep' = {
  name: 'MyApp-LogAnalyticsWorkspace'
  scope: rg
  params: {
    location: location
  }
}
module myapp_sqlserver 'myapp-sqlserver/myapp-sqlserver.module.bicep' = {
  name: 'myapp-sqlserver'
  scope: rg
  params: {
    location: location
  }
}
module myapp_sqlserver_roles 'myapp-sqlserver-roles/myapp-sqlserver-roles.module.bicep' = {
  name: 'myapp-sqlserver-roles'
  scope: rg
  params: {
    location: location
    myapp_sqlserver_outputs_name: myapp_sqlserver.outputs.name
    myapp_sqlserver_outputs_sqlserveradminname: myapp_sqlserver.outputs.sqlServerAdminName
    principalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    principalName: resources.outputs.MANAGED_IDENTITY_NAME
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
    logAnalyticsWorkspaceId: MyApp_LogAnalyticsWorkspace.outputs.logAnalyticsWorkspaceId
    jwtSecretKey: jwtSecretKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    frontendOrigin: frontendOrigin
    aspnetcoreEnvironment: aspnetcoreEnvironment
    managedIdentityPrincipalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    appConfigConnectionString: appConfiguration.outputs.appConfigConnectionString
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
    logAnalyticsWorkspaceId: MyApp_LogAnalyticsWorkspace.outputs.logAnalyticsWorkspaceId
    jwtSecretKey: jwtSecretKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    frontendOrigin: frontendOrigin
    aspnetcoreEnvironment: aspnetcoreEnvironment
    managedIdentityPrincipalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    appConfigConnectionString: appConfiguration.outputs.appConfigConnectionString
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
    logAnalyticsWorkspaceId: MyApp_LogAnalyticsWorkspace.outputs.logAnalyticsWorkspaceId
    jwtSecretKey: jwtSecretKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    frontendOrigin: frontendOrigin
    aspnetcoreEnvironment: aspnetcoreEnvironment
    managedIdentityPrincipalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    appConfigConnectionString: appConfiguration.outputs.appConfigConnectionString
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
    logAnalyticsWorkspaceId: MyApp_LogAnalyticsWorkspace.outputs.logAnalyticsWorkspaceId
    jwtSecretKey: jwtSecretKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    frontendOrigin: frontendOrigin
    aspnetcoreEnvironment: aspnetcoreEnvironment
    managedIdentityPrincipalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    appConfigConnectionString: appConfiguration.outputs.appConfigConnectionString
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
    logAnalyticsWorkspaceId: MyApp_LogAnalyticsWorkspace.outputs.logAnalyticsWorkspaceId
    jwtSecretKey: jwtSecretKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    frontendOrigin: frontendOrigin
    aspnetcoreEnvironment: aspnetcoreEnvironment
    managedIdentityPrincipalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    appConfigConnectionString: appConfiguration.outputs.appConfigConnectionString
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
    logAnalyticsWorkspaceId: MyApp_LogAnalyticsWorkspace.outputs.logAnalyticsWorkspaceId
    jwtSecretKey: jwtSecretKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    frontendOrigin: frontendOrigin
    aspnetcoreEnvironment: aspnetcoreEnvironment
    managedIdentityPrincipalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    appConfigConnectionString: appConfiguration.outputs.appConfigConnectionString
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
    logAnalyticsWorkspaceId: MyApp_LogAnalyticsWorkspace.outputs.logAnalyticsWorkspaceId
    jwtSecretKey: jwtSecretKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    frontendOrigin: frontendOrigin
    aspnetcoreEnvironment: aspnetcoreEnvironment
    managedIdentityPrincipalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    appConfigConnectionString: appConfiguration.outputs.appConfigConnectionString
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
output MYAPP_APPLICATIONINSIGHTS_APPINSIGHTSCONNECTIONSTRING string = MyApp_ApplicationInsights.outputs.appInsightsConnectionString
output MYAPP_SQLSERVER_SQLSERVERFQDN string = myapp_sqlserver.outputs.sqlServerFqdn
output AZURE_REDIS_CACHE_NAME string = redis.outputs.name
output AZURE_REDIS_CACHE_HOST string = redis.outputs.hostName
output AZURE_REDIS_CACHE_PORT int = redis.outputs.sslPort
output AZURE_SQL_SERVER_NAME string = sqlServer.outputs.name
output AZURE_SQL_SERVER_FQDN string = sqlServer.outputs.fqdn
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

targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('Id of the principal to assign database and application roles')
param principalId string = ''

@description('SQL Server administrator password')
@secure()
param sqlAdminPassword string

@description('JWT Secret Key for authentication')
@secure()
param jwtSecretKey string

@description('Frontend origin URLs (semicolon-separated)')
param frontendOrigin string = 'https://localhost:3000'

@description('Enable provisioning of Key Vault and injecting secrets from Key Vault (set true for production)')
param enableKeyVault bool = false

// Tags that should be applied to all resources
var tags = {
  'azd-env-name': environmentName
  app: 'erp-microservices'
}

// Centralized derived secret values to avoid duplication across services
var redisConnection = '${redis.outputs.hostName}:6380,password=${redis.outputs.primaryKey},ssl=True,abortConnect=False'
var redisSecret = {
  name: 'redis-connection'
  value: redisConnection
}

// Organize resources in a resource group
resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

// Container Apps Environment with Dapr
module containerAppsEnvironment 'core/host/container-apps-environment.bicep' = {
  name: 'container-apps-environment'
  scope: rg
  params: {
    name: 'cae-${environmentName}'
    location: location
    tags: tags
    daprEnabled: true
    redisHostName: redis.outputs.hostName
    redisPrimaryKey: redis.outputs.primaryKey
  }
  dependsOn: [
    
  ]
}

// Azure SQL Server and Database
module sqlServer 'core/database/sql-server.bicep' = {
  name: 'sql-server'
  scope: rg
  params: {
    name: 'sql-${environmentName}'
    location: location
    tags: tags
    administratorLogin: 'sqladmin'
    administratorLoginPassword: sqlAdminPassword
    databases: [
      { name: 'AuthDB' }
      { name: 'BillingDB' }
      { name: 'InventoryDB' }
      { name: 'OrderDB' }
      { name: 'PurchasingDB' }
      { name: 'SalesDB' }
    ]
  }
}

// Azure Cache for Redis
module redis 'core/database/redis.bicep' = {
  name: 'redis'
  scope: rg
  params: {
    name: 'redis-${environmentName}'
    location: location
    tags: tags
  }
}

// Container Registry
module containerRegistry 'core/host/container-registry.bicep' = {
  name: 'container-registry'
  scope: rg
  params: {
    name: 'cr${replace(environmentName, '-', '')}'
    location: location
    tags: tags
  }
}

// Key Vault (only when enableKeyVault is true)
// Key Vault + secrets module (deployed into the resource group). This module creates Key Vault and
// the per-environment secrets only when `enableKeyVault` is true (intended for production).
module keyVaultModule 'core/security/keyvault-secrets.bicep' = {
  name: 'keyvault-secrets'
  scope: rg
  params: {
    name: 'kv-${environmentName}'
    location: location
    tags: tags
    redisHostName: redis.outputs.hostName
    redisPrimaryKey: redis.outputs.primaryKey
    sqlFqdn: sqlServer.outputs.fqdn
    sqlAdminPassword: sqlAdminPassword
    jwtSecretKey: jwtSecretKey
    enableKeyVault: enableKeyVault
  }
}

// Log Analytics Workspace
module logAnalytics 'core/monitor/log-analytics.bicep' = {
  name: 'log-analytics'
  scope: rg
  params: {
    name: 'log-${environmentName}'
    location: location
    tags: tags
  }
}

// Gateway Container App (External Ingress)
module gateway 'core/host/container-app.bicep' = {
  name: 'gateway'
  scope: rg
  params: {
    name: 'gateway'
    location: location
    tags: tags
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    containerRegistryName: containerRegistry.outputs.name
    imageName: 'gateway:latest'
    targetPort: 8080
    externalIngress: true
    daprEnabled: false
    env: [
      {
        name: 'FRONTEND_ORIGIN'
        value: frontendOrigin
      }
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: 'Production'
      }
    ]
  }
}

// Auth Service Container App
module authService 'core/host/container-app.bicep' = {
  name: 'auth-service'
  scope: rg
  params: {
    name: 'auth-service'
    location: location
    tags: tags
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    containerRegistryName: containerRegistry.outputs.name
    imageName: 'auth-service:latest'
    targetPort: 8080
    externalIngress: false
    daprEnabled: true
    daprAppId: 'auth-service'
    daprAppPort: 8080
    env: [
      {
        name: 'ConnectionStrings__AuthDb'
        secretRef: 'sql-connection-authdb'
      }
      {
        name: 'ConnectionStrings__cache'
        secretRef: 'redis-connection'
      }
      {
        name: 'Jwt__SecretKey'
        secretRef: 'jwt-secret-key'
      }
      // Key Vault runtime hints (URI and secret names). Empty when enableKeyVault == false.
      {
        name: 'KEYVAULT_URI'
        value: keyVaultModule.outputs.keyVaultUri
      }
      {
        name: 'REDIS_SECRET_NAME'
        value: keyVaultModule.outputs.redisSecretName
      }
      {
        name: 'SQL_SECRET_NAME'
        value: keyVaultModule.outputs.sqlAuthSecretName
      }
      {
        name: 'JWT_SECRET_NAME'
        value: keyVaultModule.outputs.jwtSecretName
      }
      {
        name: 'Jwt__Issuer'
        value: 'MyApp.Auth'
      }
      {
        name: 'Jwt__Audience'
        value: 'MyApp.All'
      }
      {
        name: 'FRONTEND_ORIGIN'
        value: frontendOrigin
      }
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: 'Production'
      }
    ]
    secrets: [
      {
        name: 'sql-connection-authdb'
        value: 'Server=${sqlServer.outputs.fqdn};Database=AuthDB;User Id=sqladmin;Password=${sqlAdminPassword};'
      }
      redisSecret
      {
        name: 'jwt-secret-key'
        value: jwtSecretKey
      }
    ]
  }
}

// Billing Service Container App
module billingService 'core/host/container-app.bicep' = {
  name: 'billing-service'
  scope: rg
  params: {
    name: 'billing-service'
    location: location
    tags: tags
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    containerRegistryName: containerRegistry.outputs.name
    imageName: 'billing-service:latest'
    targetPort: 8080
    externalIngress: false
    daprEnabled: true
    daprAppId: 'billing-service'
    daprAppPort: 8080
    env: [
      {
        name: 'ConnectionStrings__BillingDb'
        secretRef: 'sql-connection-billingdb'
      }
      {
        name: 'ConnectionStrings__cache'
        secretRef: 'redis-connection'
      }
      {
        name: 'Jwt__SecretKey'
        secretRef: 'jwt-secret-key'
      }
      {
        name: 'KEYVAULT_URI'
        value: keyVaultModule.outputs.keyVaultUri
      }
      {
        name: 'REDIS_SECRET_NAME'
        value: keyVaultModule.outputs.redisSecretName
      }
      {
        name: 'SQL_SECRET_NAME'
        value: keyVaultModule.outputs.sqlBillingSecretName
      }
      {
        name: 'JWT_SECRET_NAME'
        value: keyVaultModule.outputs.jwtSecretName
      }
      {
        name: 'Jwt__Issuer'
        value: 'MyApp.Auth'
      }
      {
        name: 'Jwt__Audience'
        value: 'MyApp.All'
      }
      {
        name: 'FRONTEND_ORIGIN'
        value: frontendOrigin
      }
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: 'Production'
      }
    ]
    secrets: [
      {
        name: 'sql-connection-billingdb'
        value: 'Server=${sqlServer.outputs.fqdn};Database=BillingDB;User Id=sqladmin;Password=${sqlAdminPassword};TrustServerCertificate=True;'
      }
      redisSecret
      {
        name: 'jwt-secret-key'
        value: jwtSecretKey
      }
    ]
  }
}

// Inventory Service Container App
module inventoryService 'core/host/container-app.bicep' = {
  name: 'inventory-service'
  scope: rg
  params: {
    name: 'inventory-service'
    location: location
    tags: tags
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    containerRegistryName: containerRegistry.outputs.name
    imageName: 'inventory-service:latest'
    targetPort: 8080
    externalIngress: false
    daprEnabled: true
    daprAppId: 'inventory-service'
    daprAppPort: 8080
    env: [
      {
        name: 'ConnectionStrings__InventoryDb'
        secretRef: 'sql-connection-inventorydb'
      }
      {
        name: 'ConnectionStrings__cache'
        secretRef: 'redis-connection'
      }
      {
        name: 'Jwt__SecretKey'
        secretRef: 'jwt-secret-key'
      }
      {
        name: 'KEYVAULT_URI'
        value: keyVaultModule.outputs.keyVaultUri
      }
      {
        name: 'REDIS_SECRET_NAME'
        value: keyVaultModule.outputs.redisSecretName
      }
      {
        name: 'SQL_SECRET_NAME'
        value: keyVaultModule.outputs.sqlInventorySecretName
      }
      {
        name: 'JWT_SECRET_NAME'
        value: keyVaultModule.outputs.jwtSecretName
      }
      {
        name: 'Jwt__Issuer'
        value: 'MyApp.Auth'
      }
      {
        name: 'Jwt__Audience'
        value: 'MyApp.All'
      }
      {
        name: 'FRONTEND_ORIGIN'
        value: frontendOrigin
      }
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: 'Production'
      }
    ]
    secrets: [
      {
        name: 'sql-connection-inventorydb'
        value: 'Server=${sqlServer.outputs.fqdn};Database=InventoryDB;User Id=sqladmin;Password=${sqlAdminPassword};TrustServerCertificate=True;'
      }
      redisSecret
      {
        name: 'jwt-secret-key'
        value: jwtSecretKey
      }
    ]
  }
}

// Orders Service Container App
module ordersService 'core/host/container-app.bicep' = {
  name: 'orders-service'
  scope: rg
  params: {
    name: 'orders-service'
    location: location
    tags: tags
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    containerRegistryName: containerRegistry.outputs.name
    imageName: 'orders-service:latest'
    targetPort: 8080
    externalIngress: false
    daprEnabled: true
    daprAppId: 'orders-service'
    daprAppPort: 8080
    env: [
      {
        name: 'ConnectionStrings__OrdersDb'
        secretRef: 'sql-connection-ordersdb'
      }
      {
        name: 'ConnectionStrings__cache'
        secretRef: 'redis-connection'
      }
      {
        name: 'Jwt__SecretKey'
        secretRef: 'jwt-secret-key'
      }
      {
        name: 'KEYVAULT_URI'
        value: keyVaultModule.outputs.keyVaultUri
      }
      {
        name: 'REDIS_SECRET_NAME'
        value: keyVaultModule.outputs.redisSecretName
      }
      {
        name: 'SQL_SECRET_NAME'
        value: keyVaultModule.outputs.sqlOrdersSecretName
      }
      {
        name: 'JWT_SECRET_NAME'
        value: keyVaultModule.outputs.jwtSecretName
      }
      {
        name: 'Jwt__Issuer'
        value: 'MyApp.Auth'
      }
      {
        name: 'Jwt__Audience'
        value: 'MyApp.All'
      }
      {
        name: 'FRONTEND_ORIGIN'
        value: frontendOrigin
      }
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: 'Production'
      }
    ]
    secrets: [
      {
        name: 'sql-connection-ordersdb'
        value: 'Server=${sqlServer.outputs.fqdn};Database=OrderDB;User Id=sqladmin;Password=${sqlAdminPassword};TrustServerCertificate=True;'
      }
      redisSecret
      {
        name: 'jwt-secret-key'
        value: jwtSecretKey
      }
    ]
  }
}

// Purchasing Service Container App
module purchasingService 'core/host/container-app.bicep' = {
  name: 'purchasing-service'
  scope: rg
  params: {
    name: 'purchasing-service'
    location: location
    tags: tags
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    containerRegistryName: containerRegistry.outputs.name
    imageName: 'purchasing-service:latest'
    targetPort: 8080
    externalIngress: false
    daprEnabled: true
    daprAppId: 'purchasing-service'
    daprAppPort: 8080
    env: [
      {
        name: 'ConnectionStrings__PurchasingDb'
        secretRef: 'sql-connection-purchasingdb'
      }
      {
        name: 'ConnectionStrings__cache'
        secretRef: 'redis-connection'
      }
      {
        name: 'Jwt__SecretKey'
        secretRef: 'jwt-secret-key'
      }
      {
        name: 'KEYVAULT_URI'
        value: keyVaultModule.outputs.keyVaultUri
      }
      {
        name: 'REDIS_SECRET_NAME'
        value: keyVaultModule.outputs.redisSecretName
      }
      {
        name: 'SQL_SECRET_NAME'
        value: keyVaultModule.outputs.sqlPurchasingSecretName
      }
      {
        name: 'JWT_SECRET_NAME'
        value: keyVaultModule.outputs.jwtSecretName
      }
      {
        name: 'Jwt__Issuer'
        value: 'MyApp.Auth'
      }
      {
        name: 'Jwt__Audience'
        value: 'MyApp.All'
      }
      {
        name: 'FRONTEND_ORIGIN'
        value: frontendOrigin
      }
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: 'Production'
      }
    ]
    secrets: [
      {
        name: 'sql-connection-purchasingdb'
        value: 'Server=${sqlServer.outputs.fqdn};Database=PurchasingDB;User Id=sqladmin;Password=${sqlAdminPassword};TrustServerCertificate=True;'
      }
      redisSecret
      {
        name: 'jwt-secret-key'
        value: jwtSecretKey
      }
    ]
  }
}

// Sales Service Container App
module salesService 'core/host/container-app.bicep' = {
  name: 'sales-service'
  scope: rg
  params: {
    name: 'sales-service'
    location: location
    tags: tags
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    containerRegistryName: containerRegistry.outputs.name
    imageName: 'sales-service:latest'
    targetPort: 8080
    externalIngress: false
    daprEnabled: true
    daprAppId: 'sales-service'
    daprAppPort: 8080
    env: [
      {
        name: 'ConnectionStrings__SalesDb'
        secretRef: 'sql-connection-salesdb'
      }
      {
        name: 'ConnectionStrings__cache'
        secretRef: 'redis-connection'
      }
      {
        name: 'Jwt__SecretKey'
        secretRef: 'jwt-secret-key'
      }
      {
        name: 'KEYVAULT_URI'
        value: keyVaultModule.outputs.keyVaultUri
      }
      {
        name: 'REDIS_SECRET_NAME'
        value: keyVaultModule.outputs.redisSecretName
      }
      {
        name: 'SQL_SECRET_NAME'
        value: keyVaultModule.outputs.sqlSalesSecretName
      }
      {
        name: 'JWT_SECRET_NAME'
        value: keyVaultModule.outputs.jwtSecretName
      }
      {
        name: 'Jwt__Issuer'
        value: 'MyApp.Auth'
      }
      {
        name: 'Jwt__Audience'
        value: 'MyApp.All'
      }
      {
        name: 'FRONTEND_ORIGIN'
        value: frontendOrigin
      }
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: 'Production'
      }
    ]
    secrets: [
      {
        name: 'sql-connection-salesdb'
        value: 'Server=${sqlServer.outputs.fqdn};Database=SalesDB;User Id=sqladmin;Password=${sqlAdminPassword};TrustServerCertificate=True;'
      }
      redisSecret
      {
        name: 'jwt-secret-key'
        value: jwtSecretKey
      }
    ]
  }
}

// Outputs
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = containerRegistry.outputs.loginServer
output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = containerAppsEnvironment.outputs.id
output GATEWAY_URL string = gateway.outputs.uri
output SQL_SERVER_FQDN string = sqlServer.outputs.fqdn
output REDIS_HOST_NAME string = redis.outputs.hostName

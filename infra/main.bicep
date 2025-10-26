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

// Tags that should be applied to all resources
var tags = {
  'azd-env-name': environmentName
  app: 'erp-microservices'
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
  }
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
        value: 'Server=${sqlServer.outputs.fqdn};Database=AuthDB;User Id=sqladmin;Password=${sqlAdminPassword};'
      }
      {
        name: 'ConnectionStrings__cache'
        value: '${redis.outputs.hostName}:6380,password=${redis.outputs.primaryKey},ssl=True,abortConnect=False'
      }
      {
        name: 'Jwt__SecretKey'
        secretRef: 'jwt-secret-key'
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
        value: 'Server=${sqlServer.outputs.fqdn};Database=BillingDB;User Id=sqladmin;Password=${sqlAdminPassword};TrustServerCertificate=True;'
      }
      {
        name: 'ConnectionStrings__cache'
        value: '${redis.outputs.hostName}:6380,password=${redis.outputs.primaryKey},ssl=True,abortConnect=False'
      }
      {
        name: 'Jwt__SecretKey'
        secretRef: 'jwt-secret-key'
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
        value: 'Server=${sqlServer.outputs.fqdn};Database=InventoryDB;User Id=sqladmin;Password=${sqlAdminPassword};TrustServerCertificate=True;'
      }
      {
        name: 'ConnectionStrings__cache'
        value: '${redis.outputs.hostName}:6380,password=${redis.outputs.primaryKey},ssl=True,abortConnect=False'
      }
      {
        name: 'Jwt__SecretKey'
        secretRef: 'jwt-secret-key'
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
        value: 'Server=${sqlServer.outputs.fqdn};Database=OrderDB;User Id=sqladmin;Password=${sqlAdminPassword};TrustServerCertificate=True;'
      }
      {
        name: 'ConnectionStrings__cache'
        value: '${redis.outputs.hostName}:6380,password=${redis.outputs.primaryKey},ssl=True,abortConnect=False'
      }
      {
        name: 'Jwt__SecretKey'
        secretRef: 'jwt-secret-key'
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
        value: 'Server=${sqlServer.outputs.fqdn};Database=PurchasingDB;User Id=sqladmin;Password=${sqlAdminPassword};TrustServerCertificate=True;'
      }
      {
        name: 'ConnectionStrings__cache'
        value: '${redis.outputs.hostName}:6380,password=${redis.outputs.primaryKey},ssl=True,abortConnect=False'
      }
      {
        name: 'Jwt__SecretKey'
        secretRef: 'jwt-secret-key'
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
        value: 'Server=${sqlServer.outputs.fqdn};Database=SalesDB;User Id=sqladmin;Password=${sqlAdminPassword};TrustServerCertificate=True;'
      }
      {
        name: 'ConnectionStrings__cache'
        value: '${redis.outputs.hostName}:6380,password=${redis.outputs.primaryKey},ssl=True,abortConnect=False'
      }
      {
        name: 'Jwt__SecretKey'
        secretRef: 'jwt-secret-key'
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

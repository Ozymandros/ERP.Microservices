targetScope = 'subscription'

import { azureRoleIdSqlDbContributor } from 'config/constants.bicep'

// ============================================================================
// Deployment Parameters - Basic Configuration
// ============================================================================

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention, the name of the resource group for your application will use this name, prefixed with rg-')
param environmentName string = 'dev' // 👈 Valor per defecte segur

@minLength(1)
@description('The location used for all deployed resources')
param location string = 'westeurope' // 👈 Valor per defecte segur

// ============================================================================
// Security Parameters - Secrets and Authentication
// ============================================================================

// Secrets generats amb valor per defecte per evitar el xoc amb el parser
@secure()
@description('Redis cache password for authentication (auto-generated if not provided)')
param cache_password string = base64(newGuid())

@secure()
@description('SQL Server administrator password (auto-generated if not provided)')
param password string = base64(newGuid())

@description('JWT secret key for token signing (HS256 requires 32 bytes/256 bits) - Base64-encoded secret key used for JWT token signing and validation across all microservices')
@secure()
param jwtSecretKey string = base64('${newGuid()}${newGuid()}')

@description('JWT token issuer (e.g., MyApp.Auth) - identifies who issued the token')
param jwtIssuer string = 'MyApp.Auth'

@description('JWT token audience (e.g., MyApp.All) - identifies intended recipients of the token')
param jwtAudience string = 'MyApp.All'

// ============================================================================
// Application Configuration Parameters
// ============================================================================

@description('Frontend origin for CORS (semicolon-separated for multiple origins) - allowed origins for cross-origin requests')
param frontendOrigin string = 'http://localhost:3000;http://localhost:5000'

@description('Environment name (Development, Staging, Production) - sets ASP.NET Core environment variable')
param aspnetcoreEnvironment string = 'Production'

@description('Docker image tag for all services (e.g., latest, commit hash, version) - used to version container images')
param imageTag string = 'latest'

// ============================================================================
// Derived Naming Variables
// ============================================================================
// Used to generate consistent resource names across the infrastructure

@description('Environment name slug (lowercase, hyphenated) - used in resource naming')
var envSlug = toLower(replace(environmentName, ' ', '-'))

@description('Base name prefix for all resources (e.g., myapp-dev) - ensures consistent naming')
var namePrefix = 'myapp-${envSlug}'

@description('Flat prefix without hyphens (e.g., myappdev) - used for resources with strict naming rules')
var flatPrefix = toLower(replace(namePrefix, '-', ''))

// ============================================================================
// Resource Naming Variables
// ============================================================================
// Azure resource names have specific length and character restrictions

@description('Resource group name following Azure naming convention (rg-{prefix}-core)')
var resourceGroupName = 'rg-${namePrefix}-core'

@description('Container Registry name (max 50 chars, alphanumeric only) - stores Docker images')
var containerRegistryName = take('${flatPrefix}containerregistry', 50)

@description('Log Analytics workspace name (max 63 chars) - centralizes logs from all services')
var logAnalyticsWorkspaceName = take('${namePrefix}-log-analytics-workspace', 63)

@description('Application Insights name (max 260 chars) - provides application performance monitoring')
var applicationInsightsName = take('${namePrefix}-application-insights', 260)

@description('Storage account name (max 24 chars, lowercase alphanumeric) - stores Azure Files shares')
var storageAccountName = take('${flatPrefix}storageaccount', 24)

@description('Azure Files share name (max 63 chars) - persistent storage for cache volumes')
var storageShareName = toLower(take('${namePrefix}-cache-fileshare', 63))

@description('Container Apps Environment storage name (max 32 chars) - volume mount for containers')
var containerEnvironmentStorageName = take('${flatPrefix}cachevolume', 32)

@description('Container Apps Environment name (max 63 chars) - hosts all containerized microservices')
var containerAppsEnvironmentName = take('${namePrefix}-container-apps-environment', 63)

@description('Redis cache name (max 63 chars) - provides distributed caching and pub/sub')
var redisCacheName = take('${namePrefix}-redis-cache', 63)

@description('SQL Server name (max 63 chars) - hosts all microservice databases')
var sqlServerName = take('${namePrefix}-sql-server', 63)

@description('SQL admin managed identity name (max 128 chars) - used for passwordless authentication')
var sqlAdminIdentityName = take('${namePrefix}-sql-admin-identity', 128)

@description('Key Vault name (max 24 chars, alphanumeric only) - stores secrets and certificates')
var keyVaultName = take('${flatPrefix}keyvault', 24)

@description('App Configuration name (max 50 chars) - centralized configuration store for all services')
var appConfigurationName = take('${namePrefix}-app-configuration', 50)

// ============================================================================
// Database Configuration
// ============================================================================

@description('SQL database names for each microservice (lowercase, hyphenated)')
var sqlDatabaseNames = {
  auth: toLower('${namePrefix}-auth-db')
  billing: toLower('${namePrefix}-billing-db')
  inventory: toLower('${namePrefix}-inventory-db')
  orders: toLower('${namePrefix}-orders-db')
  purchasing: toLower('${namePrefix}-purchasing-db')
  sales: toLower('${namePrefix}-sales-db')
}

@description('Array of all database names - used for batch database creation')
var sqlDatabaseList = [
  sqlDatabaseNames.auth
  sqlDatabaseNames.billing
  sqlDatabaseNames.inventory
  sqlDatabaseNames.orders
  sqlDatabaseNames.purchasing
  sqlDatabaseNames.sales
]

// ============================================================================
// Common Tags
// ============================================================================

@description('Common tags applied to all resources for organization and cost tracking')
var tags = {
  'azd-env-name': environmentName
}

// ============================================================================
// Resource Group Creation
// ============================================================================

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

// ============================================================================
// Core Infrastructure Resources
// ============================================================================

module resources 'resources.bicep' = {
  scope: rg
  name: 'resources'
  params: {
    location: location
    tags: tags
    namePrefix: namePrefix
    containerRegistryName: containerRegistryName
    logAnalyticsWorkspaceName: logAnalyticsWorkspaceName
    applicationInsightsName: applicationInsightsName
    storageAccountName: storageAccountName
    storageShareName: storageShareName
    containerEnvironmentStorageName: containerEnvironmentStorageName
    containerAppsEnvironmentName: containerAppsEnvironmentName
    redisHostName: redis.outputs.hostName
    redisPrimaryKey: redis.outputs.primaryKey
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
    userAssignedIdentityPrincipalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
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
    applicationInsightsConnectionString: resources.outputs.AZURE_APPLICATION_INSIGHTS_CONNECTION_STRING
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

// ============================================================================
// Database Resources
// ============================================================================

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
    roleGuid: azureRoleIdSqlDbContributor
  }
}

// ============================================================================
// Microservices Deployment
// ============================================================================

module authServiceModule 'services/auth-service.bicep' = {
  name: 'auth-service-deployment'
  scope: rg
  params: {
    location: location
    tags: tags
    containerAppsEnvironmentId: resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
    containerRegistryEndpoint: resources.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
    logAnalyticsWorkspaceId: resources.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_ID
    jwtSecretKey: jwtSecretKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    frontendOrigin: frontendOrigin
    aspnetcoreEnvironment: aspnetcoreEnvironment
    imageTag: imageTag
    managedIdentityPrincipalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    appConfigEndpoint: appConfiguration.outputs.appConfigEndpoint
    namePrefix: namePrefix
    userAssignedIdentityId: resources.outputs.AZURE_USER_ASSIGNED_IDENTITY_ID
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
    logAnalyticsWorkspaceId: resources.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_ID
    jwtSecretKey: jwtSecretKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    frontendOrigin: frontendOrigin
    aspnetcoreEnvironment: aspnetcoreEnvironment
    imageTag: imageTag
    managedIdentityPrincipalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    appConfigEndpoint: appConfiguration.outputs.appConfigEndpoint
    namePrefix: namePrefix
    userAssignedIdentityId: resources.outputs.AZURE_USER_ASSIGNED_IDENTITY_ID
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
    logAnalyticsWorkspaceId: resources.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_ID
    jwtSecretKey: jwtSecretKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    frontendOrigin: frontendOrigin
    aspnetcoreEnvironment: aspnetcoreEnvironment
    imageTag: imageTag
    managedIdentityPrincipalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    appConfigEndpoint: appConfiguration.outputs.appConfigEndpoint
    namePrefix: namePrefix
    userAssignedIdentityId: resources.outputs.AZURE_USER_ASSIGNED_IDENTITY_ID
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
    logAnalyticsWorkspaceId: resources.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_ID
    jwtSecretKey: jwtSecretKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    frontendOrigin: frontendOrigin
    aspnetcoreEnvironment: aspnetcoreEnvironment
    imageTag: imageTag
    managedIdentityPrincipalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    appConfigEndpoint: appConfiguration.outputs.appConfigEndpoint
    namePrefix: namePrefix
    userAssignedIdentityId: resources.outputs.AZURE_USER_ASSIGNED_IDENTITY_ID
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
    logAnalyticsWorkspaceId: resources.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_ID
    jwtSecretKey: jwtSecretKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    frontendOrigin: frontendOrigin
    aspnetcoreEnvironment: aspnetcoreEnvironment
    imageTag: imageTag
    managedIdentityPrincipalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    appConfigEndpoint: appConfiguration.outputs.appConfigEndpoint
    namePrefix: namePrefix
    userAssignedIdentityId: resources.outputs.AZURE_USER_ASSIGNED_IDENTITY_ID
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
    logAnalyticsWorkspaceId: resources.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_ID
    jwtSecretKey: jwtSecretKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    frontendOrigin: frontendOrigin
    aspnetcoreEnvironment: aspnetcoreEnvironment
    imageTag: imageTag
    managedIdentityPrincipalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    appConfigEndpoint: appConfiguration.outputs.appConfigEndpoint
    namePrefix: namePrefix
    userAssignedIdentityId: resources.outputs.AZURE_USER_ASSIGNED_IDENTITY_ID
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
    logAnalyticsWorkspaceId: resources.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_ID
    jwtSecretKey: jwtSecretKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    frontendOrigin: frontendOrigin
    aspnetcoreEnvironment: aspnetcoreEnvironment
    imageTag: imageTag
    managedIdentityPrincipalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    appConfigEndpoint: appConfiguration.outputs.appConfigEndpoint
    namePrefix: namePrefix
    userAssignedIdentityId: resources.outputs.AZURE_USER_ASSIGNED_IDENTITY_ID
  }
}

// ============================================================================
// Aspire Dashboard Deployment
// ============================================================================
// Deploys the official Microsoft Aspire Dashboard for observability
// Dashboard automatically discovers all services in the Container Apps Environment
// Access: Internal only (recommended for security) or External (if needed)

module aspireDashboardModule 'services/aspire-dashboard.bicep' = {
  name: 'aspire-dashboard-deployment'
  scope: rg
  params: {
    location: location
    tags: tags
    containerAppsEnvironmentId: resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
    aspnetcoreEnvironment: aspnetcoreEnvironment
    namePrefix: namePrefix
    externalIngress: false  // Set to true if you need external access (not recommended)
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

// ============================================================================
// RBAC Role Assignments (via dedicated module at resource group scope)
// NO manual input, NO hardcoded IDs - uses official Microsoft patterns
// ============================================================================
module rbacAssignments 'rbac-assignments.bicep' = {
  name: 'rbac-assignments'
  scope: rg
  params: {
    appConfigName: appConfigurationName
    keyVaultName: keyVaultName
    servicePrincipalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    appConfigPrincipalId: appConfiguration.outputs.appConfigPrincipalId
  }
}

output MANAGED_IDENTITY_CLIENT_ID string = resources.outputs.AZURE_USER_ASSIGNED_IDENTITY_CLIENT_ID
output MANAGED_IDENTITY_NAME string = resources.outputs.MANAGED_IDENTITY_NAME
output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = resources.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_NAME
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = resources.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = resources.outputs.AZURE_USER_ASSIGNED_IDENTITY_ID
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
output ASPIRE_DASHBOARD_FQDN string = aspireDashboardModule.outputs.fqdn
output ASPIRE_DASHBOARD_URI string = aspireDashboardModule.outputs.uri

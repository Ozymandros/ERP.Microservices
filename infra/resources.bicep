import { containerRegistrySku, logAnalyticsSkuName, applicationInsightsKind, workloadProfileType, workloadProfileName, aspireDashboardComponentName, aspireDashboardComponentType, storageFileServiceName, storageKind, storageSkuName, storageFileShareQuota, tagAspireResourceName, azureRoleIdAcrPull } from 'config/constants.bicep'

@description('Azure region where all resources will be deployed (defaults to resource group location)')
param location string = resourceGroup().location

@description('Resource tags applied to all resources for organization, cost tracking, and resource management')
param tags object = {}

@description('Base name prefix for all resources (e.g., myapp-dev) - ensures consistent naming across infrastructure')
param namePrefix string

@description('Azure Container Registry name - stores Docker images for all microservices')
param containerRegistryName string

@description('Log Analytics workspace name - centralizes logs, metrics, and diagnostics from all services')
param logAnalyticsWorkspaceName string

@description('Application Insights resource name - provides application performance monitoring (APM) and telemetry')
param applicationInsightsName string

@description('Azure Storage account name - provides Azure Files shares for persistent volumes in Container Apps')
param storageAccountName string

@description('Azure Files share name - persistent storage volume mounted to containers for cache/data persistence')
param storageShareName string

@description('Container Apps Environment storage volume name - reference name for the volume mount in containers')
param containerEnvironmentStorageName string

@description('Container Apps Environment name - hosts all containerized microservices in a managed environment')
param containerAppsEnvironmentName string

@description('Redis cache hostname (FQDN) - used by Dapr components for state store and pub/sub messaging (optional)')
param redisHostName string = ''

@description('Redis cache primary access key - used for authentication by Dapr components (optional, required if Dapr enabled)')
@secure()
param redisPrimaryKey string = ''

@description('User-Assigned Managed Identity name - shared identity used by all services for Azure service authentication')
var managedIdentityName = take('${namePrefix}-user-assigned-identity', 128)

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: managedIdentityName
  location: location
  tags: tags
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: containerRegistryName
  location: location
  sku: {
    name: containerRegistrySku
  }
  properties: {
    adminUserEnabled: false
  }
  tags: tags
}

// Grant the User-Assigned Identity the AcrPull role on the Container Registry
resource acrPullRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(containerRegistry.id, managedIdentity.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', azureRoleIdAcrPull))
  scope: containerRegistry
  properties: {
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId:  subscriptionResourceId('Microsoft.Authorization/roleDefinitions', azureRoleIdAcrPull)
  }
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  properties: {
    sku: {
      name: logAnalyticsSkuName
    }
  }
  tags: union(tags, {
    '${tagAspireResourceName}': logAnalyticsWorkspaceName
  })
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  kind: applicationInsightsKind
  properties: {
    Application_Type: applicationInsightsKind
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
  tags: union(tags, {
    '${tagAspireResourceName}': applicationInsightsName
  })
}

resource storageVolume 'Microsoft.Storage/storageAccounts@2022-05-01' = {
  name: storageAccountName
  location: location
  kind: storageKind
  sku: {
    name: storageSkuName
  }
  properties: {
    largeFileSharesState: 'Enabled'
  }
}

resource storageVolumeFileService 'Microsoft.Storage/storageAccounts/fileServices@2022-05-01' = {
  parent: storageVolume
  name: storageFileServiceName
}

resource cacheRedisCacheFileShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2022-05-01' = {
  parent: storageVolumeFileService
  name: storageShareName
  properties: {
    shareQuota: storageFileShareQuota
    enabledProtocols: 'SMB'
  }
}

module logAnalyticsKeys 'shared/get-loganalytics-key.bicep' = {
  name: 'logAnalyticsWorkspaceKeys'
  params: {
    workspaceName: logAnalyticsWorkspace.name
  }
}

module storageAccountKeys 'shared/get-storageaccount-key.bicep' = {
  name: 'storageAccountKeys'
  params: {
    storageAccountName: storageVolume.name
  }
}

module containerAppsEnvironment 'core/host/container-apps-environment.bicep' = {
  name: 'container-apps-environment'
  params: {
    name: containerAppsEnvironmentName
    location: location
    tags: tags
    daprEnabled: true
    logAnalyticsWorkspaceId: logAnalyticsWorkspace.id
    redisHostName: redisHostName
    redisPrimaryKey: redisPrimaryKey
  }
}

// Reference the Container Apps Environment created by the module
resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
  name: containerAppsEnvironmentName
}

resource cacheRedisCacheStore 'Microsoft.App/managedEnvironments/storages@2023-05-01' = {
  parent: containerAppEnvironment
  name: containerEnvironmentStorageName
  properties: {
    azureFile: {
      shareName: cacheRedisCacheFileShare.name
      accountName: storageVolume.name
      accountKey: storageAccountKeys.outputs.primaryKey
      accessMode: 'ReadWrite'
    }
  }
}

output MANAGED_IDENTITY_CLIENT_ID string = managedIdentity.properties.clientId
output MANAGED_IDENTITY_NAME string = managedIdentity.name
output MANAGED_IDENTITY_PRINCIPAL_ID string = managedIdentity.properties.principalId
output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = logAnalyticsWorkspace.name
output AZURE_LOG_ANALYTICS_WORKSPACE_ID string = logAnalyticsWorkspace.id
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = containerRegistry.properties.loginServer
output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = managedIdentity.id
output AZURE_CONTAINER_REGISTRY_NAME string = containerRegistry.name
output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = containerAppsEnvironment.outputs.name
output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = containerAppEnvironment.id
output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = containerAppEnvironment.properties.defaultDomain
output SERVICE_CACHE_VOLUME_REDISCACHE_NAME string = cacheRedisCacheStore.name
output AZURE_VOLUMES_STORAGE_ACCOUNT string = storageVolume.name
output AZURE_APPLICATION_INSIGHTS_ID string = applicationInsights.id
output AZURE_APPLICATION_INSIGHTS_NAME string = applicationInsights.name
output AZURE_APPLICATION_INSIGHTS_CONNECTION_STRING string = applicationInsights.properties.ConnectionString
output AZURE_APPLICATION_INSIGHTS_INSTRUMENTATION_KEY string = applicationInsights.properties.InstrumentationKey
output AZURE_USER_ASSIGNED_IDENTITY_PRINCIPAL_ID string = managedIdentity.properties.principalId
output AZURE_USER_ASSIGNED_IDENTITY_CLIENT_ID string = managedIdentity.properties.clientId
output AZURE_USER_ASSIGNED_IDENTITY_ID string = managedIdentity.id
output AZURE_LOCATION string = location

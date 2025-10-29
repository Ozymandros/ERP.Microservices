import { containerRegistrySku, logAnalyticsSkuName, applicationInsightsKind, workloadProfileType, workloadProfileName, aspireDashboardComponentName, aspireDashboardComponentType, storageFileServiceName, storageKind, storageSkuName, storageFileShareQuota, tagAspireNamePrefix, tagAspireResourceName } from 'config/constants.bicep'

@description('The location used for all deployed resources')
param location string = resourceGroup().location

@description('Tags that will be applied to all resources')
param tags object = {}

@description('Base name prefix to apply to all resources (e.g., myapp-dev)')
param namePrefix string

@description('User-assigned managed identity name')
param managedIdentityName string

@description('Container registry name')
param containerRegistryName string

@description('Log Analytics workspace name')
param logAnalyticsWorkspaceName string

@description('Application Insights resource name')
param applicationInsightsName string

@description('Storage account name for shared volumes')
param storageAccountName string

@description('Azure Files share name for cache persistence')
param storageShareName string

@description('Managed environment storage name for cache volumes')
param containerEnvironmentStorageName string

@description('Container Apps Environment name')
param containerAppsEnvironmentName string

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: managedIdentityName
  location: location
  tags: union(tags, {
    '${tagAspireNamePrefix}': namePrefix
  })
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: containerRegistryName
  location: location
  sku: {
    name: containerRegistrySku
  }
  tags: tags
}

resource caeMiRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(containerRegistry.id, managedIdentity.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d'))
  scope: containerRegistry
  properties: {
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId:  subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
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

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2024-02-02-preview' = {
  name: containerAppsEnvironmentName
  location: location
  properties: {
    workloadProfiles: [{
      workloadProfileType: workloadProfileType
      name: workloadProfileName
    }]
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsKeys.outputs.primarySharedKey
      }
    }
  }
  tags: tags

  resource aspireDashboard 'dotNetComponents' = {
    name: aspireDashboardComponentName
    properties: {
      componentType: aspireDashboardComponentType
    }
  }

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
output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = containerAppEnvironment.name
output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = containerAppEnvironment.id
output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = containerAppEnvironment.properties.defaultDomain
output SERVICE_CACHE_VOLUME_REDISCACHE_NAME string = cacheRedisCacheStore.name
output AZURE_VOLUMES_STORAGE_ACCOUNT string = storageVolume.name
output AZURE_APPLICATION_INSIGHTS_ID string = applicationInsights.id
output AZURE_APPLICATION_INSIGHTS_NAME string = applicationInsights.name
output AZURE_APPLICATION_INSIGHTS_CONNECTION_STRING string = applicationInsights.properties.ConnectionString
output AZURE_APPLICATION_INSIGHTS_INSTRUMENTATION_KEY string = applicationInsights.properties.InstrumentationKey

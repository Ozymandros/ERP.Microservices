import { containerRegistrySku, logAnalyticsSkuName, applicationInsightsKind, workloadProfileType, workloadProfileName, aspireDashboardComponentName, aspireDashboardComponentType, tagAspireResourceName, azureRoleIdAcrPull, finopsLogAnalyticsRetentionDays, finopsApplicationInsightsSamplingPercentage } from 'config/constants.bicep'

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

@description('Container Apps Environment name - hosts all containerized microservices in a managed environment')
param containerAppsEnvironmentName string

@description('Redis cache hostname (FQDN) - used by Dapr components for state store and pub/sub messaging (optional)')
param redisHostName string = ''

@description('Redis cache primary access key - used for authentication by Dapr components (optional, required if Dapr enabled)')
@secure()
param redisPrimaryKey string = ''

@description('User-Assigned Managed Identity name - shared identity used by all services for Azure service authentication')
var managedIdentityName = take('${namePrefix}-user-assigned-identity', 128)

// ============================================================================
// Core Resources
// ============================================================================

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

// ============================================================================
// RBAC Role Assignments
// ============================================================================

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

// ============================================================================
// Monitoring Resources
// ============================================================================

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  properties: {
    sku: {
      name: logAnalyticsSkuName  // OPTIMITZACIÓ FINOPS: Pay-as-you-go, mai amb reserves de capacitat
    }
    retentionInDays: finopsLogAnalyticsRetentionDays  // OPTIMITZACIÓ FINOPS: Mínim possible
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
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
    SamplingPercentage: finopsApplicationInsightsSamplingPercentage  // OPTIMITZACIÓ FINOPS: Sampling per reduir costos de telemetria
  }
  tags: union(tags, {
    '${tagAspireResourceName}': applicationInsightsName
  })
}

// ============================================================================
// Storage Resources - REMOVED FOR FINOPS
// ============================================================================
// FINOPS OPTIMIZATION: Storage Account and File Share removed as they are not used
// by any container apps. This eliminates write operation costs (~€8.80+/month).
// If needed in the future, add back with volumes mounted to containers.

// ============================================================================
// Helper Modules for Keys and Secrets
// ============================================================================

module logAnalyticsKeys 'shared/get-loganalytics-key.bicep' = {
  name: 'logAnalyticsWorkspaceKeys'
  params: {
    workspaceName: logAnalyticsWorkspace.name
  }
}

// ============================================================================
// Container Apps Environment
// ============================================================================

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

// ============================================================================
// Container Apps Environment Storage - REMOVED FOR FINOPS
// ============================================================================
// FINOPS OPTIMIZATION: Environment storage removed as no containers mount it.
// Redis is used via Dapr for state/cache instead of file shares.

// ============================================================================
// Outputs
// ============================================================================

output MANAGED_IDENTITY_CLIENT_ID string = managedIdentity.properties.clientId
output MANAGED_IDENTITY_NAME string = managedIdentity.name
output MANAGED_IDENTITY_PRINCIPAL_ID string = managedIdentity.properties.principalId
output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = logAnalyticsWorkspace.name
output AZURE_LOG_ANALYTICS_WORKSPACE_ID string = logAnalyticsWorkspace.id
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = containerRegistry.properties.loginServer
output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = managedIdentity.id
output AZURE_CONTAINER_REGISTRY_NAME string = containerRegistry.name
output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = containerAppsEnvironment.outputs.name
output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = containerAppsEnvironment.outputs.id
output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = containerAppsEnvironment.outputs.domain
output AZURE_APPLICATION_INSIGHTS_ID string = applicationInsights.id
output AZURE_APPLICATION_INSIGHTS_NAME string = applicationInsights.name
output AZURE_APPLICATION_INSIGHTS_CONNECTION_STRING string = applicationInsights.properties.ConnectionString
output AZURE_APPLICATION_INSIGHTS_INSTRUMENTATION_KEY string = applicationInsights.properties.InstrumentationKey
output AZURE_USER_ASSIGNED_IDENTITY_PRINCIPAL_ID string = managedIdentity.properties.principalId
output AZURE_USER_ASSIGNED_IDENTITY_CLIENT_ID string = managedIdentity.properties.clientId
output AZURE_USER_ASSIGNED_IDENTITY_ID string = managedIdentity.id
output AZURE_LOCATION string = location

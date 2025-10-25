@description('Name of the Container Apps Environment')
param name string

@description('Location for the Container Apps Environment')
param location string = resourceGroup().location

@description('Tags to apply to the Container Apps Environment')
param tags object = {}

@description('Enable Dapr on the Container Apps Environment')
param daprEnabled bool = true

@description('Log Analytics Workspace ID for diagnostics')
param logAnalyticsWorkspaceId string = ''

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    daprAIConnectionString: daprEnabled ? '' : null
    appLogsConfiguration: !empty(logAnalyticsWorkspaceId) ? {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: reference(logAnalyticsWorkspaceId, '2021-06-01').customerId
        sharedKey: listKeys(logAnalyticsWorkspaceId, '2021-06-01').primarySharedKey
      }
    } : null
  }
}

// Dapr Component for Redis State Store
resource daprStateStore 'Microsoft.App/managedEnvironments/daprComponents@2023-05-01' = if (daprEnabled) {
  name: 'statestore'
  parent: containerAppsEnvironment
  properties: {
    componentType: 'state.redis'
    version: 'v1'
    ignoreErrors: false
    initTimeout: '60s'
    secrets: []
    metadata: [
      {
        name: 'redisHost'
        value: 'redis-cache:6379'
      }
      {
        name: 'redisPassword'
        value: ''
      }
      {
        name: 'actorStateStore'
        value: 'false'
      }
    ]
    scopes: [
      'auth-service'
      'billing-service'
      'inventory-service'
      'orders-service'
      'purchasing-service'
      'sales-service'
    ]
  }
}

output id string = containerAppsEnvironment.id
output name string = containerAppsEnvironment.name
output domain string = containerAppsEnvironment.properties.defaultDomain

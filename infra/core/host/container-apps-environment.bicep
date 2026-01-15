import { workloadProfileType } from '../../config/constants.bicep'

// ============================================================================
// Basic Parameters
// ============================================================================

@description('Name of the Container Apps Environment - must be unique within the subscription')
param name string

@description('Azure region where the Container Apps Environment will be deployed (defaults to resource group location)')
param location string = resourceGroup().location

@description('Resource tags for organization, cost tracking, and resource management')
param tags object = {}

// ============================================================================
// Dapr Configuration
// ============================================================================

@description('Enable Dapr runtime on the Container Apps Environment - if true, Dapr sidecars can be injected into apps')
param daprEnabled bool = true

// ============================================================================
// Monitoring Configuration
// ============================================================================

@description('Log Analytics Workspace resource ID - all container logs and diagnostics are sent here')
param logAnalyticsWorkspaceId string

// ============================================================================
// Redis Configuration (for Dapr Components)
// ============================================================================

@description('Redis cache hostname (FQDN) - used by Dapr components for state store and pub/sub (optional if Dapr disabled)')
param redisHostName string = ''

@description('Redis cache primary access key - used for authentication by Dapr components (optional if Dapr disabled)')
@secure()
param redisPrimaryKey string = ''

// ============================================================================
// Container Apps Environment Resource
// ============================================================================

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    workloadProfiles: [
      {
        name: toLower(workloadProfileType)
        workloadProfileType: workloadProfileType
      }
    ]
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: reference(logAnalyticsWorkspaceId, '2021-06-01').customerId
        sharedKey: listKeys(logAnalyticsWorkspaceId, '2021-06-01').primarySharedKey
      }
    }
  }
}

// ============================================================================
// Outputs
// ============================================================================

output id string = containerAppsEnvironment.id
output name string = containerAppsEnvironment.name
output domain string = containerAppsEnvironment.properties.defaultDomain

// ============================================================================
// Dapr Components
// ============================================================================

// Dapr Component for Redis State Store
resource daprStateStore 'Microsoft.App/managedEnvironments/daprComponents@2024-03-01' = if (daprEnabled && !empty(redisHostName) && !empty(redisPrimaryKey)) {
  name: 'statestore'
  parent: containerAppsEnvironment
  properties: {
    componentType: 'state.redis'
    version: 'v1'
    ignoreErrors: false
    initTimeout: '60s'
    secrets: [
      {
        name: 'redis-password'
        value: redisPrimaryKey
      }
    ]
    metadata: [
      {
        name: 'redisHost'
        value: '${redisHostName}:6380'
      }
      {
        name: 'redisPassword'
        secretRef: 'redis-password'
      }
      {
        name: 'enableTLS'
        value: 'true'
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

// Dapr Component for Redis Pub/Sub
resource daprPubSub 'Microsoft.App/managedEnvironments/daprComponents@2024-03-01' = if (daprEnabled && !empty(redisHostName) && !empty(redisPrimaryKey)) {
  name: 'pubsub'
  parent: containerAppsEnvironment
  properties: {
    componentType: 'pubsub.redis'
    version: 'v1'
    ignoreErrors: false
    initTimeout: '60s'
    secrets: [
      {
        name: 'redis-password'
        value: redisPrimaryKey
      }
    ]
    metadata: [
      {
        name: 'redisHost'
        value: '${redisHostName}:6380'
      }
      {
        name: 'redisPassword'
        secretRef: 'redis-password'
      }
      {
        name: 'enableTLS'
        value: 'true'
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

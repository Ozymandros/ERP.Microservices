@description('Name of the Container App')
param name string

@description('Location for the Container App')
param location string = resourceGroup().location

@description('Tags to apply to the Container App')
param tags object = {}

@description('Name of the Container Apps Environment')
param containerAppsEnvironmentName string

@description('Name of the Container Registry')
param containerRegistryName string

@description('Container image name')
param imageName string

@description('Target port for the container')
param targetPort int = 8080

@description('Enable external ingress')
param externalIngress bool = false

@description('Enable Dapr sidecar')
param daprEnabled bool = false

@description('Dapr app ID')
param daprAppId string = name

@description('Dapr app port')
param daprAppPort int = targetPort

// Removed unused parameters for clean build

@description('Environment variables')
param env array = []

@description('Secrets for the container app')
param secrets array = []

@description('Minimum number of replicas')
param minReplicas int = 1

@description('Maximum number of replicas')
param maxReplicas int = 10

@description('CPU cores (in cores)')
param cpu string = '0.5'

@description('Memory (in Gi)')
param memory string = '1.0Gi'

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' existing = {
  name: containerAppsEnvironmentName
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' existing = {
  name: containerRegistryName
}

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: name
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      ingress: {
        external: externalIngress
        targetPort: targetPort
        transport: 'auto'
        allowInsecure: false
      }
      registries: [
        {
          server: containerRegistry.properties.loginServer
          identity: 'system-assigned'
        }
      ]
      dapr: daprEnabled ? {
        enabled: true
        appId: daprAppId
        appPort: daprAppPort
        appProtocol: 'http'
        enableApiLogging: true
      } : null
      secrets: secrets
    }
    template: {
      containers: [
        {
          name: name
          image: '${containerRegistry.properties.loginServer}/${imageName}'
          resources: {
            cpu: any(cpu)
            memory: memory
          }
          env: env
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: targetPort
                scheme: 'HTTP'
              }
              initialDelaySeconds: 30
              periodSeconds: 30
              timeoutSeconds: 5
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health'
                port: targetPort
                scheme: 'HTTP'
              }
              initialDelaySeconds: 10
              periodSeconds: 10
              timeoutSeconds: 3
              failureThreshold: 3
            }
          ]
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
        rules: [
          {
            name: 'http-rule'
            http: {
              metadata: {
                concurrentRequests: '100'
              }
            }
          }
        ]
      }
    }
  }
}

// Assign ACR Pull role to the container app
resource acrPullRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(containerApp.id, containerRegistry.id, 'acrpull')
  scope: containerRegistry
  properties: {
    principalId: containerApp.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
  }
}

output id string = containerApp.id
output name string = containerApp.name
output uri string = externalIngress ? 'https://${containerApp.properties.configuration.ingress.fqdn}' : ''
output fqdn string = containerApp.properties.configuration.ingress.fqdn

@description('Location for resources')
param location string = resourceGroup().location

@description('Tags to apply')
param tags object = {}

@description('Container Apps Environment ID')
param containerAppsEnvironmentId string

@description('ASP.NET Core environment')
param aspnetcoreEnvironment string = 'Production'

@description('Base resource name prefix for this deployment (e.g., myapp-dev)')
param namePrefix string

@description('Enable external ingress (false = internal only, recommended for security)')
param externalIngress bool = false

var serviceName = '${namePrefix}-aspire-dashboard'
// Use official Microsoft Aspire Dashboard image (public, no ACR needed)
var dashboardImage = 'mcr.microsoft.com/dotnet/aspire-dashboard:latest'
var targetPort = 18888  // Default Aspire Dashboard port

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
  name: split(containerAppsEnvironmentId, '/')[8]
}

// ============================================================================
// Aspire Dashboard Container App
// ============================================================================
// Note: Uses public Microsoft Container Registry image, no ACR authentication needed

resource aspireDashboard 'Microsoft.App/containerApps@2024-03-01' = {
  name: serviceName
  location: location
  tags: union(
    tags,
    {
      component: 'aspire-dashboard'
      purpose: 'observability'
    }
  )
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    workloadProfileName: 'consumption'
    configuration: {
      ingress: externalIngress
        ? {
            external: true
            targetPort: targetPort
            transport: 'auto'
            allowInsecure: false
            traffic: [
              {
                weight: 100
                latestRevision: true
              }
            ]
          }
        : {
            external: false
            targetPort: targetPort
            transport: 'auto'
            allowInsecure: false
          }
      // No registry needed - using public Microsoft image
      registries: []
      dapr: {
        enabled: false
      }
      secrets: []
    }
    template: {
      containers: [
        {
          name: 'aspire-dashboard'
          image: dashboardImage
          resources: {
            cpu: json('0.5')
            memory: '1.0Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: aspnetcoreEnvironment
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:${targetPort}'
            }
            // Aspire Dashboard automatically discovers services in the Container Apps Environment
            // No additional configuration needed
          ]
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
        minReplicas: 1
        maxReplicas: 2  // Dashboard doesn't need high scaling
        rules: [
          {
            name: 'http-rule'
            http: {
              metadata: {
                concurrentRequests: '50'
              }
            }
          }
        ]
      }
    }
  }
}

output id string = aspireDashboard.id
output name string = aspireDashboard.name
output uri string = externalIngress ? 'https://${aspireDashboard.properties.configuration.ingress.fqdn}' : ''
output fqdn string = aspireDashboard.properties.configuration.ingress.fqdn

@description('Name of the Container App')
param name string

@description('Location for the Container App')
param location string = resourceGroup().location

@description('Tags to apply to the Container App')
param tags object = {}

@description('Container Apps Environment ID')
param containerAppsEnvironmentId string

@description('Container Registry endpoint')
param containerRegistryEndpoint string

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

@description('Minimum number of replicas')
param minReplicas int = 1

@description('Maximum number of replicas')
param maxReplicas int = 10

@description('CPU cores')
param cpu string = '0.5'

@description('Memory in Gi')
param memory string = '1.0Gi'

@description('JWT secret key')
@secure()
param jwtSecretKey string = ''

@description('JWT issuer')
param jwtIssuer string = 'MyApp.Auth'

@description('JWT audience')
param jwtAudience string = 'MyApp.All'

@description('Frontend origin for CORS')
param frontendOrigin string = 'http://localhost:3000'

@description('ASP.NET Core environment')
param aspnetcoreEnvironment string = 'Production'

@description('App Configuration endpoint')
param appConfigEndpoint string = ''

@description('Log Analytics Workspace ID for diagnostics and monitoring')
param logAnalyticsWorkspaceId string

@description('Managed Identity Principal ID for RBAC role assignments')
param managedIdentityPrincipalId string

@description('User-Assigned Managed Identity ID')
param userAssignedIdentityId string

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' existing = {
  name: split(containerAppsEnvironmentId, '/')[8]
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' existing = {
  name: replace(containerRegistryEndpoint, '.azurecr.io', '')
}



// Build environment variables
var environmentVariables = concat(
  [
    {
      name: 'ASPNETCORE_ENVIRONMENT'
      value: aspnetcoreEnvironment
    }
    {
      name: 'ASPNETCORE_URLS'
      value: 'http://+:${targetPort}'
    }
    {
      name: 'Jwt__Issuer'
      value: jwtIssuer
    }
    {
      name: 'Jwt__Audience'
      value: jwtAudience
    }
    {
      name: 'FRONTEND_ORIGIN'
      value: frontendOrigin
    }
  ],
  !empty(appConfigEndpoint)
    ? [
        {
          name: 'AppConfiguration__Endpoint'
          value: appConfigEndpoint
        }
      ]
    : [],
  !empty(jwtSecretKey)
    ? [
        {
          name: 'Jwt__SecretKey'
          secretRef: 'jwt-secret'
        }
      ]
    : []
)

// Build secrets array from parameters and Key Vault references
// Note: Application Insights Connection String is now centralized in App Configuration
// Services read it via App Configuration Provider, which resolves Key Vault reference
var secrets = !empty(jwtSecretKey)
  ? [
      {
        name: 'jwt-secret'
        value: jwtSecretKey
      }
    ]
  : []

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: name
  location: location
  tags: union(
    union(
      tags,
      !empty(logAnalyticsWorkspaceId)
        ? {
            'log-analytics-workspace-id': logAnalyticsWorkspaceId
          }
        : {}
    ),
    !empty(managedIdentityPrincipalId)
      ? {
          'managed-identity-principal-id': managedIdentityPrincipalId
        }
      : {}
  )
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentityId}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    workloadProfileName: 'Consumption'
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
      registries: [
        {
          server: containerRegistry.properties.loginServer
          identity: userAssignedIdentityId
        }
      ]
      dapr: daprEnabled
        ? {
            enabled: true
            appId: daprAppId
            appPort: daprAppPort
            appProtocol: 'http'
            enableApiLogging: true
          }
        : null
      secrets: secrets
    }
    template: {
      containers: [
        {
          name: name
          image: '${containerRegistry.properties.loginServer}/${imageName}'
          resources: {
            cpu: json(cpu)
            memory: memory
          }
          env: environmentVariables
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
/*
resource acrPullRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(containerApp.id, containerRegistry.id, 'acrpull')
  scope: containerRegistry
  properties: {
    principalId: containerApp.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
  }
}
*/

output id string = containerApp.id
output name string = containerApp.name
output uri string = externalIngress ? 'https://${containerApp.properties.configuration.ingress.fqdn}' : ''
@description('Managed Identity Principal ID for RBAC role assignments (Key Vault, App Config, SQL, Redis)')
output managedIdentityPrincipalId string = containerApp.identity.principalId
output fqdn string = containerApp.properties.configuration.ingress.fqdn

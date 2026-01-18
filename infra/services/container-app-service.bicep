// Registry type is now always GHCR; ACR logic removed
import { workloadProfileName } from '../config/constants.bicep'

// ============================================================================
// Basic Container App Parameters
// ============================================================================

@description('Name of the Container App - must be unique within the Container Apps Environment')
param name string

@description('Azure region where the Container App will be deployed (defaults to resource group location)')
param location string = resourceGroup().location

@description('Resource tags for organization, cost tracking, and resource management')
param tags object = {}

// ============================================================================
// Infrastructure References
// ============================================================================

@description('Full resource ID of the Container Apps Environment where this app will run')
param containerAppsEnvironmentId string


@description('Container Registry endpoint URL (e.g., ghcr.io or myregistry.azurecr.io) - where container images are stored')
param containerRegistryEndpoint string

@description('Container Registry username (for GHCR, use GitHub username)')
param ghcrUsername string = ''

@description('Container Registry Personal Access Token (for GHCR, use GitHub PAT or GITHUB_TOKEN)')
@secure()
param ghcrPat string = ''

// ACR identity parameter removed; only GHCR supported

@description('Container image name with optional tag (e.g., auth-service:latest or auth-service:abc1234)')
param imageName string

// ============================================================================
// Container Configuration
// ============================================================================

@description('TCP port number that the container application listens on (default: 8080)')
param targetPort int = 8080

@description('Enable external ingress - if true, app is accessible from internet; if false, only internal access')
param externalIngress bool = false

@description('Minimum number of container replicas - ensures high availability (default: 1)')
param minReplicas int = 1

@description('Maximum number of container replicas - limits scaling to control costs (default: 10)')
param maxReplicas int = 10

@description('CPU allocation per replica in cores (e.g., 0.5 = 500m, 1.0 = 1 core) - affects performance')
param cpu int = 1

@description('Memory allocation per replica in GiB (e.g., 1.0Gi = 1024 MiB) - affects performance')
param memory string = '1.0Gi'

// ============================================================================
// Dapr Configuration
// ============================================================================

@description('Enable Dapr sidecar - if true, Dapr runtime is injected for service-to-service communication')
param daprEnabled bool = false

@description('Dapr application ID - unique identifier for this service in the Dapr service mesh (defaults to app name)')
param daprAppId string = name

@description('Port number that Dapr sidecar uses to communicate with the application (defaults to targetPort)')
param daprAppPort int = targetPort

// ============================================================================
// Application Configuration
// ============================================================================

@description('JWT secret key for token signing and validation - stored as secret, read from App Configuration/Key Vault')
@secure()
param jwtSecretKey string = ''

@description('JWT token issuer claim - identifies who issued the token (e.g., MyApp.Auth service)')
param jwtIssuer string = 'MyApp.Auth'

@description('JWT token audience claim - identifies intended recipients (e.g., MyApp.All for all services)')
param jwtAudience string = 'MyApp.All'

@description('Frontend origin URL for CORS policy - allowed origin for cross-origin requests')
param frontendOrigin string = 'http://localhost:3000'

@description('ASP.NET Core environment name - sets ASPNETCORE_ENVIRONMENT variable (Development, Staging, Production)')
param aspnetcoreEnvironment string = 'Production'

@description('Azure App Configuration endpoint URL - services read configuration from here (centralized config)')
param appConfigEndpoint string = ''

// ============================================================================
// Monitoring and Identity
// ============================================================================

@description('Log Analytics Workspace resource ID - all container logs are sent here for centralized monitoring')
param logAnalyticsWorkspaceId string

@description('User-Assigned Managed Identity resource ID - used for authentication to Azure services (ACR, Key Vault, etc.)')
param userAssignedIdentityId string

@description('Managed Identity Principal ID - used for RBAC role assignments (Key Vault, App Config, SQL, Redis)')
param managedIdentityPrincipalId string

@description('azd service name - used by azd deploy to map services from azure.yaml to infrastructure resources')
param azdServiceName string = ''

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
  name: split(containerAppsEnvironmentId, '/')[8]
}



// ============================================================================
// Environment Variables Configuration
// ============================================================================

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

// ============================================================================
// Secrets Configuration
// ============================================================================
// Note: Application Insights Connection String is now centralized in App Configuration
// Services read it via App Configuration Provider, which resolves Key Vault reference

var secrets = union(
  !empty(jwtSecretKey)
    ? [
        {
          name: 'jwt-secret'
          value: jwtSecretKey
        }
      ]
    : [],
  !empty(ghcrPat)
    ? [
        {
          name: 'ghcr-pat'
          value: ghcrPat
        }
      ]
    : []
)

// ============================================================================
// Container App Resource
// ============================================================================

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: name
  location: location
  tags: union(
    union(
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
    ),
    !empty(azdServiceName)
      ? {
          'azd-service-name': azdServiceName
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
    workloadProfileName: workloadProfileName
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
      registries: !empty(ghcrUsername) && !empty(ghcrPat)
        ? [
            {
              server: containerRegistryEndpoint
              username: ghcrUsername
              passwordSecretRef: 'ghcr-pat'
            }
          ]
        : []
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
          image: '${containerRegistryEndpoint}/${imageName}'
          resources: {
            cpu: any(cpu)
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

// ACR Pull role assignment removed; only GHCR supported

output id string = containerApp.id
output name string = containerApp.name
output uri string = externalIngress ? 'https://${containerApp.properties.configuration.ingress.fqdn}' : ''
output fqdn string = containerApp.properties.configuration.ingress.fqdn
@description('Managed Identity Principal ID for RBAC role assignments (Key Vault, App Config, SQL, Redis)')
output managedIdentityPrincipalId string = managedIdentityPrincipalId

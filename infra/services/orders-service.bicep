import { finopsMinReplicas, finopsMaxReplicas, finopsCpuCores, finopsMemory } from '../config/constants.bicep'

@description('Location for resources')
param location string = resourceGroup().location

@description('Tags to apply')
param tags object = {}

@description('Container Apps Environment ID')
param containerAppsEnvironmentId string


@description('Container Registry endpoint')
param containerRegistryEndpoint string

@description('Container Registry username (for GHCR)')
param ghcrUsername string = ''

@description('Container Registry Personal Access Token (for GHCR)')
@secure()
param ghcrPat string = ''

@description('Type of container registry to use (acr or ghcr)')
param registryType string = 'ghcr'

@description('User-Assigned Managed Identity resource ID for ACR (for acr only)')
param acrIdentityId string = ''

@description('App Configuration endpoint')
param appConfigEndpoint string = ''

@description('Log Analytics Workspace ID')
param logAnalyticsWorkspaceId string


@description('JWT secret key')
@secure()
param jwtSecretKey string

@description('JWT issuer')
param jwtIssuer string = 'MyApp.Auth'

@description('JWT audience')
param jwtAudience string = 'MyApp.All'

@description('Frontend origin for CORS')
param frontendOrigin string = 'http://localhost:3000'

@description('ASP.NET Core environment')
param aspnetcoreEnvironment string = 'Production'

@description('Image tag for container')
param imageTag string = 'latest'

@description('Container Apps managed identity principal ID')
param managedIdentityPrincipalId string = ''

@description('User-Assigned Managed Identity ID')
param userAssignedIdentityId string

@description('Base resource name prefix for this deployment (e.g., myapp-dev)')
param namePrefix string

@description('Environment slug (e.g., dev, prod)')
param envSlug string = 'dev'

var basePrefix = replace(namePrefix, '-${envSlug}', '')
var serviceName = '${namePrefix}-orders-service'
var imageName = '${basePrefix}-orders-service-${envSlug}'

module ordersService 'container-app-service.bicep' = {
  name: serviceName
  params: {
    name: serviceName
    location: location
    tags: tags
    containerAppsEnvironmentId: containerAppsEnvironmentId
    containerRegistryEndpoint: containerRegistryEndpoint
    ghcrUsername: ghcrUsername
    ghcrPat: ghcrPat
    registryType: registryType
    acrIdentityId: acrIdentityId
    imageName: '${imageName}:${imageTag}'
    targetPort: 8080
    externalIngress: false
    daprEnabled: true
    daprAppId: serviceName
    daprAppPort: 8080
    minReplicas: finopsMinReplicas  // OPTIMITZACIÓ FINOPS: Escala a zero - no paga quan no s'usa
    maxReplicas: finopsMaxReplicas  // Mínim possible
    cpu: json(finopsCpuCores)  // Mínim absolut (250m cores)
    memory: finopsMemory  // Mínim absolut (512 MiB)
    jwtSecretKey: jwtSecretKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    frontendOrigin: frontendOrigin
    aspnetcoreEnvironment: aspnetcoreEnvironment
    appConfigEndpoint: appConfigEndpoint
    logAnalyticsWorkspaceId: logAnalyticsWorkspaceId
    managedIdentityPrincipalId: managedIdentityPrincipalId
    userAssignedIdentityId: userAssignedIdentityId
    azdServiceName: 'orders-service'  // Must match service name in azure.yaml
  }
}

output id string = ordersService.outputs.id
output name string = ordersService.outputs.name
output uri string = ordersService.outputs.uri
output fqdn string = ordersService.outputs.fqdn
@description('Managed Identity Principal ID for RBAC role assignments (Phase 3)')
output managedIdentityPrincipalId string = ordersService.outputs.managedIdentityPrincipalId

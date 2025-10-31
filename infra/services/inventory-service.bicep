@description('Location for resources')
param location string = resourceGroup().location

@description('Tags to apply')
param tags object = {}

@description('Container Apps Environment ID')
param containerAppsEnvironmentId string

@description('Container Registry endpoint')
param containerRegistryEndpoint string

@description('Key Vault URI')
param keyVaultUri string

@description('App Configuration connection string')
@secure()
param appConfigConnectionString string = ''

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

var serviceName = '${namePrefix}-inventory-service'
var imageName = 'inventory-service'

module inventoryService 'container-app-service.bicep' = {
  name: serviceName
  params: {
    name: serviceName
    location: location
    tags: tags
    containerAppsEnvironmentId: containerAppsEnvironmentId
    containerRegistryEndpoint: containerRegistryEndpoint
    imageName: '${imageName}:${imageTag}'
    targetPort: 8080
    externalIngress: false
    daprEnabled: true
    daprAppId: serviceName
    daprAppPort: 8080
    minReplicas: 2
    maxReplicas: 5
    cpu: '0.5'
    memory: '1.0Gi'
    jwtSecretKey: jwtSecretKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    frontendOrigin: frontendOrigin
    aspnetcoreEnvironment: aspnetcoreEnvironment
    keyVaultUri: keyVaultUri
    appConfigConnectionString: appConfigConnectionString
    keyVaultSecrets: [
      {
        name: 'jwt-secret-key'
        secretName: 'jwt-secret-key'
      }
      {
        name: 'db-connection'
        secretName: 'sql-connection-inventorydb'
      }
      {
        name: 'cache-connection'
        secretName: 'redis-connection'
      }
    ]
    logAnalyticsWorkspaceId: logAnalyticsWorkspaceId
    managedIdentityPrincipalId: managedIdentityPrincipalId
    userAssignedIdentityId: userAssignedIdentityId
  }
}

output id string = inventoryService.outputs.id
output name string = inventoryService.outputs.name
output uri string = inventoryService.outputs.uri
output fqdn string = inventoryService.outputs.fqdn
@description('Managed Identity Principal ID for RBAC role assignments (Phase 3)')
output managedIdentityPrincipalId string = inventoryService.outputs.managedIdentityPrincipalId

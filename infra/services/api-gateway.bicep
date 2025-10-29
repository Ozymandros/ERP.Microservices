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

@description('Base resource name prefix for this deployment (e.g., myapp-dev)')
param namePrefix string

var serviceName = '${namePrefix}-api-gateway'
var imageName = 'erp-api-gateway'

module apiGateway 'container-app-service.bicep' = {
  name: serviceName
  params: {
    name: serviceName
    location: location
    tags: tags
    containerAppsEnvironmentId: containerAppsEnvironmentId
    containerRegistryEndpoint: containerRegistryEndpoint
    imageName: '${imageName}:${imageTag}'
    targetPort: 8080
    externalIngress: true
    daprEnabled: false
    minReplicas: 2
    maxReplicas: 10
    cpu: '1.0'
    memory: '2.0Gi'
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
    ]
    logAnalyticsWorkspaceId: logAnalyticsWorkspaceId
    managedIdentityPrincipalId: managedIdentityPrincipalId
  }
}

output id string = apiGateway.outputs.id
output name string = apiGateway.outputs.name
output uri string = apiGateway.outputs.uri
output fqdn string = apiGateway.outputs.fqdn
@description('Managed Identity Principal ID for RBAC role assignments (Phase 3)')
output managedIdentityPrincipalId string = apiGateway.outputs.managedIdentityPrincipalId

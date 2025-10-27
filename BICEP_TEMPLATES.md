# PLANTILLAS PARA MÓDULOS BICEP FALTANTES

## 1. Estructura general para servicios

Todos los servicios siguen el mismo patrón. Usa esta plantilla como base.

---

## 📦 PLANTILLA: Service Module

Nombre: `infra/{service-name}/{service-name}.module.bicep`

```bicep
@description('Location for the resources')
param location string

@description('Tags for all resources')
param tags object = {}

@description('Container Apps Environment Name')
param containerAppsEnvironmentName string

@description('Container Registry Name')
param containerRegistryName string

@description('Managed Identity ID')
param managedIdentityId string

@description('SQL Server FQDN')
param sqlServerFqdn string

@description('SQL Server Admin Name')
param sqlServerAdminName string

@description('Redis hostname')
param redisHostName string

@description('Redis SSL port')
param redisSslPort int = 6379

@description('Redis password')
@secure()
param redisPrimaryKey string

@description('Application Insights Connection String')
param appInsightsConnectionString string

@description('JWT Secret Key')
@secure()
param jwtSecretKey string

@description('JWT Issuer')
param jwtIssuer string

@description('JWT Audience')
param jwtAudience string

@description('Frontend Origin for CORS')
param frontendOrigin string = 'https://localhost:3000'

@description('Service name (e.g., auth, billing, inventory)')
param serviceName string

@description('Database name for this service')
param databaseName string

@description('Container image name')
param imageName string

@description('Min replicas for autoscaling')
param minReplicas int = 2

@description('Max replicas for autoscaling')
param maxReplicas int = 5

@description('Port exposed by the container')
param containerPort int = 8080

// Resource references
resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' existing = {
  name: containerAppsEnvironmentName
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' existing = {
  name: containerRegistryName
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01' existing = {
  name: split(sqlServerFqdn, '.')[0]
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01' existing = {
  parent: sqlServer
  name: databaseName
}

// Build connection string
var connectionString = 'Server=tcp:${sqlServerFqdn},1433;Initial Catalog=${databaseName};Persist Security Info=False;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

// Redis connection string
var redisConnectionString = '${redisHostName}:${redisSslPort},password=${redisPrimaryKey},ssl=True'

// Container App
resource containerApp 'Microsoft.App/containerApps@2024-02-02-preview' = {
  name: '${serviceName}-service'
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      ingress: {
        external: false  // Internal only, traffic through Gateway
        targetPort: containerPort
        transport: 'auto'
        allowInsecure: false
      }
      registries: [
        {
          server: containerRegistry.properties.loginServer
          identity: 'system-assigned'
        }
      ]
      secrets: [
        {
          name: 'connection-string'
          value: connectionString
        }
        {
          name: 'redis-connection-string'
          value: redisConnectionString
        }
        {
          name: 'jwt-secret-key'
          value: jwtSecretKey
        }
        {
          name: 'app-insights-connection-string'
          value: appInsightsConnectionString
        }
      ]
    }
    template: {
      containers: [
        {
          name: serviceName
          image: '${containerRegistry.properties.loginServer}/${imageName}'
          resources: {
            cpu: json('1.0')
            memory: '2.0Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:${containerPort}'
            }
            {
              name: 'ConnectionStrings__DefaultConnection'
              secretRef: 'connection-string'
            }
            {
              name: 'Redis__ConnectionString'
              secretRef: 'redis-connection-string'
            }
            {
              name: 'Jwt__SecretKey'
              secretRef: 'jwt-secret-key'
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
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              secretRef: 'app-insights-connection-string'
            }
            {
              name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
              value: '~3'
            }
            {
              name: 'FRONTEND_ORIGIN'
              value: frontendOrigin
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: containerPort
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
                port: containerPort
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
            name: 'http-scaling-rule'
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

// Grant this service access to ACR
resource acrPullRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(containerApp.id, containerRegistry.id, 'acrpull')
  scope: containerRegistry
  properties: {
    principalId: containerApp.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
  }
}

// Grant this service access to SQL Server database
resource sqlDatabaseReaderRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(sqlDatabase.id, containerApp.identity.principalId, 'db-reader')
  scope: sqlDatabase
  properties: {
    principalId: containerApp.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'c12c1926-28b6-475e-b378-a00be61cc639')
  }
}

output id string = containerApp.id
output name string = containerApp.name
output fqdn string = containerApp.properties.configuration.ingress.fqdn
output internalFqdn string = 'https://${serviceName}-service.internal.${containerAppsEnvironment.properties.defaultDomain}'
```

---

## 🔌 PLANTILLA: API Gateway Module

Nombre: `infra/api-gateway/api-gateway.module.bicep`

```bicep
@description('Location for the resources')
param location string

@description('Tags for all resources')
param tags object = {}

@description('Container Apps Environment Name')
param containerAppsEnvironmentName string

@description('Container Registry Name')
param containerRegistryName string

@description('Managed Identity ID')
param managedIdentityId string

@description('Application Insights Connection String')
param appInsightsConnectionString string

@description('Container image name')
param imageName string = 'erpapigateway:latest'

@description('Min replicas for autoscaling')
param minReplicas int = 2

@description('Max replicas for autoscaling')
param maxReplicas int = 5

// Resource references
resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' existing = {
  name: containerAppsEnvironmentName
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' existing = {
  name: containerRegistryName
}

// API Gateway Container App
resource containerApp 'Microsoft.App/containerApps@2024-02-02-preview' = {
  name: 'api-gateway'
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      ingress: {
        external: true  // This is the public entry point
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
        corsPolicy: {
          allowCredentials: true
          allowedHeaders: ['*']
          allowedHttpMethods: ['GET', 'POST', 'PUT', 'DELETE', 'PATCH', 'OPTIONS']
          allowedOrigins: ['*']  // Restrict in production
          exposeHeaders: ['*']
          maxAge: 3600
        }
      }
      registries: [
        {
          server: containerRegistry.properties.loginServer
          identity: 'system-assigned'
        }
      ]
      secrets: [
        {
          name: 'app-insights-connection-string'
          value: appInsightsConnectionString
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'gateway'
          image: '${containerRegistry.properties.loginServer}/${imageName}'
          resources: {
            cpu: json('1.0')
            memory: '2.0Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8080'
            }
            {
              name: 'OCELOT_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              secretRef: 'app-insights-connection-string'
            }
            {
              name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
              value: '~3'
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: 8080
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
                port: 8080
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
            name: 'http-scaling-rule'
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

// Grant this service access to ACR
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
output fqdn string = containerApp.properties.configuration.ingress.fqdn
output url string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
```

---

## 📊 PLANTILLA: Application Insights Module

Nombre: `infra/MyApp-ApplicationInsights/MyApp-ApplicationInsights.module.bicep`

```bicep
@description('Location for Application Insights')
param location string

@description('Log Analytics Workspace ID')
param workspaceId string

@description('Resource tags')
param tags object = {}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appins-${uniqueString(resourceGroup().id)}'
  location: location
  kind: 'web'
  tags: tags
  properties: {
    Application_Type: 'web'
    RetentionInDays: 30
    WorkspaceResourceId: workspaceId
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

output appInsightsId string = applicationInsights.id
output appInsightsName string = applicationInsights.name
output appInsightsConnectionString string = applicationInsights.properties.ConnectionString
output appInsightsInstrumentationKey string = applicationInsights.properties.InstrumentationKey
```

---

## 📊 PLANTILLA: Log Analytics Module

Nombre: `infra/MyApp-LogAnalyticsWorkspace/MyApp-LogAnalyticsWorkspace.module.bicep`

```bicep
@description('Location for Log Analytics Workspace')
param location string

@description('Resource tags')
param tags object = {}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'law-${uniqueString(resourceGroup().id)}'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.id
output logAnalyticsWorkspaceName string = logAnalyticsWorkspace.name
output customerId string = logAnalyticsWorkspace.properties.customerId
```

---

## 🔐 PLANTILLA: SQL Server Roles Module

Nombre: `infra/myapp-sqlserver-roles/myapp-sqlserver-roles.module.bicep`

```bicep
@description('SQL Server name')
param sqlServerName string

@description('SQL Server Admin Managed Identity Name')
param sqlServerAdminName string

@description('Principal ID (Managed Identity) of Container Apps')
param principalId string

@description('Principal Name for display')
param principalName string

@description('Location')
param location string = resourceGroup().location

// SQL Server reference
resource sqlServer 'Microsoft.Sql/servers@2023-08-01' existing = {
  name: sqlServerName
}

// Create contained user for the managed identity
resource sqlServerAdministrator 'Microsoft.Sql/servers/administrators@2023-08-01' = {
  parent: sqlServer
  name: 'ActiveDirectory'
  properties: {
    administratorType: 'ActiveDirectory'
    login: principalName
    sid: principalId
    tenantId: subscription().tenantId
  }
}

output adminName string = sqlServerAdministrator.name
```

---

## 📝 COMANDOS PARA IMPLEMENTAR

### 1. Crear estructura de carpetas

```powershell
# Crear carpetas para módulos de servicios
$services = @('auth-service', 'billing-service', 'inventory-service', 'orders-service', 'purchasing-service', 'sales-service')
foreach ($service in $services) {
    New-Item -ItemType Directory -Path "infra/$service" -Force
}

# Crear carpetas faltantes
New-Item -ItemType Directory -Path "infra/MyApp-ApplicationInsights" -Force
New-Item -ItemType Directory -Path "infra/MyApp-LogAnalyticsWorkspace" -Force
New-Item -ItemType Directory -Path "infra/myapp-sqlserver-roles" -Force
```

### 2. Copiar plantillas

Usa los templates anteriores para crear cada módulo `.bicep`

### 3. Actualizar main.bicep

Agregar módulos en `main.bicep` (ver documento siguiente)


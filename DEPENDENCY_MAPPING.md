# 📊 MAPEO DE DEPENDENCIAS ASPIRE → AZURE BICEP

Visualización de cómo el código Aspire (`Program.cs`) se mapea a la infraestructura Bicep.

---

## 🏗️ ARQUITECTURA GENERAL

```
┌─────────────────────────────────────────────────────────┐
│                   ASPIRE HOST (AppHost)                 │
│  (local development + generates deployment manifests)   │
└─────────────────────────────────────────────────────────┘
                            │
                            │ azd deploy
                            ↓
┌─────────────────────────────────────────────────────────┐
│              AZURE BICEP TEMPLATES                      │
│  (main.bicep + resource modules)                        │
└─────────────────────────────────────────────────────────┘
                            │
                            │ Executes
                            ↓
┌─────────────────────────────────────────────────────────┐
│              AZURE CONTAINER APPS                       │
│  (microservices running in production)                  │
└─────────────────────────────────────────────────────────┘
```

---

## 📋 MAPEO DETALLADO

### 1. ASPIRE: SQL SERVER LOCAL

**Código en Program.cs (línea 36-44):**
```csharp
if (isDeployment)
{
    var sqlServer = builder.AddAzureSqlServer("myapp-sqlserver");
    projectBuilder = builder.CreateProjectBuilder();
}
else
{
    var sqlServer = builder.AddSqlServer("myapp-sqlserver", password, 1455)
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume("sqlserver-data");
    projectBuilder = builder.CreateProjectBuilder(sqlServer);
}
```

**Mapeo a BICEP (main.bicep):**
```
✅ Existe: infra/myapp-sqlserver/myapp-sqlserver.module.bicep
```

**Recurso Azure creado:**
```bicep
Microsoft.Sql/servers@2023-08-01
  → nombre: myapp-sqlserver-{uniqueString}
  → admin: Managed Identity (Azure AD)
  → firewall: AllowAllAzureIps
```

---

### 2. ASPIRE: REDIS CACHE

**Código en Program.cs (línea 19-24):**
```csharp
var redis = builder.AddRedis("cache")
    .WithRedisCommander()
    .WithRedisInsight()
    .WithDataVolume("redis-cache");
```

**Mapeo a BICEP:**
```
❌ FALTA: infra/core/database/redis.bicep NO se usa en main.bicep
```

**Recurso Azure necesario:**
```bicep
Microsoft.Cache/redis@2023-08-01
  → nombre: redis-{uniqueString}
  → SKU: Standard
  → capacity: 1
  → Modo SSL/TLS
```

---

### 3. ASPIRE: SERVICIOS MICROSERVICIOS

**Código en Program.cs (línea 50-56):**
```csharp
var authService = projectBuilder.AddWebProject<Projects.MyApp_Auth_API>(redis, origin, isDeployment, applicationInsights);
var billingService = projectBuilder.AddWebProject<Projects.MyApp_Billing_API>(redis, origin, isDeployment, applicationInsights);
// ... 4 servicios más
```

**Mapeo a BICEP:**
```
❌ FALTA x6:
  - infra/auth-service/auth-service.module.bicep
  - infra/billing-service/billing-service.module.bicep
  - infra/inventory-service/inventory-service.module.bicep
  - infra/orders-service/orders-service.module.bicep
  - infra/purchasing-service/purchasing-service.module.bicep
  - infra/sales-service/sales-service.module.bicep
```

**Recurso Azure necesario (x6):**
```bicep
Microsoft.App/containerApps@2024-02-02-preview
  → nombre: {serviceName}-service
    • auth-service
    • billing-service
    • inventory-service
    • orders-service
    • purchasing-service
    • sales-service
  
  → propiedades:
    • containerAppsEnvironmentId: CAE
    • image: {registry}/{serviceName}:latest
    • ingress: false (solo acceso interno)
    • env:
      - ConnectionStrings__DefaultConnection: SQL
      - Redis__ConnectionString: Redis
      - Jwt__SecretKey: secret
      - ApplicationInsights__ConnectionString: AppInsights
```

---

### 4. ASPIRE: API GATEWAY (OCELOT)

**Código en Program.cs (línea 69-85):**
```csharp
if (isDeployment)
{
    var apiGateway = builder.AddProject<Projects.ErpApiGateway>("api-gateway")
        .WaitFor(authService)
        .WaitFor(billingService)
        // ... wait for all services
        .WithExternalHttpEndpoints()
        .WithEnvironment("OCELOT_ENVIRONMENT", "Production")
        .PublishAsDockerFile();
}
```

**Mapeo a BICEP:**
```
❌ FALTA: infra/api-gateway/api-gateway.module.bicep
```

**Recurso Azure necesario:**
```bicep
Microsoft.App/containerApps@2024-02-02-preview
  → nombre: api-gateway
  → propiedades:
    • image: {registry}/erpapigateway:latest
    • ingress: true (EXTERNAL = punto de entrada público)
    • port: 8080
    • env:
      - OCELOT_ENVIRONMENT: Production
      - ASPNETCORE_ENVIRONMENT: Production
    
  → rutas (ocelot.Production.json):
    • /auth/* → auth-service:8080
    • /billing/* → billing-service:8080
    • /inventory/* → inventory-service:8080
    • /orders/* → orders-service:8080
    • /purchasing/* → purchasing-service:8080
    • /sales/* → sales-service:8080
```

---

### 5. ASPIRE: APPLICATION INSIGHTS

**Código en Program.cs (línea 10-13):**
```csharp
var analyticsWorkspace = isDeployment ? builder
    .AddAzureLogAnalyticsWorkspace("MyApp-LogAnalyticsWorkspace") : null;
var applicationInsights = isDeployment ? builder
    .AddAzureApplicationInsights("MyApp-ApplicationInsights")
    .WithLogAnalyticsWorkspace(analyticsWorkspace) : null;
```

**Mapeo a BICEP:**
```
❌ FALTA: infra/MyApp-ApplicationInsights/MyApp-ApplicationInsights.module.bicep
❌ FALTA: infra/MyApp-LogAnalyticsWorkspace/MyApp-LogAnalyticsWorkspace.module.bicep
```

**Recursos Azure necesarios:**
```bicep
Microsoft.OperationalInsights/workspaces@2022-10-01
  → nombre: law-{uniqueString}

Microsoft.Insights/components@2020-02-02
  → nombre: appins-{uniqueString}
  → linked: Log Analytics Workspace
  → Output: connection string
```

---

### 6. ASPIRE: HEALTH CHECKS

**Código en Program.cs (línea 15):**
```csharp
builder.Services.AddHealthChecks();
```

**En AspireProjectBuilder.cs (línea 100):**
```csharp
.WithHttpHealthCheck(path: "/health", statusCode: 200)
```

**Mapeo a BICEP:**
```bicep
# Cada container app tiene probes:
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
    failureThreshold: 3
  },
  {
    type: 'Readiness'
    httpGet: {
      path: '/health'
      port: 8080
      scheme: 'HTTP'
    }
    initialDelaySeconds: 10
    periodSeconds: 10
    failureThreshold: 3
  }
]
```

**⚠️ CRÍTICO:** Todos los servicios DEBEN responder en `/health` endpoint

---

## 🔗 FLUJO DE DEPENDENCIAS

```
main.bicep (orquestador principal)
    │
    ├─ resources.bicep
    │   ├─ Managed Identity
    │   ├─ Container Registry
    │   ├─ Log Analytics Workspace
    │   └─ Container Apps Environment
    │
    ├─ MyApp-LogAnalyticsWorkspace.module.bicep
    │   └─ Log Analytics Workspace
    │
    ├─ MyApp-ApplicationInsights.module.bicep
    │   └─ Application Insights (depende de LAW)
    │
    ├─ myapp-sqlserver.module.bicep
    │   ├─ SQL Server (Azure AD auth)
    │   └─ Firewall rules
    │
    ├─ myapp-sqlserver-roles.module.bicep
    │   └─ SQL Server roles (depende de SQL Server)
    │
    ├─ core/database/redis.bicep
    │   └─ Redis Cache
    │
    ├─ auth-service.module.bicep
    │   ├─ depende de: SQL Server, Redis, AppInsights, CAE, Registry
    │   └─ crea: Container App (auth-service)
    │
    ├─ billing-service.module.bicep
    │   └─ idem
    │
    ├─ inventory-service.module.bicep
    │   └─ idem
    │
    ├─ orders-service.module.bicep
    │   └─ idem
    │
    ├─ purchasing-service.module.bicep
    │   └─ idem
    │
    ├─ sales-service.module.bicep
    │   └─ idem
    │
    └─ api-gateway.module.bicep
        ├─ depende de: CAE, Registry, AppInsights
        └─ crea: Container App (api-gateway) con ingress externa
```

---

## 🔄 ORDEN DE DEPLOYMENT

Bicep despliega automáticamente en orden correcto (resuelve dependencias):

```
1️⃣ resources.bicep
   ↓
2️⃣ MyApp-LogAnalyticsWorkspace
   ↓
3️⃣ MyApp-ApplicationInsights (depende 2)
   ↓
4️⃣ myapp-sqlserver
   ↓
5️⃣ myapp-sqlserver-roles (depende 4)
   ↓
6️⃣ redis
   ↓
7️⃣ auth-service (depende 1,3,4,6)
8️⃣ billing-service (depende 1,3,4,6)
9️⃣ inventory-service (depende 1,3,4,6)
🔟 orders-service (depende 1,3,4,6)
1️⃣1️⃣ purchasing-service (depende 1,3,4,6)
1️⃣2️⃣ sales-service (depende 1,3,4,6)
   ↓
1️⃣3️⃣ api-gateway (depende 1,3 - servicios accesibles internamente)
```

---

## 🔐 MATRIZ DE ACCESO Y PERMISOS

| Componente | Acceso a | Tipo de auth | Configurado en Bicep |
|-----------|----------|-------------|-------------------|
| Servicios | SQL Server | Managed Identity | ❓ Parcial |
| Servicios | Redis | Connection string + password | ❌ NO |
| Servicios | AppInsights | Connection string | ❌ NO |
| API Gateway | Servicios | Internal DNS | ✅ SI (Ocelot) |
| Usuarios externos | API Gateway | HTTPS/HTTP | ✅ SI (ingress) |
| Container Apps | ACR | Managed Identity | ✅ SI |

---

## 📤 OUTPUTS DE BICEP → VARIABLES DE ENTORNO

Después del deployment, estos outputs están disponibles para CI/CD o scripts:

```
MANAGED_IDENTITY_CLIENT_ID         → Para auth
MANAGED_IDENTITY_NAME              → Para audit
AZURE_CONTAINER_REGISTRY_ENDPOINT  → Para push imágenes
AZURE_CONTAINER_REGISTRY_NAME      → Para pull imágenes
AZURE_CONTAINER_APPS_ENVIRONMENT_ID → Para refs en Bicep
AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN → Para service discovery
MYAPP_SQLSERVER_SQLSERVERFQDN      → Connection string
MYAPP_APPLICATIONINSIGHTS_APPINSIGHTSCONNECTIONSTRING → Para logging
API_GATEWAY_URL                    → URL pública (NUEVO)
AUTH_SERVICE_FQDN                  → Para testing (NUEVO)
BILLING_SERVICE_FQDN               → Para testing (NUEVO)
... (más servicios)
```

---

## ⚠️ PUNTOS CRÍTICOS DE VALIDACIÓN

### 1. Service Discovery Names
```
Aspire nombres:        DEBE coincidir con        Bicep nombres:
auth-service           ←→                        auth-service
billing-service        ←→                        billing-service
inventory-service      ←→                        inventory-service
orders-service         ←→                        orders-service
purchasing-service     ←→                        purchasing-service
sales-service          ←→                        sales-service
api-gateway            ←→                        api-gateway
```

**Validación:** Ocelot `ocelot.Production.json` usa exactamente estos nombres

### 2. Port Consistency
```
Local (Aspire):          Production (Bicep):
localhost:5000 (gateway) ←→ api-gateway:8080
localhost:5001 (auth)    ←→ auth-service:8080
localhost:5002 (billing) ←→ billing-service:8080
... etc
```

**Validación:** Todos los servicios escuchan en puerto 8080 en Container Apps

### 3. Health Check Paths
```
Aspire config:                     Bicep liveness probe:
WithHttpHealthCheck(path: "/health") ←→ path: '/health'
statusCode: 200                    ←→ expectedStatus: 200
```

**Validación:** Todos los servicios DEBEN tener endpoint GET `/health` → 200 OK

### 4. Environment Variables
```
Program.cs SetEnvironment:              Container App env:
.WithEnvironment("Jwt__Issuer", value)  ←→ Jwt__Issuer: value
```

---

## 🧪 CHECKLIST DE VALIDACIÓN

Antes de `azd deploy`:

- [ ] Todos los módulos `.bicep` existen
- [ ] `main.bicep` llama a todos los módulos
- [ ] Service names coinciden: Aspire = Ocelot routes = Container App names
- [ ] Todos los servicios tienen `/health` endpoint
- [ ] Todos los Dockerfiles existen
- [ ] Todas las imágenes están en ACR
- [ ] `ocelot.Production.json` tiene rutas para todos los servicios
- [ ] Variables de entorno configuradas en `azure.yaml` o `.env.local`
- [ ] Conexión strings para SQL, Redis, AppInsights mapeadas
- [ ] Managed Identities configuradas para SQL Server
- [ ] Health checks retornan 200 OK en todos los servicios

---


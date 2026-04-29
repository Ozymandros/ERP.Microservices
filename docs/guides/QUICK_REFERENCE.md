# ⚡ QUICK REFERENCE - INFRAESTRUCTURA ASPIRE → AZURE

Tarjeta rápida de referencia para implementar correcciones.

---

## 🎯 QUÉ FALTA (7 cosas)

```
1. ❌ auth-service.module.bicep
2. ❌ billing-service.module.bicep
3. ❌ inventory-service.module.bicep
4. ❌ orders-service.module.bicep
5. ❌ purchasing-service.module.bicep
6. ❌ sales-service.module.bicep
7. ❌ api-gateway.module.bicep
8. ❌ MyApp-ApplicationInsights.module.bicep
9. ❌ MyApp-LogAnalyticsWorkspace.module.bicep
10. ❌ myapp-sqlserver-roles.module.bicep
```

---

## 🛠️ PASO A PASO (3 HORAS)

### PASO 1: Crear carpetas (2 min)
```powershell
$services = 'auth-service','billing-service','inventory-service','orders-service','purchasing-service','sales-service'
foreach ($s in $services) { mkdir "infra/$s" -Force }
mkdir "infra/MyApp-ApplicationInsights" -Force
mkdir "infra/MyApp-LogAnalyticsWorkspace" -Force  
mkdir "infra/myapp-sqlserver-roles" -Force
```

### PASO 2: Crear módulos (1.5 horas)
Copiar cada template de `BICEP_TEMPLATES.md` a su archivo `.bicep` correspondiente

**6x Servicios:** Usar template "PLANTILLA: Service Module"
```
infra/auth-service/auth-service.module.bicep (copiar template, cambiar serviceName: 'auth', databaseName: 'AuthDB')
infra/billing-service/billing-service.module.bicep (serviceName: 'billing', databaseName: 'BillingDB')
...etc
```

**1x API Gateway:** Usar template "PLANTILLA: API Gateway Module"
```
infra/api-gateway/api-gateway.module.bicep
```

**3x Componentes Core:** Usar templates especializados
```
infra/MyApp-ApplicationInsights/MyApp-ApplicationInsights.module.bicep
infra/MyApp-LogAnalyticsWorkspace/MyApp-LogAnalyticsWorkspace.module.bicep
infra/myapp-sqlserver-roles/myapp-sqlserver-roles.module.bicep
```

### PASO 3: Actualizar main.bicep (20 min)
Reemplazar contenido completo con código de `MAIN_BICEP_UPDATE.md`

### PASO 4: Actualizar parámetros (10 min)
Actualizar `infra/main.parameters.json` con nuevos parámetros del documento

### PASO 5: Validar (5 min)
```powershell
cd src
./validate-infra.ps1
```

**Si dice ✅ OK → Ir a PASO 6**  
**Si dice ❌ ERROR → Revisar y corregir**

### PASO 6: Preparar deployment (10 min)
```powershell
# Ver qué imágenes existen en ACR
az acr repository list --name <registry-name>

# Configurar variables (o en .env.local)
azd env set JWT_SECRET_KEY "tu-secret-key"
azd env set JWT_ISSUER "tu-issuer"
azd env set JWT_AUDIENCE "tu-audience"
azd env set FRONTEND_ORIGIN "https://tudominio.com"
```

### PASO 7: Deploy (10 min)
```powershell
azd validate    # Validar templates
azd deploy      # Desplegar a Azure
```

---

## 📋 TEMPLATES ESENCIALES

### Template MÍNIMO para Servicio
```bicep
# Nombre: infra/{serviceName}/{serviceName}.module.bicep
@description('Location for the resources')
param location string

@description('Container Apps Environment Name')
param containerAppsEnvironmentName string

@description('Container Registry Name')
param containerRegistryName string

@description('Service name')
param serviceName string

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' existing = {
  name: containerAppsEnvironmentName
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' existing = {
  name: containerRegistryName
}

resource containerApp 'Microsoft.App/containerApps@2024-02-02-preview' = {
  name: '${serviceName}-service'
  location: location
  identity: { type: 'SystemAssigned' }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      ingress: {
        external: false  # NO es público
        targetPort: 8080
      }
      registries: [{ server: containerRegistry.properties.loginServer, identity: 'system-assigned' }]
    }
    template: {
      containers: [{
        name: serviceName
        image: '${containerRegistry.properties.loginServer}/myapp.${serviceName}.api:latest'
        resources: { cpu: json('1.0'), memory: '2.0Gi' }
        env: [
          { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
          { name: 'ASPNETCORE_URLS', value: 'http://+:8080' }
        ]
      }]
      scale: { minReplicas: 2, maxReplicas: 5 }
    }
  }
}
```

### Template main.bicep - Agregar módulos
```bicep
module {serviceName}_service '{serviceName}-service/{serviceName}-service.module.bicep' = {
  name: '{serviceName}-service'
  scope: rg
  params: {
    location: location
    tags: tags
    containerAppsEnvironmentName: resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_NAME
    containerRegistryName: resources.outputs.AZURE_CONTAINER_REGISTRY_NAME
    serviceName: '{serviceName}'
    databaseName: '{DatabaseName}DB'
    imageName: 'myapp.{serviceName}.api:latest'
  }
}
```

---

## 🔗 NOMBRES - DEBEN COINCIDIR

| Aspire | Bicep Module | Container App | Ocelot Route |
|--------|-------------|----------------|-------------|
| `auth-service` | `auth-service.module.bicep` | `auth-service` | `Host: auth-service:8080` |
| `billing-service` | `billing-service.module.bicep` | `billing-service` | `Host: billing-service:8080` |
| `inventory-service` | `inventory-service.module.bicep` | `inventory-service` | `Host: inventory-service:8080` |
| `orders-service` | `orders-service.module.bicep` | `orders-service` | `Host: orders-service:8080` |
| `purchasing-service` | `purchasing-service.module.bicep` | `purchasing-service` | `Host: purchasing-service:8080` |
| `sales-service` | `sales-service.module.bicep` | `sales-service` | `Host: sales-service:8080` |
| `api-gateway` | `api-gateway.module.bicep` | `api-gateway` | N/A (es público) |

---

## ⚡ TROUBLESHOOTING RÁPIDO

### Error: "Module not found"
```
❌ module auth-service 'auth-service/auth-service.module.bicep'
✅ Crear: infra/auth-service/auth-service.module.bicep
```

### Error: "Parameter missing"
```
❌ jwtSecretKey parameter not defined
✅ Agregar en main.bicep parámetros de MAIN_BICEP_UPDATE.md
```

### Error: "Connection string invalid"
```
❌ Server=tcp:localhost,1433
✅ Server=tcp:myappsqlserver-xxx.database.windows.net,1433
```

### Error: "Service not found"
```
❌ Host: 'auth-service' Puerto: 6001
✅ Host: 'auth-service' Puerto: 8080
```

### Error: "Image not found in registry"
```
❌ image: '${registry}/auth.api:latest'
✅ image: '${registry}/myapp.auth.api:latest'
```

---

## 📊 MATRIZ DE DEPENDENCIAS

```
SQLServer     Redis     AppInsights
    │           │            │
    └───┬───────┴────┬───────┘
        │            │
        v            v
  ┌─────────────────────────┐
  │  Auth/Billing/Inventory │
  │  Orders/Purchasing/Sales│
  └─────────────────────────┘
          │
          v
    ┌──────────────┐
    │  API Gateway │ → Público
    └──────────────┘
```

---

## 🚀 COMANDOS CLAVE

```powershell
# 1. Validar
azd validate

# 2. Desplegar
azd deploy

# 3. Ver status
azd show

# 4. Ver servicios desplegados
az containerapp list -g rg-<env-name>

# 5. Ver logs de servicio
az containerapp logs show --name auth-service -g rg-<env-name>

# 6. Probar API Gateway
curl https://<gateway-fqdn>/health

# 7. Limpiar (si necesitas rollback)
az group delete -g rg-<env-name> -y
```

---

## ✅ CHECKLIST PRE-DEPLOY

- [ ] ¿Existen todos los 10 módulos `.bicep`?
- [ ] ¿Contiene `main.bicep` todas las referencias?
- [ ] ¿`validate-infra.ps1` retorna OK?
- [ ] ¿Todas las imágenes en ACR?
- [ ] ¿Variables de entorno configuradas?
- [ ] ¿`azd validate` pasa sin errores?

---

## 📞 DOCUMENTACIÓN COMPLETA

Si necesitas más detalles:
- **Visión general:** `SUMMARY_ACTIONS.md`
- **Todos los problemas:** `INFRA_REVIEW.md`
- **Templates completos:** `BICEP_TEMPLATES.md`
- **main.bicep completo:** `MAIN_BICEP_UPDATE.md`
- **Arquitectura:** `DEPENDENCY_MAPPING.md`

---

**⏱️ Tiempo total: 3-4 horas**  
**🎯 Resultado: Infraestructura lista para producción**


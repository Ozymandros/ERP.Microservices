# 📊 Bicep vs docker-compose.yml: Complete Configuration Mapping

This document maps every configuration element from `docker-compose.yml` to its required Bicep implementation, identifying gaps.

---

## 1. Global Configuration

### docker-compose.yml
```yaml
version: "3.9"

x-api-env: &api-env
  ASPNETCORE_ENVIRONMENT: Development
  ASPNETCORE_URLS: http://+:8080
  FRONTEND_ORIGIN: ${FRONTEND_ORIGIN:-http://localhost:3000;http://localhost:5000}
  Jwt__SecretKey: ${JWT_SECRET:-una_clau_molt_llarga_i_super_ultra_secreta_01234566789}
  Jwt__Issuer: ${JWT_ISSUER:-MyApp.Auth}
  Jwt__Audience: ${JWT_AUDIENCE:-MyApp.All}
  ConnectionStrings__cache: redis:6379
```

### Bicep Implementation Status

| Config | docker-compose | Bicep | Status | Gap |
|--------|----------------|-------|--------|-----|
| `ASPNETCORE_ENVIRONMENT` | ✅ `Development` | ⚠️ Parameter only | ⚠️ | Need to pass to container apps |
| `ASPNETCORE_URLS` | ✅ `http://+:8080` | ✅ Hardcoded | ✅ | No gap |
| `FRONTEND_ORIGIN` | ✅ Parameterized | ❌ Missing | ❌ | Add to main.bicep params |
| `Jwt__SecretKey` | ✅ From env var | ❌ Missing from main | ❌ | Add @secure() param |
| `Jwt__Issuer` | ✅ From env var | ⚠️ Hardcoded in KeyVault | ⚠️ | Make parameterized |
| `Jwt__Audience` | ✅ From env var | ⚠️ Hardcoded in KeyVault | ⚠️ | Make parameterized |
| `ConnectionStrings__cache` | ✅ `redis:6379` | ❌ Missing from services | ❌ | Add to container-app env |

---

## 2. Infrastructure Services

### SQL Server (docker-compose.yml: `sqlserver` service)

**docker-compose Configuration:**
```yaml
sqlserver:
  image: mcr.microsoft.com/mssql/server:2022-latest
  container_name: sqlserver
  environment:
    ACCEPT_EULA: "Y"
    MSSQL_PID: Developer
    MSSQL_SA_PASSWORD: ${SQL_PASSWORD:-P@ssw0rd12345!}
  ports:
    - "1455:1433"
  volumes:
    - sqlserver-data:/var/opt/mssql
  healthcheck: [...]
```

**Bicep Requirements:**

| Requirement | Implementation | Status |
|-------------|----------------|--------|
| SQL Server resource | `Microsoft.Sql/servers` | ✅ In core/database/sql-server.bicep |
| SQL Server FQDN output | `sqlServer.properties.fullyQualifiedDomainName` | ✅ Implemented |
| Firewall rules | AllowAllAzureIps | ✅ Implemented |
| **Databases (6 total)** | Loop create AuthDB, BillingDB, InventoryDB, OrderDB, PurchasingDB, SalesDB | ❌ **MISSING** |
| Admin password | Parameterized | ⚠️ Passed but not to services |
| **Database outputs** | Connection string templates | ❌ **MISSING** |

**Gap Analysis:**
```bicep
// MISSING: Database creation in myapp-sqlserver.module.bicep
// MISSING: Connection string outputs
// MISSING: Role assignments for service identities
```

---

### Redis Cache (docker-compose.yml: `redis` service)

**docker-compose Configuration:**
```yaml
redis:
  image: redis:7-alpine
  container_name: redis
  command: ["redis-server", "--save", "", "--appendonly", "no"]
  ports:
    - "6379:6379"
  volumes:
    - redis-data:/data
```

**Bicep Requirements:**

| Requirement | Implementation | Status |
|-------------|----------------|--------|
| Redis resource | `Microsoft.Cache/redis` | ✅ In core/database/redis.bicep |
| SKU (Standard/Premium) | Parameterized | ✅ Implemented |
| TLS enforcement | 1.2 minimum | ✅ Implemented |
| Primary key output | `redis.listKeys().primaryKey` | ✅ Implemented |
| **Module call** | In main.bicep | ❌ **MISSING CALL** |
| **Connection string** | `hostName:port,password=X` | ⚠️ Template exists, not used |

**Gap Analysis:**
```bicep
// MISSING: Call to redis module from main.bicep
// MISSING: Pass redis hostname to Key Vault
// MISSING: Reference redis connection string in services
```

---

### Dapr Placement Service

**docker-compose Configuration:**
```yaml
dapr-placement:
  image: daprio/dapr:1.13.0
  container_name: dapr-placement
  command: ["./placement", "-port", "50005", "-log-level", "info"]
  ports:
    - "50005:50005"
```

**Bicep Requirements:**

| Requirement | Implementation | Status |
|-------------|----------------|--------|
| Placement service | Not needed in Azure (managed) | ✅ Azure handles automatically |
| Service discovery | Container Apps built-in | ✅ Native support |
| Dapr sidecars | Parameter in container-app.bicep | ✅ Implemented |
| **Service-to-service communication** | Via Dapr service invocation | ⚠️ Template supports but not configured |

---

## 3. Microservices Configuration

### Auth Service Example (docker-compose.yml: `auth-service`)

**docker-compose Configuration:**
```yaml
auth-service:
  build:
    context: .
    dockerfile: MyApp.Auth/MyApp.Auth.API/Dockerfile
  container_name: auth-service
  environment:
    <<: *api-env  # Inherits from x-api-env
    ConnectionStrings__AuthDb: Server=sqlserver,1433;Database=AuthDb;User Id=sa;Password=${SQL_PASSWORD:-P@ssw0rd12345!};TrustServerCertificate=True;Encrypt=False;
    ConnectionStrings__AuthDB: Server=sqlserver,1433;Database=AuthDb;User Id=sa;Password=${SQL_PASSWORD:-P@ssw0rd12345!};TrustServerCertificate=True;Encrypt=False;
  depends_on:
    - sqlserver
    - redis
    - dapr-placement
  ports:
    - "5007:8080"
```

**Bicep Mapping:**

| Config Element | docker-compose | Bicep | Status |
|----------------|----------------|-------|--------|
| Image | `auth-service:latest` | `imageName: 'auth-service:latest'` | ✅ |
| `ASPNETCORE_ENVIRONMENT` | `Development` | ⚠️ Parameter | ⚠️ |
| `ASPNETCORE_URLS` | `http://+:8080` | ✅ Hardcoded | ✅ |
| `Jwt__SecretKey` | From env var | ❌ Missing | ❌ |
| `Jwt__Issuer` | From env var | ❌ Parameter missing | ❌ |
| `Jwt__Audience` | From env var | ❌ Parameter missing | ❌ |
| `ConnectionStrings__cache` | `redis:6379` | ❌ Missing | ❌ |
| `ConnectionStrings__AuthDb` | `Server=sqlserver,...` | ❌ Missing | ❌ |
| Dapr sidecar | ✅ Configured | ✅ Parameter support | ✅ |
| Service depends on Redis | ✅ Explicit | ✅ Parameter dependency | ✅ |
| **Service module** | N/A (docker-compose) | ❌ **MISSING FILE** | ❌ |

**Gap Analysis:**
```bicep
// MISSING FILE: infra/auth-service/auth-service.module.bicep
// This module needs to:
// 1. Call core/host/container-app.bicep
// 2. Pass all environment variables listed above
// 3. Reference JWT secret from Key Vault
// 4. Reference database connection from Key Vault
// 5. Reference cache connection from Key Vault
```

---

## 4. Complete Environment Variable Mapping

### All Services Need These (from `x-api-env`):

```bicep
// GLOBAL - ALL SERVICES
{
  name: 'ASPNETCORE_ENVIRONMENT'
  value: aspnetcoreEnvironment  // Parameter
}
{
  name: 'ASPNETCORE_URLS'
  value: 'http://+:8080'
}
{
  name: 'FRONTEND_ORIGIN'
  value: frontendOrigin  // Parameter
}
{
  name: 'Jwt__Issuer'
  value: jwtIssuer  // Parameter
}
{
  name: 'Jwt__Audience'
  value: jwtAudience  // Parameter
}
{
  name: 'ConnectionStrings__cache'
  value: 'redis:6379'  // FROM KEY VAULT SECRET
}
```

### Service-Specific Connection Strings:

```bicep
// AUTH SERVICE
{
  name: 'ConnectionStrings__AuthDb'
  value: 'Server=myapp.database.windows.net;Database=AuthDB;...'  // FROM KEY VAULT
}

// BILLING SERVICE
{
  name: 'ConnectionStrings__BillingDB'
  value: 'Server=myapp.database.windows.net;Database=BillingDB;...'  // FROM KEY VAULT
}

// INVENTORY SERVICE
{
  name: 'ConnectionStrings__InventoryDB'
  value: '...'  // FROM KEY VAULT
}

// And so on for Orders, Purchasing, Sales
```

### Bicep Current Status:

| Global Env Var | Parameter in main.bicep | Passed to services | In container-app.bicep | Status |
|---|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | ❌ | ❌ | ⚠️ (template only) | ❌ **GAP** |
| `ASPNETCORE_URLS` | ✅ | ❌ | ✅ (hardcoded) | ✅ |
| `FRONTEND_ORIGIN` | ❌ | ❌ | ❌ | ❌ **GAP** |
| `Jwt__Issuer` | ❌ | ❌ | ❌ | ❌ **GAP** |
| `Jwt__Audience` | ❌ | ❌ | ❌ | ❌ **GAP** |
| `ConnectionStrings__cache` | ❌ | ❌ | ❌ | ❌ **GAP** |
| Service DB connection strings | ❌ | ❌ | ❌ | ❌ **GAP** (6 gaps total) |

---

## 5. Key Vault Secret Mapping

### docker-compose.yml Approach (Local Dev):
```yaml
# Secrets stored as environment variables with defaults
Jwt__SecretKey: ${JWT_SECRET:-hardcoded_default}
MSSQL_SA_PASSWORD: ${SQL_PASSWORD:-P@ssw0rd12345!}
```

### Bicep Approach (Production):
```bicep
// Secrets stored in Key Vault
resource kvJwtSecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'jwt-secret-key'
  properties: {
    value: jwtSecretKey  // From secure parameter
  }
}

resource kvSqlSecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'sql-connection-authdb'
  properties: {
    value: 'Server=${sqlFqdn};Database=AuthDB;...'
  }
}
```

### Current Bicep Secret Status:

| Secret | docker-compose | Key Vault Module | main.bicep call | Container Ref | Overall Status |
|--------|----------------|------------------|-----------------|---------------|-----------------|
| `jwt-secret-key` | ✅ Env var | ✅ Defined | ❌ Not called | ❌ Not referenced | ❌ **BROKEN** |
| `redis-connection` | ✅ Env var | ✅ Defined | ❌ Not called | ❌ Not referenced | ❌ **BROKEN** |
| `sql-connection-authdb` | ✅ Env var | ✅ Defined | ❌ Not called | ❌ Not referenced | ❌ **BROKEN** |
| `sql-connection-billingdb` | ✅ Env var | ✅ Defined | ❌ Not called | ❌ Not referenced | ❌ **BROKEN** |
| `sql-connection-inventorydb` | ✅ Env var | ✅ Defined | ❌ Not called | ❌ Not referenced | ❌ **BROKEN** |
| `sql-connection-ordersdb` | ✅ Env var | ✅ Defined | ❌ Not called | ❌ Not referenced | ❌ **BROKEN** |
| `sql-connection-purchasingdb` | ✅ Env var | ✅ Defined | ❌ Not called | ❌ Not referenced | ❌ **BROKEN** |
| `sql-connection-salesdb` | ✅ Env var | ✅ Defined | ❌ Not called | ❌ Not referenced | ❌ **BROKEN** |

---

## 6. API Gateway Configuration

### docker-compose.yml
```yaml
erpapigateway:
  build:
    context: .
    dockerfile: ErpApiGateway/Dockerfile
  environment:
    <<: *api-env
    Ocelot__Routes__0__DownstreamHostAndPorts__0__Host: auth-service
    Ocelot__Routes__0__DownstreamHostAndPorts__0__Port: 8080
    # ... repeat for all services
  ports:
    - "5000:8080"
```

### Bicep Requirements:

| Component | Status | Detail |
|-----------|--------|--------|
| API Gateway Container App | ❌ **MISSING MODULE** | No `api-gateway.module.bicep` |
| External Ingress | ✅ Possible | `externalIngress: true` in template |
| Ocelot Routes as env vars | ⚠️ Possible | Would need to pass as env array |
| JWT authentication | ❌ Missing | No JWT secret passed |
| CORS headers | ❌ Missing | No FRONTEND_ORIGIN passed |
| Service discovery | ✅ Possible | Container Apps DNS: `auth-service:8080` |

---

## 7. Dapr Sidecar Configuration

### docker-compose.yml
```yaml
auth-service-dapr:
  image: daprio/dapr:1.13.0
  command:
    - "./daprd"
    - "-app-id", "auth-service"
    - "-app-port", "8080"
    - "-placement-host-address", "dapr-placement:50005"
```

### Bicep Implementation:

```bicep
// In container-app.bicep
dapr: daprEnabled ? {
  enabled: true
  appId: daprAppId
  appPort: daprAppPort
  appProtocol: 'http'
  enableApiLogging: true
} : null
```

### Mapping:

| Dapr Config | docker-compose | Bicep | Status |
|---|---|---|---|
| Dapr enabled | ✅ Sidecar containers | ✅ Parameter `daprEnabled` | ✅ |
| App ID | ✅ `auth-service` | ✅ Parameter `daprAppId` | ✅ |
| App port | ✅ `8080` | ✅ Parameter `daprAppPort` | ✅ |
| Placement host | ✅ `dapr-placement:50005` | ✅ Auto in Container Apps | ✅ |
| State stores | ❌ Not configured | ❌ Not configured | ❌ |
| Pub/Sub | ❌ Not configured | ❌ Not configured | ❌ |
| **Module call** | N/A | ❌ **MISSING MODULES** | ❌ |

---

## 8. Database Configuration

### docker-compose.yml Approach:
```yaml
# Each service has connection string with hardcoded database name
auth-service:
  environment:
    ConnectionStrings__AuthDb: Server=sqlserver,1433;Database=AuthDb;...

billing-service:
  environment:
    ConnectionStrings__billingdb: Server=sqlserver,1433;Database=BillingDB;...
```

### Bicep Current Status:

| Database | docker-compose | Bicep (sql-server.bicep) | Bicep (myapp-sqlserver.module.bicep) | Status |
|---|---|---|---|---|
| AuthDB | ✅ Created | ✅ In array | ❌ Not called | ❌ **NOT CREATED** |
| BillingDB | ✅ Created | ✅ In array | ❌ Not called | ❌ **NOT CREATED** |
| InventoryDB | ✅ Created | ✅ In array | ❌ Not called | ❌ **NOT CREATED** |
| OrdersDB | ✅ Created | ✅ In array | ❌ Not called | ❌ **NOT CREATED** |
| PurchasingDB | ✅ Created | ✅ In array | ❌ Not called | ❌ **NOT CREATED** |
| SalesDB | ✅ Created | ✅ In array | ❌ Not called | ❌ **NOT CREATED** |

**Gap:** The loop in `sql-server.bicep` is correct, but `myapp-sqlserver.module.bicep` doesn't create databases. It only creates the SQL Server.

---

## 9. Health Checks

### docker-compose.yml
```yaml
sqlserver:
  healthcheck:
    test:
      - CMD-SHELL
      - "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P ... -Q 'SELECT 1' || exit 1"
```

### Bicep Implementation:

```bicep
// In container-app.bicep
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
]
```

### Mapping:

| Aspect | docker-compose | Bicep | Status |
|--------|---|---|---|
| SQL Server health check | ✅ Configured | ⚠️ Not in Bicep (Azure handles) | ✅ |
| Application health check | ✅ `/health` endpoint | ✅ Configured | ✅ |
| Liveness probe | ✅ HTTP | ✅ HTTP GET | ✅ |
| Readiness probe | ⚠️ Not explicit | ✅ HTTP GET | ✅ |

---

## 10. Network & Communication

### docker-compose.yml
```yaml
services:
  auth-service:
    depends_on:
      - sqlserver
      - redis
      - dapr-placement
    networks:
      - erp
```

### Bicep Equivalent:

| Pattern | docker-compose | Bicep | Status |
|---------|---|---|---|
| Service network | ✅ `erp` bridge network | ✅ Container Apps Environment | ✅ |
| Service discovery | ✅ DNS by container name | ✅ DNS by container name | ✅ |
| Inter-service communication | ✅ `http://auth-service:8080` | ✅ `http://auth-service:8080` | ✅ |
| Port mapping | ✅ Mapped (5007→8080) | ✅ All services 8080 | ✅ |
| External access | ⚠️ Only gateway (5000) | ✅ Gateway ingress=true | ✅ |

---

## Summary Gap Table

### Critical Gaps (Deployment Blocked)

| Gap | docker-compose | Bicep | Priority |
|-----|---|---|---|
| JWT secret parameter | ✅ Configured | ❌ Missing | 🔴 P0 |
| Key Vault integration | N/A (local) | ❌ Not called | 🔴 P0 |
| Service modules (6x) | N/A (compose) | ❌ Missing | 🔴 P0 |
| API Gateway module | N/A (compose) | ❌ Missing | 🔴 P0 |
| Database creation | ✅ 6 databases | ❌ Not created | 🔴 P0 |
| SQL connections in services | ✅ All 6 | ❌ Not referenced | 🔴 P0 |
| Redis connection in services | ✅ Config | ❌ Not referenced | 🔴 P0 |

### High Priority Gaps

| Gap | docker-compose | Bicep | Priority |
|-----|---|---|---|
| FRONTEND_ORIGIN parameter | ✅ Used | ❌ Missing | 🟠 P1 |
| JWT Issuer/Audience params | ✅ Env vars | ❌ Missing | 🟠 P1 |
| ASPNETCORE_ENVIRONMENT | ✅ Set per service | ⚠️ Parameter only | 🟠 P1 |
| Cache connection secret | ✅ Redis | ⚠️ Not referenced | 🟠 P1 |

---

## Validation Checklist

- [ ] JWT secret key parameter added to main.bicep
- [ ] JWT issuer/audience parameters added
- [ ] FRONTEND_ORIGIN parameter added
- [ ] Key Vault module called from main.bicep with enableKeyVault=true
- [ ] Redis module called from main.bicep
- [ ] SQL Server module called with 6 database array
- [ ] Auth service module created and called
- [ ] Billing service module created and called
- [ ] Inventory service module created and called
- [ ] Orders service module created and called
- [ ] Purchasing service module created and called
- [ ] Sales service module created and called
- [ ] API Gateway module created and called
- [ ] All services configured with JWT environment variables
- [ ] All services reference Key Vault secrets for connections
- [ ] All services configured with Dapr if needed
- [ ] Gateway configured with external ingress
- [ ] All services reference cache connection
- [ ] All services reference their service database connection
- [ ] `az bicep validate` passes all files
- [ ] `az deployment group validate` passes

---

**Reference Date:** October 27, 2025  
**Status:** Complete mapping - Ready for implementation

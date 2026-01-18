# Naming Conventions - ERP Backend

Aquest document defineix la nomenclatura consistent utilitzada a tot el projecte per evitar confusions i errors.

## Variables Principals

### Entorn (Environment)
- **Variable d'entorn**: `AZURE_ENV_NAME` (workflow) = `"dev"` (per defecte)
- **Paràmetre Bicep**: `environmentName` (azure.yaml) = `${AZURE_ENV_NAME=dev}` (per defecte)
- **Slug calculat**: `envSlug = toLower(replace(environmentName, ' ', '-'))` → `"dev"` o `"production"`

### Prefix Base
- **Variable d'entorn**: `APP_PREFIX` (workflow) = `"myapp"` (per defecte)
- **Variable Bicep**: `namePrefix = 'myapp-${envSlug}'` → `"myapp-dev"` o `"myapp-production"`
- **Prefix base**: `basePrefix = replace(namePrefix, '-${envSlug}', '')` → `"myapp"`

## Noms de Serveis

Els noms de serveis han de coincidir **exactament** entre tots els fitxers:

| Servei | azure.yaml | Bicep (azdServiceName) | Workflow (SERVICE_NAME) |
|--------|------------|------------------------|-------------------------|
| Auth | `auth-service` | `auth-service` | `auth-service` |
| Billing | `billing-service` | `billing-service` | `billing-service` |
| Inventory | `inventory-service` | `inventory-service` | `inventory-service` |
| Orders | `orders-service` | `orders-service` | `orders-service` |
| Purchasing | `purchasing-service` | `purchasing-service` | `purchasing-service` |
| Sales | `sales-service` | `sales-service` | `sales-service` |
| API Gateway | `api-gateway` | `api-gateway` | `api-gateway` |

## Noms d'Imatges Docker

### Format
```
{APP_PREFIX}-{service-name}-{environment}
```

### Exemples (amb `APP_PREFIX=myapp`, `AZURE_ENV_NAME=dev`)
- `myapp-auth-service-dev`
- `myapp-billing-service-dev`
- `myapp-inventory-service-dev`
- `myapp-orders-service-dev`
- `myapp-purchasing-service-dev`
- `myapp-sales-service-dev`
- `myapp-api-gateway-dev`

### On es construeixen

**Workflow** (`.github/workflows/azure-build-deploy.yml`):
```bash
REPO_NAME="${APP_PREFIX}-${SERVICE_NAME}-${AZURE_ENV_NAME}"
# Exemple: myapp-auth-service-dev
```

**Bicep** (`infra/services/*-service.bicep`):
```bicep
var imageName = '${basePrefix}-{service-name}-${envSlug}'
# Exemple: myapp-auth-service-dev
```

**Important**: `AZURE_ENV_NAME` (workflow) i `envSlug` (Bicep) han de coincidir!

## Noms de Recursos Azure

### Resource Group
```
rg-{APP_PREFIX}-{AZURE_ENV_NAME}-core
```
Exemple: `rg-myapp-dev-core`

### Container Apps
```
{APP_PREFIX}-{AZURE_ENV_NAME}-{service-name}
```
Exemple: `myapp-dev-auth-service`

### Container Registry
```
{flatPrefix}containerregistry
```
On `flatPrefix = toLower(replace(namePrefix, '-', ''))` → `myappdev`

## Imatge Base Compartida

### Nom
```
{APP_PREFIX}-microservices-base
```
Exemple: `myapp-microservices-base`

### Tags
- `{IMAGE_TAG}` (commit hash, ex: `a1b2c3d`)
- `10.0` (versió .NET)
- `latest`

## Verificació de Consistència

Per assegurar que tot coincideix:

1. **Verificar `AZURE_ENV_NAME`**:
   - Workflow: `env.AZURE_ENV_NAME = "dev"`
   - azure.yaml: `environmentName: value: ${AZURE_ENV_NAME=dev}`

2. **Verificar `APP_PREFIX`**:
   - Workflow: `env.APP_PREFIX = "myapp"`
   - Bicep: `namePrefix = 'myapp-${envSlug}'` → `basePrefix = replace(namePrefix, '-${envSlug}', '')` → `"myapp"`

3. **Verificar noms de serveis**:
   - Tots els fitxers han d'utilitzar els mateixos noms de la taula anterior

4. **Verificar noms d'imatges**:
   - Workflow: `${APP_PREFIX}-${SERVICE_NAME}-${AZURE_ENV_NAME}`
   - Bicep: `${basePrefix}-{service-name}-${envSlug}`
   - Han de produir el mateix resultat!

## Exemples de Valors Reals

### Entorn Dev
- `AZURE_ENV_NAME = "dev"`
- `environmentName = "dev"`
- `envSlug = "dev"`
- `namePrefix = "myapp-dev"`
- `basePrefix = "myapp"`
- Imatges: `myapp-auth-service-dev`, `myapp-billing-service-dev`, etc.

### Entorn Production
- `AZURE_ENV_NAME = "production"` (o `"prod"`)
- `environmentName = "production"` (o `"prod"`)
- `envSlug = "production"` (o `"prod"`)
- `namePrefix = "myapp-production"` (o `"myapp-prod"`)
- `basePrefix = "myapp"`
- Imatges: `myapp-auth-service-production`, `myapp-billing-service-production`, etc.

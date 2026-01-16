# Shared Docker Base Images

## Overview

This directory contains shared Docker base images used across multiple microservices to:
- Reduce duplication
- Ensure consistency
- Improve build caching
- Simplify maintenance

## Base Images

### microservices-base.Dockerfile

**Purpose:** Common runtime base for all .NET 9 microservices (except Auth and Gateway)

**Used By:**
- Billing Service
- Inventory Service
- Orders Service
- Purchasing Service
- Sales Service

**Includes:**
- .NET 9 ASP.NET Core runtime
- curl (for health checks)
- ca-certificates (for HTTPS)
- libicu72 (internationalization support)
- Non-root user (appuser)
- Health check configuration
- Port 8080 exposed

**Build Command:**
```bash
docker build -f docker/microservices-base.Dockerfile -t myapp-microservices-base:9.0 .
```

## Service-Specific Dockerfiles

Each service's Dockerfile now follows this pattern:

```dockerfile
# Build stage (unchanged)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
# ... build steps ...

# Runtime stage (uses shared base)
FROM myapp-microservices-base:9.0 AS final
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.[Service].API.dll"]
```

## Benefits

1. **Consistency** - All services use identical runtime configuration
2. **Caching** - Base layer cached once, reused by all services
3. **Security** - Security updates applied in one place
4. **Size** - Shared layers reduce total image size
5. **Maintenance** - Update runtime config in one file

## Excluded Services

**Auth Service:** Has unique requirements (Identity, external auth providers)
**API Gateway:** Different purpose, uses Ocelot, different dependencies

These services maintain their own Dockerfiles.

## Build Order

1. Build shared base image first
2. Build individual services (they reference the base)

## CI/CD Integration

In your CI/CD pipeline:

```yaml
- name: Build shared base
  run: docker build -f docker/microservices-base.Dockerfile -t myapp-microservices-base:9.0 .

- name: Build services
  run: |
    docker build -f src/MyApp.Billing/MyApp.Billing.API/Dockerfile -t billing:latest .
    docker build -f src/MyApp.Inventory/MyApp.Inventory.API/Dockerfile -t inventory:latest .
    # ... etc
```

## Versioning

Tag base images with .NET version:
- `myapp-microservices-base:9.0` - .NET 9
- `myapp-microservices-base:11.0` - .NET 11 (future)

## Customization

Services can still override:
- ENTRYPOINT (always service-specific)
- Additional dependencies (via RUN commands)
- Environment variables
- Health check paths

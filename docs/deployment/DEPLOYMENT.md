# Azure Container Apps Deployment Guide

This guide explains how to deploy the ERP Microservices application to Azure Container Apps using Azure Developer CLI (azd).

## Architecture

The application consists of:
- **6 Microservices**: Auth, Billing, Inventory, Orders, Purchasing, Sales
- **YARP Gateway**: Reverse proxy with external ingress (HTTPS)
- **Azure SQL Database**: One database per microservice
- **Azure Cache for Redis**: Shared cache for all services
- **Dapr**: Service-to-service communication, pub/sub, and state management

### Service Configuration

| Service | Ingress | Dapr | App ID | Port |
|---------|---------|------|--------|------|
| Gateway | External (HTTPS) | No | - | 8080 |
| Auth | Internal | Yes | auth-service | 8080 |
| Billing | Internal | Yes | billing-service | 8080 |
| Inventory | Internal | Yes | inventory-service | 8080 |
| Orders | Internal | Yes | orders-service | 8080 |
| Purchasing | Internal | Yes | purchasing-service | 8080 |
| Sales | Internal | Yes | sales-service | 8080 |

## Prerequisites

1. **Azure Developer CLI (azd)**: [Install azd](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd)
2. **.NET 9.0 SDK**: [Install .NET](https://dotnet.microsoft.com/download)
3. **Docker**: [Install Docker](https://docs.docker.com/get-docker/)
4. **Azure Subscription**: Active Azure subscription with permissions to create resources

## Local Development

The local development environment uses Aspire with Docker containers for SQL Server and Redis:

```bash
# Run the application locally
cd AppHost
dotnet run
```

This will start:
- Redis container with RedisCommander and RedisInsight
- SQL Server container with persistent volumes
- All 6 microservices with Dapr sidecars
- YARP Gateway on port 5000

The Aspire dashboard will open automatically at `https://localhost:15888` (or similar).

## Azure Deployment

### One-Time Setup

1. **Initialize Azure Developer CLI**:
   ```bash
   azd init
   ```

2. **Set environment variables**:
   ```bash
   azd env set AZURE_ENV_NAME <your-environment-name>
   azd env set AZURE_LOCATION <azure-region>
   azd env set FRONTEND_ORIGIN <frontend-url>
   ```

3. **Set secrets**:
   ```bash
   azd env set SQL_ADMIN_PASSWORD <strong-password> --secret
   azd env set JWT_SECRET_KEY <jwt-secret-key> --secret
   ```

### Deploy to Azure

```bash
# Provision infrastructure and deploy all services
azd up
```

This command will:
1. Create a resource group
2. Provision Azure Container Apps Environment with Dapr
3. Create Azure SQL Server with 6 databases
4. Create Azure Cache for Redis
5. Create Azure Container Registry
6. Build and push Docker images for all services
7. Deploy all services to Container Apps
8. Configure ingress, secrets, and environment variables

### Individual Operations

```bash
# Only provision infrastructure
azd provision

# Only deploy code (after infrastructure exists)
azd deploy

# View environment details
azd env list

# Delete all resources
azd down
```

## CI/CD with GitHub Actions

The repository includes a GitHub Actions workflow (`.github/workflows/azure-deploy.yml`) for automated deployment.

### Setup GitHub Actions

1. **Configure OIDC authentication** (recommended):
   - Create an Azure AD App Registration
   - Configure federated credentials for GitHub
   - Set repository secrets:
     - `AZURE_CLIENT_ID`
     - `AZURE_TENANT_ID`
     - `AZURE_SUBSCRIPTION_ID`
     - `SQL_ADMIN_PASSWORD`
     - `JWT_SECRET_KEY`
   - Set repository variables:
     - `AZURE_ENV_NAME`
     - `AZURE_LOCATION`
     - `FRONTEND_ORIGIN`

2. **Push to main branch** to trigger deployment:
   ```bash
   git push origin main
   ```

## Access the Application

After deployment, you can access:

- **Gateway URL**: The output of `azd up` will show the gateway URL
- **Service URLs**: All services are internal-only, accessible via Dapr service invocation or through the gateway

```bash
# Get the gateway URL
azd env get-values | grep GATEWAY_URL
```

## Environment Variables

### Required Secrets
- `SQL_ADMIN_PASSWORD`: Password for Azure SQL Server admin account
- `JWT_SECRET_KEY`: Secret key for JWT token generation and validation

### Optional Variables
- `FRONTEND_ORIGIN`: Allowed CORS origins (default: `https://localhost:3000`)
- `AZURE_ENV_NAME`: Environment name used in resource naming
- `AZURE_LOCATION`: Azure region for deployment

## Service-to-Service Communication

Services communicate using Dapr service invocation:

```csharp
// Example: Calling the auth service from another service
var daprClient = new DaprClientBuilder().Build();
var result = await daprClient.InvokeMethodAsync<RequestDto, ResponseDto>(
    HttpMethod.Post,
    "auth-service",
    "api/auth/validate",
    request
);
```

## Monitoring and Logs

- **Azure Portal**: View logs and metrics in the Azure Portal
- **Log Analytics**: All services send logs to a shared Log Analytics workspace
- **Dapr Dashboard**: Available in the Container Apps Environment

```bash
# View logs for a specific service
az containerapp logs show \
  --name auth-service \
  --resource-group rg-<env-name> \
  --follow
```

## Scaling

Each service is configured with:
- **Min replicas**: 1
- **Max replicas**: 10
- **Scaling rule**: HTTP-based (100 concurrent requests per replica)

To customize scaling:
1. Edit `infra/core/host/container-app.bicep`
2. Modify the `minReplicas`, `maxReplicas`, or scaling rules
3. Redeploy: `azd deploy`

## Health Checks

All services expose a `/health` endpoint:
- **Liveness probe**: Checks if the service is running
- **Readiness probe**: Checks if the service is ready to accept traffic

## Troubleshooting

### Build Failures
```bash
# Check build logs
azd deploy --debug
```

### Service Not Starting
```bash
# Check service logs
az containerapp logs show --name <service-name> --resource-group rg-<env-name>
```

### Database Connection Issues
- Verify SQL Server firewall rules allow Container Apps
- Check connection strings in Container App environment variables
- Ensure SQL admin password is correct

### Dapr Issues
- Check Dapr sidecar logs in the Container App
- Verify Dapr components are configured in the Container Apps Environment
- Ensure `daprAppId` matches the service name

## Cost Optimization

To reduce costs in development environments:

1. **Use smaller SKUs**:
   - SQL: Basic tier
   - Redis: Basic C0
   - Container Registry: Basic

2. **Scale down when not in use**:
   ```bash
   # Scale all services to 0 replicas
   az containerapp update --name <service-name> --min-replicas 0
   ```

3. **Delete resources when not needed**:
   ```bash
   azd down
   ```

## Migration from Local to Production

The codebase is designed to work seamlessly in both environments:

- **Local**: Aspire manages containers and configuration
- **Production**: Azure Container Apps with managed services

No code changes are required. Environment variables and connection strings are automatically configured based on the deployment target.

## Additional Resources

- [Azure Container Apps Documentation](https://learn.microsoft.com/azure/container-apps/)
- [Azure Developer CLI Documentation](https://learn.microsoft.com/azure/developer/azure-developer-cli/)
- [Dapr on Azure Container Apps](https://learn.microsoft.com/azure/container-apps/dapr-overview)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)

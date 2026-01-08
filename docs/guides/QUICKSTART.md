# Quick Start Guide - Azure Deployment

This guide will get you deploying to Azure Container Apps in under 10 minutes.

## Prerequisites

Install these tools:
```bash
# Azure Developer CLI
curl -fsSL https://aka.ms/install-azd.sh | bash

# .NET SDK 10.0
# Download from: https://dotnet.microsoft.com/download/dotnet/10.0

# Docker Desktop
# Download from: https://www.docker.com/products/docker-desktop
```

## 5-Minute Deployment

### 1. Clone and Navigate
```bash
git clone https://github.com/Ozymandros/ERP.Microservices.git
cd ERP.Microservices
```

### 2. Initialize Azure Developer CLI
```bash
azd init
```

When prompted:
- Environment name: Choose any name (e.g., `erp-dev`)
- Azure location: Choose a region (e.g., `eastus`)

### 3. Set Required Secrets
```bash
# Set SQL Server admin password (must be strong)
azd env set SQL_ADMIN_PASSWORD "YourStrongPassword123!" --secret

# Set JWT secret key (generate a random string)
azd env set JWT_SECRET_KEY "your-super-secret-jwt-key-at-least-32-chars" --secret
```

### 4. (Optional) Set Frontend Origin
```bash
azd env set FRONTEND_ORIGIN "https://yourdomain.com;https://localhost:3000"
```

### 5. Deploy Everything
```bash
azd up
```

This single command will:
- ✅ Create Azure resources (Container Apps, SQL, Redis, etc.)
- ✅ Build Docker images for all 7 services
- ✅ Push images to Azure Container Registry
- ✅ Deploy all services to Azure Container Apps
- ✅ Configure Dapr, secrets, and networking

**Expected time**: 10-15 minutes

### 6. Get Your Gateway URL
```bash
azd env get-values | grep GATEWAY_URL
```

Visit the URL shown to access your deployed application!

## Local Development

Run locally with Aspire:
```bash
cd AppHost
dotnet run
```

Access the Aspire dashboard to monitor all services.

## Clean Up

Delete all Azure resources:
```bash
azd down
```

## Next Steps

- [Full Deployment Guide](../deployment/DEPLOYMENT.md) - Detailed deployment options
- [GitHub Actions Setup](#github-actions) - Automated CI/CD
- [Monitoring Guide](#monitoring) - View logs and metrics

## Troubleshooting

### "Authentication failed"
```bash
# Login to Azure
az login
azd auth login
```

### "Image build failed"
Ensure Docker is running:
```bash
docker ps
```

### "Deployment timeout"
Check service logs:
```bash
az containerapp logs show --name auth-service --resource-group rg-<env-name> --follow
```

## GitHub Actions Setup

For automated deployments on every push:

1. **Create Azure Service Principal** with OIDC:
```bash
azd pipeline config
```

2. **Follow the prompts** to:
   - Create a GitHub connection
   - Configure federated credentials
   - Set repository secrets

3. **Push to main** to trigger deployment:
```bash
git push origin main
```

The workflow in `.github/workflows/azure-deploy.yml` will run automatically.

## Common Commands

```bash
# Check deployment status
azd show

# View all environment variables
azd env get-values

# Update a secret
azd env set JWT_SECRET_KEY "new-secret" --secret

# Redeploy without reprovisioning
azd deploy

# View resource group in Azure Portal
az group show --name rg-<env-name> --query properties.portalUrl
```

## Architecture Overview

```
Internet
   |
   v
Gateway (External HTTPS)
   |
   +-- Auth Service (Internal, Dapr)
   +-- Billing Service (Internal, Dapr)
   +-- Inventory Service (Internal, Dapr)
   +-- Orders Service (Internal, Dapr)
   +-- Purchasing Service (Internal, Dapr)
   +-- Sales Service (Internal, Dapr)
        |
        +-- Azure SQL Database
        +-- Azure Cache for Redis
```

All services communicate internally via:
- HTTP through the gateway
- Dapr service invocation
- Shared Redis cache
- Individual SQL databases

## What Gets Deployed?

| Resource | Type | Purpose |
|----------|------|---------|
| Container Apps Environment | Microsoft.App/managedEnvironments | Hosts all services with Dapr |
| Gateway | Microsoft.App/containerApps | Public-facing reverse proxy |
| 6 Microservices | Microsoft.App/containerApps | Business logic services |
| SQL Server | Microsoft.Sql/servers | Database server |
| 6 SQL Databases | Microsoft.Sql/databases | One per microservice |
| Redis Cache | Microsoft.Cache/redis | Shared distributed cache |
| Container Registry | Microsoft.ContainerRegistry | Stores Docker images |
| Log Analytics | Microsoft.OperationalInsights | Centralized logging |

**Total**: ~15 Azure resources

## Cost Estimate

Development environment (Basic tier):
- Container Apps: ~$15/month
- SQL Databases (6x Basic): ~$30/month
- Redis (Basic): ~$15/month
- Other services: ~$10/month

**Total**: ~$70/month

> Tip: Use `azd down` when not using to avoid charges.

## Support

For issues:
1. Check [DEPLOYMENT.md](../deployment/DEPLOYMENT.md) for detailed troubleshooting
2. Review logs in Azure Portal
3. Open a GitHub issue

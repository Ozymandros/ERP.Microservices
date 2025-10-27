# Pre-Deployment Checklist

Before deploying to Azure Container Apps, ensure the following:

## ✅ Prerequisites Installed

- [ ] Azure Developer CLI (`azd`) - [Install](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd)
- [ ] .NET 9.0 SDK - [Install](https://dotnet.microsoft.com/download/dotnet/9.0)
- [ ] Docker Desktop (for local testing) - [Install](https://www.docker.com/products/docker-desktop)
- [ ] Azure CLI (`az`) - [Install](https://learn.microsoft.com/cli/azure/install-azure-cli)

## ✅ Azure Setup

- [ ] Active Azure subscription
- [ ] Sufficient quota for Container Apps in target region
- [ ] Permissions to create resources
- [ ] Logged in: `azd auth login` and `az login`

## ✅ Required Secrets

Generate and set these secrets before deployment:

```bash
# SQL Server admin password (must meet complexity requirements)
azd env set SQL_ADMIN_PASSWORD "YourComplexPassword123!" --secret

# JWT secret key (minimum 32 characters recommended)
azd env set JWT_SECRET_KEY "your-super-secret-jwt-key-at-least-32-characters-long" --secret
```

## ✅ Optional Configuration

```bash
# Frontend origin (CORS allowed origins)
azd env set FRONTEND_ORIGIN "https://yourdomain.com;https://localhost:3000"

# Choose Azure region
azd env set AZURE_LOCATION "eastus"

# Environment name (used in resource naming)
azd env set AZURE_ENV_NAME "erp-dev"
```

## ✅ Pre-Deployment Validation

1. **Build locally**:
```bash
cd /path/to/ERP.Microservices
dotnet restore
dotnet build
```

2. **Test locally** (optional but recommended):
```bash
cd AppHost
dotnet run
```
Visit the Aspire dashboard and verify all services start.

3. **Validate Bicep templates**:
```bash
cd infra
az bicep build --file main.bicep
```

## ✅ External Service Configuration

If using external OAuth providers, configure these **after** initial deployment:

### Google OAuth
```bash
azd env set AUTHENTICATION__GOOGLE__CLIENTID "your-client-id"
azd env set AUTHENTICATION__GOOGLE__CLIENTSECRET "your-secret" --secret
```

### Microsoft OAuth
```bash
azd env set AUTHENTICATION__MICROSOFT__CLIENTID "your-client-id"
azd env set AUTHENTICATION__MICROSOFT__CLIENTSECRET "your-secret" --secret
```

### GitHub OAuth
```bash
azd env set AUTHENTICATION__GITHUB__CLIENTID "your-client-id"
azd env set AUTHENTICATION__GITHUB__CLIENTSECRET "your-secret" --secret
```

### Apple OAuth
```bash
azd env set AUTHENTICATION__APPLE__CLIENTID "your-client-id"
azd env set AUTHENTICATION__APPLE__CLIENTSECRET "your-secret" --secret
```

Then redeploy: `azd deploy`

## ✅ Deployment

Once all checks pass:

```bash
# Deploy everything
azd up
```

Expected duration: 10-15 minutes

## ✅ Post-Deployment Verification

1. **Get the gateway URL**:
```bash
azd env get-values | grep GATEWAY_URL
```

2. **Test the gateway**:
```bash
curl https://<gateway-url>/weatherforecast
```

3. **Check service health**:
```bash
curl https://<gateway-url>/health
```

4. **View logs** (if issues):
```bash
# List container apps
az containerapp list --resource-group rg-<env-name> --output table

# View logs for a specific service
az containerapp logs show \
  --name auth-service \
  --resource-group rg-<env-name> \
  --follow
```

5. **Monitor in Azure Portal**:
```bash
# Get resource group portal URL
az group show --name rg-<env-name> --query properties.portalUrl -o tsv
```

## ✅ GitHub Actions (Optional)

For automated CI/CD:

1. **Configure pipeline**:
```bash
azd pipeline config
```

2. **Follow prompts** to set up:
   - GitHub connection
   - Federated credentials
   - Repository secrets

3. **Verify workflow**:
- Check `.github/workflows/azure-deploy.yml` exists
- Push to main branch to trigger deployment

## ❌ Troubleshooting

If deployment fails:

1. **Check error messages**:
```bash
azd up --debug
```

2. **Verify secrets are set**:
```bash
azd env get-values
```

3. **Check Azure quota**:
```bash
az vm list-usage --location <region> --output table
```

4. **Review resource group**:
```bash
az group show --name rg-<env-name>
```

5. **Clean up and retry**:
```bash
azd down --purge
azd up
```

## 📞 Support Resources

- [Azure Container Apps Docs](https://learn.microsoft.com/azure/container-apps/)
- [Azure Developer CLI Docs](https://learn.microsoft.com/azure/developer/azure-developer-cli/)
- [DEPLOYMENT.md](./DEPLOYMENT.md) - Full deployment guide
- [QUICKSTART.md](../guides/QUICKSTART.md) - Quick start guide
- [IMPLEMENTATION_SUMMARY.md](../implementation/IMPLEMENTATION_SUMMARY.md) - Technical details

## 🎯 Success Criteria

Deployment is successful when:
- ✅ All 7 container apps are running
- ✅ Gateway URL is accessible via HTTPS
- ✅ Health endpoints return 200 OK
- ✅ Services can communicate via Dapr
- ✅ Database connections work
- ✅ Redis cache is accessible

## 🧹 Cleanup

To delete all Azure resources and avoid charges:

```bash
azd down
```

This will:
- Delete the resource group
- Remove all Container Apps
- Delete SQL databases
- Remove Redis cache
- Clean up all Azure resources

⚠️ **Warning**: This is irreversible and will delete all data!

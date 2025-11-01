# Quick Start: Automated Build & Deploy

## The Problem (Fixed ✓)
```
ERROR: MANIFEST_UNKNOWN: manifest tagged by "latest" is not found
```
**Why:** Container Apps tried to pull images that weren't built yet.

## The Solution (Complete ✓)
Docker images are now **automatically built and pushed to ACR** before deployment.

---

## Local Development - 3 Steps

### Step 1: Build Images
```powershell
cd C:\Projects\ERP_ASPIRE_APP\src
./infra/scripts/build-push-images.ps1
```

**Output:**
```
✓ Successful: 7
✓ Failed: 0
✓ All images built and pushed successfully!
```

### Step 2: Deploy Infrastructure
```powershell
azd up
```

### Step 3: Verify Services
```powershell
# Check Container Apps
az containerapp list --resource-group rg-myapp-dev-core --output table

# Test service health
curl https://myapp-dev-auth-service.blueyellow.azurewebsites.net/health
```

---

## GitHub Actions - Fully Automatic ✓

**Setup:**
1. Add 3 secrets to GitHub repo:
   - `AZURE_CLIENT_ID`
   - `AZURE_TENANT_ID`
   - `AZURE_SUBSCRIPTION_ID`

2. Push to `main` or `develop` branch

**What happens automatically:**
- ✓ Builds all 7 services in ACR (parallel)
- ✓ Tags images with commit hash + `latest`
- ✓ Deploys with `azd deploy`
- ✓ Verifies all services are running
- ✓ Logs everything to GitHub Actions

**View status:**
- GitHub Actions tab shows progress
- Takes ~15-20 minutes total

---

## Files Created

| File | Purpose |
|------|---------|
| `infra/scripts/build-push-images.ps1` | PowerShell script for local builds |
| `.github/workflows/azure-build-deploy.yml` | GitHub Actions CI/CD pipeline |
| `infra/core/build/acr-build.bicep` | Bicep documentation module |
| `docs/deployment/BUILD_AND_DEPLOY_AUTOMATION.md` | Complete documentation |

---

## What Changed

### Before
```
azd up → Error: Images not found in ACR ✗
```

### After
```
Step 1: ./build-push-images.ps1  → Builds all images ✓
Step 2: azd up                    → Deploys with images ready ✓
Step 3: Services running          → Everything works ✓
```

---

## Troubleshooting

**Problem:** Script fails to authenticate
```powershell
az login
az account set --subscription "your-subscription-id"
```

**Problem:** Images still not found
```powershell
# Check if images were built
az acr repository list --name myappdevcontainerregistry
```

**Problem:** Deployment timeout
```powershell
# Check Container App status
az containerapp show --name myapp-dev-auth-service \
    --resource-group rg-myapp-dev-core \
    --query "properties.latestRevisionFqdn"
```

---

## Next Steps

✅ **Immediate:**
```powershell
./infra/scripts/build-push-images.ps1
azd up
```

✅ **Optional - Setup GitHub Actions:**
- Add secrets (AZURE_CLIENT_ID, etc.)
- Push to GitHub
- Automatic CI/CD pipeline starts

---

## Key Benefits

✓ **Works locally** - Build images before deploying  
✓ **Works in CI/CD** - Fully automated with GitHub Actions  
✓ **No manual steps** - PowerShell script handles everything  
✓ **Fast** - Parallel ACR builds (faster than sequential Docker builds)  
✓ **Traceable** - Images tagged with commit hash  
✓ **Reliable** - Bicep + Bicep modules ensure consistency  

---

**Ready to deploy?**
```powershell
./infra/scripts/build-push-images.ps1 && azd up
```

See `docs/deployment/BUILD_AND_DEPLOY_AUTOMATION.md` for details.

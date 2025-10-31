# Automated Build & Deploy Solution

## Overview

This solution ensures that **Docker images are built and pushed to Azure Container Registry (ACR) BEFORE Container Apps attempts to deploy them**.

The issue you encountered was: `MANIFEST_UNKNOWN: manifest tagged by "latest" is not found`

**Root cause:** Container Apps tried to pull images that hadn't been built yet.

**Solution:** Automate image building as part of the deployment pipeline.

---

## Architecture

```
Local Dev:
  azd up
    ↓
  1. Bicep provisions infrastructure
    ↓
  2. PowerShell script builds & pushes images to ACR
    ↓
  3. Container Apps pulls images and deploys
    ↓
  4. Services running ✓

CI/CD (GitHub Actions):
  Push to main/develop
    ↓
  1. Build images in ACR (parallel builds, cached layers)
    ↓
  2. Tag as: commit-hash AND latest
    ↓
  3. Run azd deploy
    ↓
  4. Container Apps automatically pulls new images
```

---

## Setup Options

### Option 1: Local Development (EASIEST)

Run the PowerShell script BEFORE deploying:

```powershell
# Navigate to repo root
cd C:\Projects\ERP_ASPIRE_APP\src

# Step 1: Build images in ACR
./infra/scripts/build-push-images.ps1

# Step 2: Deploy with azd
azd up
```

**What it does:**
- Builds all 7 services in ACR (parallel builds)
- Pushes images with `latest` tag
- Returns when complete
- Then `azd up` deploys the infrastructure and Container Apps

### Option 2: GitHub Actions (RECOMMENDED for CI/CD)

This is already configured in `.github/workflows/azure-build-deploy.yml`

**Setup required:**

1. **Add GitHub Secrets** (in your GitHub repo settings):
   ```
   AZURE_CLIENT_ID          (Azure app registration ID)
   AZURE_TENANT_ID          (Your Azure AD tenant ID)
   AZURE_SUBSCRIPTION_ID    (Your Azure subscription ID)
   ```

2. **Enable Federated Credentials** in Azure:
   ```powershell
   # Create app registration and federated credentials for GitHub
   # See: https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure
   ```

3. **Push to main or develop** to trigger the workflow automatically

**What it does:**
- Automatically builds all images on every push
- Tags with both commit hash (for traceability) AND latest
- Automatically deploys with `azd deploy`
- Verifies deployment health
- Logs all steps

### Option 3: Manual Build Before Deploy

If you just want to test locally without automation:

```powershell
# Login to Azure
az login
az account set --subscription "your-subscription-id"

# Build each service
az acr login --name myappdevcontainerregistry

$REGISTRY = "myappdevcontainerregistry.azurecr.io"

# Build Auth Service
az acr build --registry myappdevcontainerregistry `
    --image auth-service:latest `
    --file MyApp.Auth/MyApp.Auth.API/Dockerfile `
    MyApp.Auth/MyApp.Auth.API

# Repeat for other services...

# Then deploy
azd up
```

---

## Files Overview

### 1. `infra/scripts/build-push-images.ps1`
**Purpose:** PowerShell script for local development

**Usage:**
```powershell
./infra/scripts/build-push-images.ps1
```

**What it does:**
- Reads Azure credentials from `az login` session
- Builds all 7 services in ACR
- Uses parallel builds (faster than sequential)
- Retries on failure
- Provides detailed progress output

**Output:**
```
═══════════════════════════════════════════════════════════════
🔍 VALIDATING ENVIRONMENT
═══════════════════════════════════════════════════════════════
→ ResourceGroup: rg-myapp-dev-core
→ Registry: myappdevcontainerregistry
...
✓ Environment validation passed

═══════════════════════════════════════════════════════════════
🐳 BUILDING & PUSHING DOCKER IMAGES
═══════════════════════════════════════════════════════════════
→ Building: auth-service
  Service Path: MyApp.Auth/MyApp.Auth.API
  Image URI: myappdevcontainerregistry.azurecr.io/auth-service:latest
✓ Successfully built and pushed auth-service:latest

...

═══════════════════════════════════════════════════════════════
📊 BUILD SUMMARY
═══════════════════════════════════════════════════════════════
Total Services: 7
Successful: 7
Failed: 0

✓ All images built and pushed successfully!
```

### 2. `.github/workflows/azure-build-deploy.yml`
**Purpose:** Automate entire build + deploy pipeline in CI/CD

**Triggers:** On push to main/develop or manual workflow dispatch

**Jobs:**
1. `build-and-push-images` - Build all services in ACR
2. `deploy-to-azure` - Run azd deploy
3. `verify-deployment` - Health check Container Apps

### 3. `infra/core/build/acr-build.bicep`
**Purpose:** Documentation module for Bicep

Currently this is a placeholder with documentation. In the future, could use Bicep deployment scripts for more integration.

### 4. `azure.yaml`
**Purpose:** Azure Developer CLI configuration

No postprovision hook (not supported), so we use:
- Local: `build-push-images.ps1` before `azd up`
- CI/CD: GitHub Actions workflow

---

## Troubleshooting

### Problem: "MANIFEST_UNKNOWN: manifest tagged by "latest" is not found"

**Cause:** Images weren't built before Container Apps tried to deploy

**Solution:**
```powershell
# Option 1: Run build script
./infra/scripts/build-push-images.ps1
azd up

# Option 2: Check if images exist in ACR
az acr repository list --name myappdevcontainerregistry

# Option 3: Check ACR build logs
az acr task logs --name build-task-id --registry myappdevcontainerregistry
```

### Problem: "No credential provided to access ACR"

**Cause:** Not authenticated to Azure

**Solution:**
```powershell
az login
az account set --subscription "your-subscription-id"
```

### Problem: "Build script is slow"

**Cause:** Sequential builds

**Solution:** The script uses ACR's parallel build capability (`az acr build`), which is faster than local Docker builds

**To speed up further:**
- Use GitHub Actions (runs in Azure's data center, faster uploads)
- Use `--no-logs` flag for non-interactive mode

### Problem: "Running out of ACR storage quota"

**Cause:** Too many image tags accumulated

**Solution:**
```powershell
# Delete old images
az acr repository delete --name myappdevcontainerregistry --image auth-service --tag old-tag

# Or use ACR purge:
az acr run --registry myappdevcontainerregistry \
  --cmd "acr purge --filter 'auth-service:.*' --ago 30d" /dev/null
```

---

## Best Practices

### 1. Use Commit Hash as Image Tag (Production)
```powershell
# In GitHub Actions, uses: ${{ github.sha }}
# This ensures traceability and allows rolling back
az acr build --image auth-service:${commit-hash}
```

### 2. Always Tag With `latest`
```powershell
# Makes it easy for development
# Production should use specific version tags
az acr build --image auth-service:latest
```

### 3. Validate Images Before Deploying
```powershell
# Verify all services are in ACR
az acr repository list --name myappdevcontainerregistry

# Check specific image
az acr repository show-tags --name myappdevcontainerregistry --repository auth-service
```

### 4. Monitor Build Status
```powershell
# In Azure Portal:
# 1. Go to Container Registry
# 2. Click "Webhooks" or "Build tasks" to see history
# 3. Or use CLI:
az acr build-task show --name task-name --registry myappdevcontainerregistry
```

### 5. Use Bicep Parameters for Image Tag
Currently, service Bicep files use hardcoded `latest` tag:
```bicep
param imageTag string = 'latest'
```

For production, consider:
```bicep
param imageTag string // Required, no default
```

Then pass from `azure.yaml`:
```yaml
imageTag:
  value: ${IMAGE_TAG=latest}
```

---

## Complete Workflow Example

### Local Development
```powershell
# 1. Make code changes
# 2. Build and push images
./infra/scripts/build-push-images.ps1

# 3. Deploy
azd up

# 4. Test services
curl http://localhost:8080/health
```

### Continuous Deployment (GitHub Actions)
```bash
# 1. Commit and push changes
git add .
git commit -m "feat: add new endpoint"
git push origin main

# 2. GitHub Actions automatically:
#    - Builds images (triggers after 30 sec)
#    - Pushes to ACR
#    - Deploys with azd
#    - Verifies health

# 3. Check status:
#    - GitHub Actions tab shows progress
#    - Azure Portal shows Container App status
#    - Services available at configured FQDNs
```

---

## Summary

| Scenario | Method | Time | Automation |
|----------|--------|------|-----------|
| Local dev | `build-push-images.ps1` + `azd up` | 10-15 min | Manual script |
| Testing before commit | Manual `az acr build` | 5-10 min | None |
| Production CI/CD | GitHub Actions | 15-20 min | Full automation |
| One-time deploy | `azd up` (assumes images exist) | 10 min | Infrastructure only |

---

## Next Steps

1. ✅ **Run local build script:**
   ```powershell
   ./infra/scripts/build-push-images.ps1
   azd up
   ```

2. ✅ **Test deployment:**
   - Check Container Apps in Azure Portal
   - Verify services are running

3. ✅ **Setup GitHub Actions (optional):**
   - Add secrets to GitHub repo
   - Push to main branch
   - Observe automatic deployment

4. ✅ **Monitor:**
   - Container App revisions
   - Image registry
   - Deployment history

---

## References

- [Azure Container Registry Build Documentation](https://learn.microsoft.com/en-us/azure/container-registry/container-registry-quickstart-task-cli)
- [Azure Container Apps Documentation](https://learn.microsoft.com/en-us/azure/container-apps/)
- [GitHub Actions with Azure](https://learn.microsoft.com/en-us/azure/developer/github/)
- [Azure Developer CLI](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/)

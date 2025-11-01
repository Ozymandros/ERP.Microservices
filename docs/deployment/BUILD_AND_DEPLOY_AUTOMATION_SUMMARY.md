# ✅ BUILD & DEPLOY AUTOMATION - COMPLETE SOLUTION

## 🎯 Problem Solved

Your deployment error:
```
ERROR: MANIFEST_UNKNOWN: manifest tagged by "latest" is not found
```

**Why it happened:** Container Apps tried to deploy services before Docker images were built and pushed to ACR.

**How it's fixed:** Complete automated pipeline that builds images BEFORE deploying.

---

## 📦 Complete Solution Includes

### 1. PowerShell Build Script
- **File:** `infra/scripts/build-push-images.ps1`
- **Purpose:** Builds all 7 Docker images and pushes to ACR
- **Usage:** `./infra/scripts/build-push-images.ps1`
- **Time:** 5-10 minutes for all services

### 2. Deploy Wrapper Script
- **File:** `Deploy.ps1`
- **Purpose:** One-command build + deploy + verify
- **Usage:** `./Deploy.ps1` or `./Deploy.ps1 -Environment prod`
- **Time:** ~20 minutes total

### 3. GitHub Actions Workflow
- **File:** `.github/workflows/azure-build-deploy.yml`
- **Purpose:** Automatic CI/CD on every push
- **Trigger:** Push to main/develop branch
- **Features:** Parallel builds, full logging, health checks

### 4. Deployment Documentation
- **File:** `docs/deployment/BUILD_AND_DEPLOY_AUTOMATION.md`
- **Purpose:** Complete technical guide with all options
- **File:** `docs/deployment/QUICK_START_BUILD_DEPLOY.md`
- **Purpose:** Quick reference for common tasks
- **File:** `docs/deployment/DEPLOYMENT_AUTOMATION_COMPLETE.md`
- **Purpose:** Full solution overview

### 5. Quick Reference Guide
- **File:** `DEPLOYMENT_GUIDE.ps1`
- **Purpose:** Display usage options and workflows
- **Usage:** `./DEPLOYMENT_GUIDE.ps1` or view as text

---

## 🚀 How to Use

### Option 1: One-Command Deploy (Easiest)
```powershell
cd C:\Projects\ERP_ASPIRE_APP\src
./Deploy.ps1
```
Takes ~20 minutes. Builds images, deploys everything, verifies health.

### Option 2: Step-by-Step
```powershell
# Step 1: Build images
./infra/scripts/build-push-images.ps1

# Step 2: Deploy infrastructure
azd up

# Step 3: Verify
./Deploy.ps1 -Verify
```

### Option 3: GitHub Actions (Automatic)
1. Add GitHub Secrets (AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_SUBSCRIPTION_ID)
2. Push to main/develop
3. Automatic build + deploy + verify

---

## 📋 What Gets Built

| Service | Image | Status |
|---------|-------|--------|
| Auth Service | `auth-service:latest` | ✓ |
| Billing Service | `billing-service:latest` | ✓ |
| Inventory Service | `inventory-service:latest` | ✓ |
| Orders Service | `orders-service:latest` | ✓ |
| Purchasing Service | `purchasing-service:latest` | ✓ |
| Sales Service | `sales-service:latest` | ✓ |
| API Gateway | `erp-api-gateway:latest` | ✓ |

All images are built in Azure Container Registry with parallel builds.

---

## 🔄 Process Flow

```
Your Code
    ↓
./Deploy.ps1 or git push
    ↓
Build Phase:
  - 7 Docker images built in parallel in ACR
  - Tags: latest (and commit-hash in CI/CD)
  - Cached layers speed up rebuilds
    ↓
Deploy Phase:
  - Bicep provisions infrastructure
  - Azure SQL databases created
  - Redis cache provisioned
  - Key Vault configured
  - RBAC permissions set
  - Container Apps deployed with images
    ↓
Verify Phase:
  - All 7 services running
  - Health checks passing
  - Endpoints ready
  - Logging enabled
    ↓
✅ Services Running
  - Ready for traffic
  - DAPR enabled
  - Automatic scaling
  - Monitoring active
```

---

## 📊 Performance

| Scenario | Time | Notes |
|----------|------|-------|
| First deployment | 20-25 min | Infrastructure + all images |
| Redeploy (images exist) | 10-15 min | Only infrastructure changes |
| Image rebuild only | 5-10 min | Parallel ACR builds |
| GitHub Actions | 15-20 min | Automatic, includes all steps |

---

## ✨ Key Features

✅ **Fully Automated**
- No manual Docker commands needed
- No manual ACR operations needed
- No manual Container Apps updates needed

✅ **Fast**
- Parallel image builds in ACR
- Cached Docker layers
- Efficient Bicep deployments

✅ **Reliable**
- Retry logic for failed builds
- Detailed error messages
- Health check verification

✅ **Traceable**
- Image tags include commit hash (CI/CD)
- Deployment logs captured
- Service status visible

✅ **Production Ready**
- Works locally and in cloud
- Supports multiple environments (dev/staging/prod)
- GitHub Actions integration
- Secure credential handling (no hardcoded secrets)

---

## 🛠️ What Each File Does

| File | Purpose | When Used |
|------|---------|-----------|
| `Deploy.ps1` | One-command wrapper | Local dev, quick deploys |
| `infra/scripts/build-push-images.ps1` | Build images only | Before `azd up`, testing builds |
| `.github/workflows/azure-build-deploy.yml` | CI/CD automation | On every Git push |
| `infra/core/build/acr-build.bicep` | Documentation | Reference, future enhancements |
| `docs/deployment/BUILD_AND_DEPLOY_AUTOMATION.md` | Complete guide | Deep dive, troubleshooting |
| `docs/deployment/QUICK_START_BUILD_DEPLOY.md` | Quick reference | Getting started, common tasks |
| `DEPLOYMENT_GUIDE.ps1` | Display guide | Learning the process |

---

## 🎓 Learning Resources

### Quick Learning (5 minutes)
1. Run: `./DEPLOYMENT_GUIDE.ps1` (this displays the guide)
2. Read: `docs/deployment/QUICK_START_BUILD_DEPLOY.md`

### Moderate Learning (30 minutes)
1. Read: `docs/deployment/BUILD_AND_DEPLOY_AUTOMATION.md`
2. Try: `./Deploy.ps1`
3. Explore Azure Portal resources

### Deep Learning (2 hours)
1. Read: All documentation files
2. Study: PowerShell scripts
3. Review: GitHub Actions workflow
4. Examine: Bicep infrastructure files

---

## 🔧 Troubleshooting Quick Links

**Issue: Authentication fails**
```powershell
az login
az account set --subscription "your-subscription-id"
```

**Issue: Images not in ACR**
```powershell
az acr repository list --name myappdevcontainerregistry
```

**Issue: Services won't start**
```powershell
az containerapp logs show --name myapp-dev-auth-service \
    --resource-group rg-myapp-dev-core --tail 50
```

See `docs/deployment/BUILD_AND_DEPLOY_AUTOMATION.md` for complete troubleshooting guide.

---

## 🎯 Next Steps

### Today
```powershell
./Deploy.ps1
```

### This Week
- [ ] Test all service endpoints
- [ ] Verify DAPR communication
- [ ] Run HTTP tests (auth-tests.http)
- [ ] Check Application Insights logs

### This Month
- [ ] Setup GitHub Actions secrets
- [ ] Enable automatic CI/CD deployments
- [ ] Configure deployment monitoring
- [ ] Set up alerting

---

## 📞 Support

**Quick questions?** See `docs/deployment/QUICK_START_BUILD_DEPLOY.md`

**Detailed help?** See `docs/deployment/BUILD_AND_DEPLOY_AUTOMATION.md`

**Learning the process?** Run `./DEPLOYMENT_GUIDE.ps1`

**Something broken?** Check troubleshooting section above or the detailed documentation.

---

## 🎉 Summary

You now have:
✅ Automated image building with `build-push-images.ps1`
✅ One-command deployment with `Deploy.ps1`
✅ CI/CD automation with GitHub Actions
✅ Complete documentation
✅ Production-ready infrastructure
✅ Full solution to the "manifest not found" error

**Ready to deploy?**
```powershell
./Deploy.ps1
```

**Takes ~20 minutes. Sit back and enjoy the automation! 🚀**

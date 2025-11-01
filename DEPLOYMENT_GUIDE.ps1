#!/usr/bin/env pwsh
# ============================================================================
# QUICK REFERENCE - How to Deploy
# ============================================================================
# This file explains the complete automated deployment solution
# ============================================================================

Write-Host @"
╔════════════════════════════════════════════════════════════════════════════╗
║                    🚀 AUTOMATED BUILD & DEPLOY SYSTEM                      ║
║                                                                            ║
║ Problem Fixed: "MANIFEST_UNKNOWN: manifest tagged by latest not found"    ║
║ Solution: Automatic image building before Container Apps deployment       ║
╚════════════════════════════════════════════════════════════════════════════╝

📋 DEPLOYMENT OPTIONS

┌─ OPTION 1: One-Command Deploy (EASIEST) ───────────────────────────────────┐
│                                                                              │
│  Command: ./Deploy.ps1                                                      │
│                                                                              │
│  What it does:                                                              │
│    1. Builds all 7 Docker images in ACR (parallel)                          │
│    2. Deploys infrastructure with azd                                       │
│    3. Verifies all services are healthy                                     │
│                                                                              │
│  Time: ~20 minutes                                                          │
│  For: Local development, testing, production deploys                        │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘

┌─ OPTION 2: Manual Steps (RECOMMENDED for learning) ───────────────────────────┐
│                                                                              │
│  Step 1: Build images                                                       │
│    ./infra/scripts/build-push-images.ps1                                    │
│                                                                              │
│  Step 2: Deploy                                                             │
│    azd up                                                                   │
│                                                                              │
│  Step 3: Verify                                                             │
│    ./Deploy.ps1 -Verify                                                     │
│                                                                              │
│  Time: ~20 minutes                                                          │
│  For: Understanding the process, troubleshooting                            │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘

┌─ OPTION 3: GitHub Actions (BEST for CI/CD) ────────────────────────────────┐
│                                                                              │
│  Setup (one-time):                                                          │
│    1. Add 3 GitHub Secrets:                                                 │
│       - AZURE_CLIENT_ID                                                     │
│       - AZURE_TENANT_ID                                                     │
│       - AZURE_SUBSCRIPTION_ID                                               │
│    2. Enable Federated Credentials in Azure                                 │
│                                                                              │
│  Usage: git push origin main                                                │
│                                                                              │
│  What it does automatically:                                                │
│    1. Builds images on every push                                           │
│    2. Tags with commit hash                                                 │
│    3. Deploys to Azure                                                      │
│    4. Sends notifications                                                   │
│                                                                              │
│  Time: ~20 minutes (automatic)                                              │
│  For: Production deployments, continuous delivery                           │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘

🎯 QUICK START

1. Prerequisites:
   ✓ Azure CLI installed (az --version)
   ✓ Logged in to Azure (az login)
   ✓ azd CLI installed (azd version)

2. Run deployment:
   cd C:\Projects\ERP_ASPIRE_APP\src
   ./Deploy.ps1

3. Wait for completion (~20 minutes)

4. Test services:
   - Check Azure Portal for Container App status
   - Or run: ./Deploy.ps1 -Verify

📦 SERVICES DEPLOYED

✓ Auth Service           (myapp-dev-auth-service)
✓ Billing Service       (myapp-dev-billing-service)
✓ Inventory Service     (myapp-dev-inventory-service)
✓ Orders Service        (myapp-dev-orders-service)
✓ Purchasing Service    (myapp-dev-purchasing-service)
✓ Sales Service         (myapp-dev-sales-service)
✓ API Gateway           (myapp-dev-api-gateway)

📂 FILES CREATED

New Files:
  • infra/scripts/build-push-images.ps1
    → PowerShell script to build images locally

  • Deploy.ps1
    → Wrapper script for build + deploy + verify

  • .github/workflows/azure-build-deploy.yml
    → GitHub Actions CI/CD pipeline

  • docs/deployment/BUILD_AND_DEPLOY_AUTOMATION.md
    → Complete technical documentation

  • docs/deployment/QUICK_START_BUILD_DEPLOY.md
    → Quick reference guide

  • docs/deployment/DEPLOYMENT_AUTOMATION_COMPLETE.md
    → Full solution overview

🔍 VERIFY DEPLOYMENT

After running ./Deploy.ps1, verify:

1. Check Container Apps:
   az containerapp list --resource-group rg-myapp-dev-core --output table

2. Check service status:
   ./Deploy.ps1 -Verify

3. View logs:
   az containerapp logs show --name myapp-dev-auth-service \
       --resource-group rg-myapp-dev-core

4. Test endpoints:
   curl https://myapp-dev-api-gateway.<region>.containerapp.io/health

⚙️ TROUBLESHOOTING

Problem: Authentication fails
  Solution: az login && az account set --subscription YOUR_SUB_ID

Problem: Images not found
  Solution: 
    1. Check: az acr repository list --name myappdevcontainerregistry
    2. Rebuild: ./infra/scripts/build-push-images.ps1

Problem: Container Apps won't start
  Solution:
    1. Check logs: az containerapp logs show --name SERVICE_NAME ...
    2. Check revisions: az containerapp revision list --name SERVICE_NAME ...

Problem: Deployment timeout
  Solution:
    1. Wait longer (first deploy takes ~20 minutes)
    2. Check: azd env get-values | grep AZURE_ENV_NAME

📚 DOCUMENTATION

For more details, see:
  • docs/deployment/BUILD_AND_DEPLOY_AUTOMATION.md (technical guide)
  • docs/deployment/QUICK_START_BUILD_DEPLOY.md (quick reference)
  • docs/deployment/DEPLOYMENT_AUTOMATION_COMPLETE.md (solution overview)

🚀 COMMON WORKFLOWS

Workflow 1: Local Development
  ./Deploy.ps1              # Full build + deploy

Workflow 2: Only Deploy (Images Already Built)
  ./Deploy.ps1 -SkipBuild   # Skip build, just deploy

Workflow 3: Just Verify Status
  ./Deploy.ps1 -Verify      # Check service health

Workflow 4: Redeploy to Production
  ./Deploy.ps1 -Environment prod

✅ WHAT'S FIXED

❌ Before:
  azd up → ERROR: MANIFEST_UNKNOWN: manifest not found

✅ After:
  ./Deploy.ps1 → All services deployed successfully

🎓 HOW IT WORKS

1. BUILD PHASE:
   - Reads Dockerfile from each service
   - Submits build to Azure Container Registry
   - ACR builds in parallel (faster)
   - Images tagged with 'latest'
   - Returns when all images are in ACR

2. DEPLOY PHASE:
   - Bicep provisions infrastructure
   - Creates Azure resources (SQL, Redis, Key Vault, etc.)
   - Deploys Container Apps with image references
   - Container Apps pulls images from ACR
   - Services start and pass health checks

3. VERIFY PHASE:
   - Checks each Container App status
   - Confirms services are running
   - Reports FQDNs and endpoints

📞 NEXT STEPS

1. NOW:
   ./Deploy.ps1

2. AFTER DEPLOYMENT:
   - Visit Azure Portal to see resources
   - Check Container Apps health
   - Test service endpoints

3. FOR CI/CD:
   - Add GitHub Secrets (one-time setup)
   - Push to main branch
   - Enjoy automatic deployments!

════════════════════════════════════════════════════════════════════════════════
Ready? Run: ./Deploy.ps1
Questions? See: docs/deployment/BUILD_AND_DEPLOY_AUTOMATION.md
════════════════════════════════════════════════════════════════════════════════
"@

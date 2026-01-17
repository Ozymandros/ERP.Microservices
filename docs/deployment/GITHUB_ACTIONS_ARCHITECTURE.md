# 🚀 GitHub Actions Workflow Architecture

**Complete CI/CD Pipeline Architecture for ERP Microservices**  
Last Updated: October 31, 2025

---

## 📋 Overview

All workflows follow the **optimized three-phase pattern**:
1. **Phase 1 (Test)** - Run in parallel
2. **Phase 2 (Build Images)** - Build 7 services in parallel using matrix strategy
3. **Phase 3 (Deploy)** - Deploy after all images ready

---

## 🔄 Workflow Triggers & Branches

```
┌─────────────────────────────────────────────────────────────┐
│ GITHUB ACTION TRIGGERS                                      │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Pull Request                    Push to Branch            │
│  ├─ Triggers: dotnet.yml         ├─ Triggers: workflow    │
│  ├─ On: develop, main            ├─ Branch: develop       │
│  └─ Actions:                     └─ Actions:              │
│     ✅ Restore                       ✅ Restore           │
│     ✅ Build                         ✅ Build             │
│     ✅ Test                          ✅ Test              │
│     ❌ Deploy                        ✅ Build Images      │
│                                     ✅ Deploy to Dev     │
│                                                             │
│                     Push to Main                            │
│                     ├─ Triggers: azure-deploy.yml          │
│                     ├─ Actions:                            │
│                     │  ✅ Restore                          │
│                     │  ✅ Build (strict: fail on error)    │
│                     │  ✅ Test (strict: fail on error)    │
│                     │  ✅ Build Images                    │
│                     │  ✅ Deploy to Production            │
│                     └─ Manual: workflow_dispatch           │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 📊 Workflow Comparison Table

| Workflow | Trigger | Environment | Test | Build | Deploy | Branch |
|----------|---------|-------------|------|-------|--------|--------|
| **dotnet.yml** | Pull Request | None | ✅ | ✅ | ❌ | develop/main |
| **azure-dev.yml** | Push | DEV | ✅ | ✅ | ✅ | develop |
| **azure-deploy.yml** | Push | PROD | ✅ Strict | ✅ Strict | ✅ | main |

---

## 🔀 Complete CI/CD Flow

```
Developer Creates Feature Branch
    ↓
Commits & Pushes to Feature Branch
    ↓
Creates Pull Request to develop
    ↓
┌─────────────────────────────────┐
│  dotnet.yml TRIGGERED           │
│  ────────────────────────────   │
│  Actions:                       │
│  1. Checkout code               │
│  2. Setup .NET 10.x             │
│  3. Cache NuGet packages        │
│  4. Restore dependencies        │
│  5. Build solution              │
│  6. Run unit tests              │
│                                 │
│  Result: ✅ All tests pass      │
└─────────────────────────────────┘
    ✅ Approval to Merge
    ↓
Merge to develop
    ↓
┌──────────────────────────────────────────────┐
│  azure-dev.yml TRIGGERED                     │
│  ────────────────────────────────────────── │
│  PHASE 1: TEST (runs in parallel)            │
│  ├─ Checkout                                 │
│  ├─ Setup .NET 10.x                          │
│  ├─ Cache NuGet packages                     │
│  ├─ Restore & Build solution                 │
│  └─ Run unit tests (continue on error)       │
│                                              │
│  PHASE 2: BUILD IMAGES (7 parallel jobs)     │
│  ├─ Azure login (federated credentials)      │
│  ├─ ACR login                                │
│  ├─ Matrix[0]: auth-service                  │
│  │  ├─ az acr build                          │
│  │  ├─ --image auth-service:latest           │
│  │  ├─ --image auth-service:<sha>            │
│  │  └─ --file src/.../Dockerfile             │
│  ├─ Matrix[1]: billing-service               │
│  ├─ Matrix[2]: inventory-service             │
│  ├─ Matrix[3]: orders-service                │
│  ├─ Matrix[4]: purchasing-service            │
│  ├─ Matrix[5]: sales-service                 │
│  └─ Matrix[6]: erp-api-gateway               │
│                                              │
│  PHASE 3: DEPLOY (after Phase 2)             │
│  ├─ Checkout                                 │
│  ├─ Install azd                              │
│  ├─ Azure login (federated)                  │
│  ├─ azd provision --no-prompt                │
│  ├─ azd deploy --no-prompt                   │
│  └─ Verify deployment                        │
│                                              │
│  Result: ✅ Services deployed to DEV         │
└──────────────────────────────────────────────┘
    ↓
Create PR from develop to main
    ↓
┌─────────────────────────────────┐
│  dotnet.yml TRIGGERED (again)   │
│  (Same as before)               │
│                                 │
│  Result: ✅ All tests pass      │
└─────────────────────────────────┘
    ✅ Approval & Merge to main
    ↓
Push to main
    ↓
┌──────────────────────────────────────────────┐
│  azure-deploy.yml TRIGGERED                  │
│  ────────────────────────────────────────── │
│  PHASE 1: TEST (strict: FAIL on error)       │
│  ├─ Restore & Build                          │
│  └─ Run unit tests (stop on failure)         │
│                                              │
│  PHASE 2: BUILD IMAGES (7 parallel jobs)     │
│  ├─ Same matrix as dev                       │
│  ├─ Push to PROD ACR                         │
│  └─ Tags: latest + commit SHA                │
│                                              │
│  PHASE 3: DEPLOY (strict)                    │
│  ├─ azd provision --no-prompt (PROD)         │
│  ├─ azd deploy --no-prompt (PROD)            │
│  └─ Verify deployment                        │
│                                              │
│  Result: ✅ Services deployed to PROD        │
└──────────────────────────────────────────────┘
    ↓
✅ All Services Running in Production
```

---

## 📦 Job Dependencies & Parallelization

### **dotnet.yml**
```
✓ Single job: test
  └─ Runs sequentially (restore → build → test)
```

### **azure-dev.yml & azure-deploy.yml**

```
Phase 1 (1 job)          Phase 2 (7 parallel jobs)          Phase 3 (1 job)
───────────────         ─────────────────────────          ──────────────
│                       │                                   │
├─ test                 ├─ build-images[0]                 ├─ deploy
│  ├─ restore           │   ├─ auth-service                │  ├─ checkout
│  ├─ build             │   ├─ az acr build                │  ├─ install azd
│  ├─ test              │   └─ push to ACR                 │  ├─ azure login
│  └─ ✅               │                                   │  ├─ provision
│                       ├─ build-images[1]                 │  ├─ deploy
│                       │   ├─ billing-service             │  └─ verify
│                       │   └─ ...                          │  └─ ✅
│                       │                                   │
│                       ├─ build-images[2]                 │
│                       │   ├─ inventory-service           │
│                       │   └─ ...                          │
│                       │                                   │
│                       ├─ build-images[3..6]              │
│                       │   └─ (remaining services)         │
│                       │   └─ ✅ all images ready          │
│                       │                                   │
                                                        ⬇️ needs: build-images


LEGEND:
✓ Runs independently
⬇️ Depends on previous phase
🔄 Parallel execution
```

---

## ⏱️ Estimated Execution Times

### **dotnet.yml (PR)**
```
Restore:     2 min
Build:       5 min
Test:        8 min
────────────────
Total:      ~15 min
```

### **azure-dev.yml (develop branch)**
```
Phase 1 (test):           ~15 min
Phase 2 (build × 7):      ~12 min (parallel, cached layers)
Phase 3 (provision+deploy): ~15 min
────────────────────────────────────
Total:                   ~42 min
```

### **azure-deploy.yml (main branch)**
```
Phase 1 (test strict):     ~15 min (strict: fails on error)
Phase 2 (build × 7):       ~12 min (parallel)
Phase 3 (provision+deploy): ~20 min (production setup)
─────────────────────────────────────
Total:                    ~47 min
```

---

## 🔐 Authentication & Permissions

### **Federated Credentials (OIDC)**
```yaml
Trigger: GitHub Actions pushes event
    ↓
Azure AD receives token request
    ↓
Validates: github.com certificate
    ↓
Validates: branch (develop/main)
    ↓
Validates: repository owner (Ozymandros)
    ↓
✅ Issues short-lived token (no secrets needed)
    ↓
Workflow uses token to authenticate to Azure
```

### **Secrets Used**
```
❌ AZURE_CLIENT_ID      → Uses vars (not secret)
❌ AZURE_TENANT_ID      → Uses vars (not secret)
❌ AZURE_SUBSCRIPTION_ID → Uses vars (not secret)
✅ SQL_ADMIN_PASSWORD    → Uses secrets (if needed)
✅ JWT_SECRET_KEY        → Uses secrets (if needed)
```

---

## 🐛 Debugging & Monitoring

### **Check Workflow Status**
```
GitHub UI: Settings → Actions → Workflows
Timeline shows:
  ✅ test job (15 min)
  ✅ build-images[0-6] (12 min parallel)
  ✅ deploy (15 min)
```

### **View Logs**
```
Each job has collapsible logs:
  - Checkout code
  - Setup .NET
  - Cache operations
  - Restore dependencies
  - Build solution
  - Run unit tests
  - Azure login
  - ACR login
  - az acr build commands
  - azd provision output
  - azd deploy output
```

### **Common Issues**

| Issue | Cause | Solution |
|-------|-------|----------|
| Tests fail | Code broken | Fix code before merge |
| Image build fails | Dockerfile error | Check Dockerfile path |
| Deploy fails | Infrastructure issue | Check azd output logs |
| Auth fails | Token expired | Verify federated credentials |
| ACR login fails | Registry mismatch | Check registry name in env |

---

## 📈 Optimization Features

### **1. Parallel Execution**
✅ Test job runs while waiting for user approval  
✅ 7 Docker image builds run simultaneously  
✅ Saves ~30 min compared to sequential builds  

### **2. Caching**
✅ NuGet package cache (key: csproj files)  
✅ ACR layer caching (automatic)  
✅ GitHub Actions cache across runs  

### **3. Matrix Strategy**
✅ One job definition, 7 parallel executions  
✅ Each service: independent build & push  
✅ Fail-fast: false → continues if one fails  

### **4. Federated Credentials**
✅ No stored secrets in GitHub  
✅ Automatic token generation per job  
✅ Time-limited tokens (1 hour)  

### **5. Conditional Error Handling**
```
DEV:  Tests can fail, deploy continues
PROD: Tests must pass, deploy stops on error
```

---

## 📝 Workflow Files Location

```
.github/workflows/
├── dotnet.yml                           # PR validation (15 min)
├── azure-dev.yml                        # Dev deployment (42 min)
├── azure-deploy.yml                     # Prod deployment (47 min)
└── azure-build-deploy.yml               # ⚠️ Legacy (can be removed)
```

---

## 🎯 Matrix Strategy Details

### **Service Matrix (Used in Phase 2)**

```yaml
strategy:
  matrix:
    service:
      - name: auth-service
        dockerfile: src/MyApp.Auth/MyApp.Auth.API/Dockerfile
      - name: billing-service
        dockerfile: src/MyApp.Billing/MyApp.Billing.API/Dockerfile
      - name: inventory-service
        dockerfile: src/MyApp.Inventory/MyApp.Inventory.API/Dockerfile
      - name: orders-service
        dockerfile: src/MyApp.Orders/MyApp.Orders.API/Dockerfile
      - name: purchasing-service
        dockerfile: src/MyApp.Purchasing/MyApp.Purchasing.API/Dockerfile
      - name: sales-service
        dockerfile: src/MyApp.Sales/MyApp.Sales.API/Dockerfile
      - name: erp-api-gateway
        dockerfile: src/ErpApiGateway/Dockerfile
  fail-fast: false  # Continue if one fails
```

**Execution:**
```
Job 1: auth-service       → starts immediately
Job 2: billing-service    → starts immediately
Job 3: inventory-service  → starts immediately
...
Job 7: erp-api-gateway    → starts immediately

All 7 run in parallel, each takes ~2 min
Total: ~2 min (not 14 min)
```

---

## 🔄 Image Tagging Strategy

Each image gets **two tags**:
```
auth-service:latest      ← Always points to latest build
auth-service:<commit-sha> ← Specific to this commit

Example:
  docker pull myappdevcontainerregistry.azurecr.io/auth-service:latest
  docker pull myappdevcontainerregistry.azurecr.io/auth-service:a1b2c3d
```

**Benefits:**
✅ Rollback to specific commit  
✅ Track which build is running  
✅ Production debugging easier  

---

## 📋 Phase Descriptions

### **Phase 1: Test**
```
✓ Restore NuGet packages (cache hit: ~10s)
✓ Build solution in Release mode (~5 min)
✓ Run all unit tests (~8 min)

Continue-on-error behavior:
  DEV:  If tests fail, continue to Phase 2
  PROD: If tests fail, STOP (no deploy)
```

### **Phase 2: Build Images**
```
For each service (7 total, in parallel):
  ✓ Checkout repository
  ✓ Azure federated login
  ✓ ACR login
  ✓ Execute: az acr build
      ├─ Dockerfile path
      ├─ Tag: latest
      ├─ Tag: commit-sha
      └─ Source: src/ directory

All 7 services push to ACR in parallel (~12 min)
```

### **Phase 3: Deploy**
```
✓ Checkout repository
✓ Install azd tool
✓ Azure federated login
✓ azd provision (infrastructure)
  ├─ Creates resource groups
  ├─ Creates storage
  ├─ Creates databases
  ├─ Creates container apps
  └─ (~5-10 min)
✓ azd deploy (services)
  ├─ Updates container app revisions
  ├─ Pulls images from ACR
  ├─ Deploys 7 services
  └─ (~5-10 min)
✓ Verify deployment (health checks)
```

---

## 🚀 Next Steps

1. **Monitor first deployment** - Watch workflow logs
2. **Set up branch protections** - Require checks to pass
3. **Configure alerts** - Get notified on failures
4. **Document environment variables** - For team reference
5. **Set up manual approval gates** - For production deployments

---

## 📞 Quick Reference

| Need | Workflow | Time | Trigger |
|------|----------|------|---------|
| **Test code** | dotnet.yml | 15 min | PR to develop/main |
| **Deploy to Dev** | azure-dev.yml | 42 min | Push to develop |
| **Deploy to Prod** | azure-deploy.yml | 47 min | Push to main |
| **Manual deploy** | Any workflow | - | workflow_dispatch |

---

**Last Updated:** October 31, 2025  
**Status:** ✅ Optimized & Production-Ready  
**Optimization:** 3-phase parallel execution with matrix builds

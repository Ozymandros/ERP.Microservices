# 🎯 GitHub Actions Workflows - Visual Diagrams

**Quick Visual Reference for CI/CD Pipeline**

---

## 1️⃣ Overall Workflow Trigger Strategy

```
┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃              GITHUB REPOSITORY EVENTS                 ┃
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

    ┌─────────────────┐
    │  Pull Request   │
    │ (any branch)    │
    └────────┬────────┘
             │ Triggers
             ▼
    ┌─────────────────────┐
    │   dotnet.yml        │
    │  (15 min)           │
    │ ✅ Restore          │
    │ ✅ Build            │
    │ ✅ Test             │
    │ ❌ No Deploy        │
    └─────────────────────┘


    ┌─────────────────┐
    │  Push to        │
    │  develop        │
    └────────┬────────┘
             │ Triggers
             ▼
    ┌─────────────────────┐
    │ azure-dev.yml       │
    │  (42 min)           │
    │ ✅ Test             │
    │ ✅ Build Images (7) │
    │ ✅ Deploy to DEV    │
    └─────────────────────┘


    ┌─────────────────┐
    │  Push to        │
    │  main           │
    └────────┬────────┘
             │ Triggers
             ▼
    ┌─────────────────────┐
    │ azure-deploy.yml    │
    │  (47 min)           │
    │ ✅ Test (strict)    │
    │ ✅ Build Images (7) │
    │ ✅ Deploy to PROD   │
    └─────────────────────┘
```

---

## 2️⃣ Development Workflow (Pull Request → Develop → Main)

```
┌──────────────────────────────────────────────────────────────────┐
│ Step 1: Developer creates feature branch & commits code          │
└──────────────────────────────────────────────────────────────────┘
    │
    ▼
┌──────────────────────────────────────────────────────────────────┐
│ Step 2: Push to GitHub & Create Pull Request                     │
└──────────────────────────────────────────────────────────────────┘
    │
    ├──────────────────────────────────────┐
    │                                      │
    ▼                                      ▼
    
  ┌─────────────────┐            ┌──────────────────────┐
  │  Branch exists  │            │ GitHub detects PR    │
  │  (local)        │            │                      │
  └────────┬────────┘            └──────────┬───────────┘
           │                               │
           ▼                               ▼
           
   CHECKOUT & SETUP              WORKFLOW TRIGGERED
   ────────────────────          ──────────────────
   
   ┌─────────────────────────┐   ┌──────────────────────────┐
   │  dotnet.yml             │   │  Branch: PR to develop   │
   │  (in GitHub)            │   │  Files changed detected  │
   │                         │   │                          │
   │  Job: test              │   │  Auto-triggered by GitHub│
   │  ├─ Restore (-2s ⚡)   │   │                          │
   │  ├─ Build (5 min)      │   └──────────┬───────────────┘
   │  ├─ Test (8 min)       │               │
   │  └─ ✅ PASS (15 min)  │               ▼
   └─────────────────────────┘
                                   ┌──────────────────────────┐
                                   │  GitHub Status Check     │
                                   │  (shows in PR)           │
                                   │                          │
                                   │  ✅ All checks passed    │
                                   │  "Ready to merge"        │
                                   └──────────┬───────────────┘
                                              │
                                              ▼
                                   ┌──────────────────────────┐
                                   │  Code Review (optional)  │
                                   │  - Reviewer approves     │
                                   │  - Maintainer merges     │
                                   └──────────┬───────────────┘
                                              │
                                              ▼

┌──────────────────────────────────────────────────────────────────┐
│ Step 3: Merge to develop (automatic workflow trigger)            │
└──────────────────────────────────────────────────────────────────┘
    │
    ├────────────────────────────────────────────────────────────┐
    │                                                            │
    ▼                                                            │
    
  ┌────────────────────────────────────────────────────────┐   │
  │  azure-dev.yml TRIGGERED                               │   │
  │  (Branch: develop)                                     │   │
  │  ◄═══════════════════════════════════════════════════ │   │
  │                                                        │   │
  │  PHASE 1: TEST (5 min)                                │   │
  │  ─────────────────────────                           │   │
  │  ├─ Checkout                                         │   │
  │  ├─ Setup .NET (8, 9)                               │   │
  │  ├─ Cache NuGet                                      │   │
  │  ├─ Restore & Build (Release)                        │   │
  │  └─ Run tests (continue-on-error: true)             │   │
  │     └─ ✅ Phase 1 complete                           │   │
  │                                                        │   │
  │  PHASE 2: BUILD IMAGES (7 parallel, 12 min)         │   │
  │  ──────────────────────────────────────────         │   │
  │  │                                                    │   │
  │  ├─ Job[0]: auth-service                           │   │
  │  │  ├─ Checkout                                     │   │
  │  │  ├─ Azure login (federated)                      │   │
  │  │  ├─ ACR login                                    │   │
  │  │  ├─ az acr build                                 │   │
  │  │  │  ├─ docker build (cache ⚡)                  │   │
  │  │  │  ├─ --image auth-service:latest              │   │
  │  │  │  ├─ --image auth-service:a1b2c3d             │   │
  │  │  │  └─ Push to ACR                               │   │
  │  │  └─ ✅ Complete                                  │   │
  │  │                                                    │   │
  │  ├─ Job[1]: billing-service   ┐                     │   │
  │  ├─ Job[2]: inventory-service │ All 7 in PARALLEL  │   │
  │  ├─ Job[3]: orders-service    │ (2 min each)       │   │
  │  ├─ Job[4]: purchasing-service│ = 12 min total    │   │
  │  ├─ Job[5]: sales-service     │ (not 14 min ⚡)   │   │
  │  └─ Job[6]: erp-api-gateway   ┘                     │   │
  │     └─ ✅ All images ready                          │   │
  │                                                        │   │
  │  PHASE 3: DEPLOY (15 min)                            │   │
  │  ─────────────────────                              │   │
  │  ├─ Checkout                                         │   │
  │  ├─ Install azd                                      │   │
  │  ├─ Azure login (federated)                          │   │
  │  ├─ azd provision                                    │   │
  │  │  └─ Create infrastructure (ResourceGroups, SQL,   │   │
  │  │     Redis, Container Apps, etc)                  │   │
  │  ├─ azd deploy                                       │   │
  │  │  └─ Pull images from ACR & deploy                │   │
  │  ├─ Verify deployment                               │   │
  │  └─ ✅ DEV Environment Ready                        │   │
  │                                                        │   │
  │  Total: 5 + 12 + 15 = 42 minutes                    │   │
  └────────────────────────────────────────────────────────┘   │
    │                                                           │
    └───────────────────────────────────────────────────────────┘
                        │
                        ▼

┌──────────────────────────────────────────────────────────────────┐
│ Step 4: Create PR from develop → main (same as Step 2)          │
└──────────────────────────────────────────────────────────────────┘
    │
    ▼
  ┌─────────────────────┐
  │  dotnet.yml         │
  │  (PR validation)    │
  │  ✅ Tests pass      │
  └────────┬────────────┘
           │
           ▼
    ┌────────────────┐
    │ PR Approved    │
    └────────┬───────┘
             │
             ▼

┌──────────────────────────────────────────────────────────────────┐
│ Step 5: Merge to main (PRODUCTION deployment)                   │
└──────────────────────────────────────────────────────────────────┘
    │
    ▼
  ┌────────────────────────────────────────────────────────┐
  │  azure-deploy.yml TRIGGERED                            │
  │  (Branch: main / PRODUCTION)                           │
  │  ◄═══════════════════════════════════════════════════ │
  │                                                        │
  │  PHASE 1: TEST (STRICT: must pass) (5 min)           │
  │  ─────────────────────────────────────────           │
  │  ├─ Continue-on-error: FALSE ← FAIL STOPS DEPLOY    │
  │  └─ ✅ All tests pass (required)                    │
  │                                                        │
  │  PHASE 2: BUILD IMAGES (12 min, 7 parallel)         │
  │  ──────────────────────────────────────────         │
  │  ├─ Same matrix as dev (auth, billing, etc)         │
  │  ├─ Push to PRODUCTION ACR (different registry)     │
  │  └─ ✅ All images tagged & pushed                   │
  │                                                        │
  │  PHASE 3: DEPLOY (20 min, strict)                   │
  │  ──────────────────────────────                    │
  │  ├─ azd provision (production setup)                │
  │  │  └─ Production resource group & configs         │
  │  ├─ azd deploy (production deployment)              │
  │  │  └─ Blue-green ready, canary capable            │
  │  └─ ✅ PRODUCTION Ready                            │
  │                                                        │
  │  Total: 5 + 12 + 20 = 47 minutes                    │
  └────────────────────────────────────────────────────────┘
    │
    ▼
  ┌──────────────────────────────────────┐
  │ ✅ ALL SERVICES IN PRODUCTION        │
  │ (7 services running at scale)        │
  └──────────────────────────────────────┘
```

---

## 3️⃣ Parallel Execution Timeline

```
DEVELOPMENT WORKFLOW (from develop branch push)

TIME    PHASE 1           PHASE 2 (Matrix)                   PHASE 3
────    ─────────         ──────────────────                ────────
0:00                      
        ┌───────┐         
        │ START │         
        │ test  │         
        │ job   │         
        │       │         
5:00    │  ✅   │         
        │ END   │         
        └───┬───┘         
            │         ┌────────────────────────────────┐
            │         │ PARALLEL BUILD MATRIX          │
            ├────────→│ (7 jobs simultaneous)          │
            │         │                                │
            │         │ auth-service                   │
            │         │ billing-service                │
            │         │ inventory-service              │
            │         │ orders-service                 │
            │         │ purchasing-service             │
            │         │ sales-service                  │
            │         │ erp-api-gateway                │
            │         │                                │
17:00       │         │ (Each ~2 min)                  │
            │         │ (All parallel = 2 min ⚡)      │
            │         │                                │
            │         └────┬───────────────────────────┘
            │              │
            │              │ All images ready
            │              │
            │              ├─────────────→┌────────────────┐
            │              │              │ PHASE 3: DEPLOY│
            │              │              │ (15 min)       │
            │              │              │ - azd provision│
            │              │              │ - azd deploy   │
            │              │              │ - verify       │
            │              │              │                │
32:00       │              │              │ ✅ COMPLETE   │
            │              │              └────────────────┘
            │              │
            └──────────────┘
                   42 min total

EXECUTION SAVINGS:
─────────────────
Sequential:     5 + (7×2) + 15 = 34 min + waiting = ~42 min
But if sequential: 5 + 14 + 15 = 34 min (without matrix benefits)

⚡ Matrix parallelization saves ~12 minutes!
```

---

## 4️⃣ Job Dependency Graph

```
azure-dev.yml & azure-deploy.yml structure:

┌─────────────────────────────────────────────────────────┐
│                                                         │
│  job: test                                              │
│  ├─ No dependencies                                     │
│  ├─ Starts immediately                                  │
│  └─ Completes in ~5 min                                │
│                                                         │
│  ┌──────────────────────────────┐                      │
│  │                              │                      │
│  ▼                              ▼                      │
│                                                         │
│  job: build-images             (needs: test)           │
│  ├─ Matrix: 7 parallel jobs                            │
│  ├─ Each service independent                           │
│  ├─ Start after test complete                          │
│  └─ All complete in ~12 min                            │
│                                                         │
│  ┌──────────────────────────────┐                      │
│  │      All 7 images done        │                      │
│  └──────────────┬───────────────┘                      │
│                 │                                      │
│                 ▼                                      │
│                                                         │
│  job: deploy                   (needs: build-images)   │
│  ├─ Single sequential job                              │
│  ├─ Provision infrastructure                           │
│  ├─ Deploy services                                    │
│  └─ Completes in ~15 min                              │
│                                                         │
│  ✅ Deployment complete                                │
│                                                         │
└─────────────────────────────────────────────────────────┘

YAML representation:
───────────────────
jobs:
  test:
    runs-on: ubuntu-latest
    # No dependencies
    
  build-images:
    needs: test           ← Waits for test to complete
    strategy:
      matrix:
        service: [...]    ← 7 jobs in parallel
        
  deploy:
    needs: build-images   ← Waits for all 7 images
```

---

## 5️⃣ Docker Image Tagging Strategy

```
Each image gets TWO tags:

┌────────────────────────────────────────────┐
│ SERVICE: auth-service                      │
├────────────────────────────────────────────┤
│                                            │
│  ACR Build Command:                        │
│  ─────────────────                         │
│  az acr build \                            │
│    --image auth-service:latest \           │
│    --image auth-service:<commit-sha> \    │
│    --file Dockerfile                       │
│                                            │
├────────────────────────────────────────────┤
│  Result: TWO IMAGES IN ACR                │
│  ────────────────────────                 │
│                                            │
│  1. auth-service:latest                   │
│     ├─ Always points to most recent build │
│     ├─ Used by "latest" deployments       │
│     └─ Example: Can be replaced daily     │
│                                            │
│  2. auth-service:a1b2c3d7 (commit SHA)    │
│     ├─ Points to specific commit          │
│     ├─ Immutable tag                      │
│     ├─ For production tracing             │
│     └─ Example: rollback to specific ver  │
│                                            │
└────────────────────────────────────────────┘

Benefits:
─────────
✅ Can quickly identify which build is running
✅ Can rollback to any specific commit
✅ Debugging is easier ("it was from build a1b2c3d7")
✅ Production stability ("pin to specific commit")
```

---

## 6️⃣ Federated Credentials Flow

```
GitHub Actions Workflow Step:
───────────────────────────────

Step: "Log in with Azure (Federated Credentials)"

  ┌─────────────────────────────────────────┐
  │  GitHub Action Pushes Event             │
  │  ├─ Event: push to develop              │
  │  ├─ Repository: Ozymandros/ERP.Micro...│
  │  ├─ Branch: develop                     │
  │  └─ Signed by: GitHub certificate      │
  └──────────────┬──────────────────────────┘
                 │
                 ▼
  ┌──────────────────────────────────────────┐
  │  Azure AD (Entra ID) Receives Request    │
  │  ├─ Provider: github.com                │
  │  ├─ Subject: <github-org>/<repository> │
  │  ├─ Certificate chain validation        │
  │  └─ OIDC token validation               │
  └──────────────┬───────────────────────────┘
                 │
                 ▼
  ┌──────────────────────────────────────────┐
  │  Azure AD Issues Access Token            │
  │  ├─ Audience: Azure API                 │
  │  ├─ Subject: GitHub Actions            │
  │  ├─ Duration: 1 hour                    │
  │  ├─ Scope: AZURE_SUBSCRIPTION_ID        │
  │  └─ ⚠️ NO STORED SECRETS NEEDED        │
  └──────────────┬───────────────────────────┘
                 │
                 ▼
  ┌──────────────────────────────────────────┐
  │  Workflow Uses Token                     │
  │  ├─ azd auth login ...                   │
  │  ├─ az acr login ...                     │
  │  ├─ az ... (any Azure command)           │
  │  └─ ✅ Authenticated to Azure            │
  └──────────────────────────────────────────┘

Security Benefits:
──────────────────
✅ No secrets stored in GitHub
✅ No credentials in logs
✅ Time-limited tokens (1 hour)
✅ Automatic renewal per job
✅ Tied to specific branch
✅ Tied to specific repository
```

---

## 7️⃣ Failure Scenarios

```
DEVELOPMENT (azure-dev.yml):
────────────────────────────

Test Phase Fails:
  │
  ├─ continue-on-error: true  ← Doesn't stop
  │
  ├─→ Build Images Phase: ✅ CONTINUES
  │   (Images built despite test failure)
  │
  └─→ Deploy Phase: ✅ CONTINUES
      (Deployment proceeds to DEV)
      
  ⚠️ Risk: DEV may have broken code


Image Build Fails (e.g., billing-service):
  │
  ├─ fail-fast: false  ← Other builds continue
  │
  ├─→ auth-service: ✅ Continues
  ├─→ billing-service: ❌ FAILED
  ├─→ inventory-service: ✅ Continues
  │   ... (remaining services)
  │
  └─→ Deploy Phase: ❌ BLOCKED
      (Deployment blocked if any image missing)


PRODUCTION (azure-deploy.yml):
──────────────────────────────

Test Phase Fails:
  │
  ├─ continue-on-error: FALSE  ← STOPS HERE
  │
  ├─→ Build Images Phase: ❌ SKIPPED
  │   (No images built)
  │
  └─→ Deploy Phase: ❌ SKIPPED
      (No deployment to production)
      
  ✅ Production protected


Image Build Fails:
  │
  ├─ fail-fast: false  ← Other builds continue
  │
  └─→ Deploy Phase: ❌ BLOCKED
      (Missing images = no deployment)
      
  ✅ Partial deployments prevented
```

---

## 8️⃣ Caching Strategy

```
Workflow Caching for NuGet Packages:
────────────────────────────────────

First Run (no cache):
  │
  ├─→ Restore: Download all packages → 60s
  │
  └─→ cache action stores ~/.nuget/packages
      Key: ubunut-nuget-<csproj-hash>


Second Run (cache hit ⚡):
  │
  ├─→ Cache restored: ~/.nuget/packages
  │   └─ Hit: ~2s instead of 60s
  │
  └─→ Restore: Verify packages → 10s total
      (58s faster!)


Third Run (different csproj):
  │
  ├─→ Cache key mismatch
  │   (different hash = different cache)
  │
  ├─→ restore-keys used: ubunut-nuget-
  │   └─ Partial match: 30s (some packages reused)
  │
  └─→ New packages downloaded


Benefits:
─────────
✅ Speeds up workflow by ~50s per run
✅ Reduces GitHub bandwidth
✅ Faster CI/CD feedback to developers
✅ Automatic invalidation on csproj changes
```

---

## 9️⃣ Branch Strategy Summary

```
┌─────────────────────────────────────────────────────────┐
│ BRANCH FLOW DIAGRAM                                     │
└─────────────────────────────────────────────────────────┘

                        Feature/Dev Work
                        ────────────────
                        
┌─────────────────────────┐
│ feature/my-feature ↘    │
│ (local development)  \  │
│                       \ │
│ commits & pushes        │
│        │                │
│        ▼                │
│ Push to GitHub          │
└─────────────────────────┘
        │
        ▼
┌─────────────────────────────────────┐
│ CREATE PULL REQUEST                 │
│ (feature/my-feature → develop)      │
└──────────┬────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────┐
│ WORKFLOW: dotnet.yml                     │
│ ├─ Restore                               │
│ ├─ Build                                 │
│ ├─ Test                    (15 min)      │
│ └─ Status Check: ✅ PASS                │
└──────────┬────────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────┐
│ REVIEW & APPROVAL                        │
│ (optional: code review, discussions)     │
└──────────┬────────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────┐
│ MERGE TO develop                         │
│ (feature/my-feature → develop)           │
└──────────┬────────────────────────────────┘
           │
           ▼
┌────────────────────────────────────┐
│ WORKFLOW: azure-dev.yml            │
│ ├─ Test          (5 min)           │
│ ├─ Build Images  (12 min)          │
│ └─ Deploy to DEV (15 min)  (42min) │
│    Status: ✅ DEPLOYED to dev      │
└────────────────────────────────────┘
           │
           │ After dev verification
           │
           ▼
┌──────────────────────────────────────┐
│ CREATE PULL REQUEST                  │
│ (develop → main)                     │
└──────────┬───────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────┐
│ WORKFLOW: dotnet.yml                     │
│ (Same validation as before)              │
│ Status Check: ✅ PASS                   │
└──────────┬───────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────┐
│ REVIEW & FINAL APPROVAL                  │
│ (Required for production)                │
└──────────┬───────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────┐
│ MERGE TO main                            │
│ (develop → main)                         │
└──────────┬───────────────────────────────┘
           │
           ▼
┌────────────────────────────────────┐
│ WORKFLOW: azure-deploy.yml         │
│ ├─ Test (strict)  (5 min)          │
│ ├─ Build Images   (12 min)         │
│ └─ Deploy to PROD (20 min) (47min) │
│    Status: ✅ DEPLOYED to prod     │
└────────────────────────────────────┘
           │
           ▼
   ✅ PRODUCTION READY
```

---

## 🔟 Environment Comparison

```
┌─────────────────────────────────────────────────────────────┐
│ ENVIRONMENT DIFFERENCES                                     │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│                     DEV         PROD                        │
│                     ───         ────                        │
│  Trigger Branch:    develop     main                       │
│  Workflow:          azure-dev   azure-deploy              │
│  Time:              42 min      47 min                     │
│  Test strictness:   lenient     strict                     │
│  ACR Registry:      dev         prod                       │
│  Resource Tier:     Standard    Premium                    │
│  Replicas:          1-2         3+                        │
│  Database:          Basic       Premium                    │
│  SLA:               None        99.95%                     │
│  Monitoring:        App Insights (both)                    │
│  Alerts:            Dev team    On-call team              │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

**Last Updated:** October 31, 2025  
**Status:** ✅ Complete with visual references

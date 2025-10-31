# ☁️ Deployment & Azure

**Azure Deployment Guides, Environment Configuration & Bicep Infrastructure**  
Last Updated: October 27, 2025

---

## 📍 Overview

This category covers deploying the microservices platform to Azure. From initial infrastructure setup with Bicep to environment configuration and production deployments. Designed for DevOps and infrastructure teams.

---

## 📚 Documents in This Category

### 🚀 **NEW: Automated Build & Deploy Pipeline**

#### [BUILD_AND_DEPLOY_AUTOMATION.md](BUILD_AND_DEPLOY_AUTOMATION.md)
**Complete automated build and deploy solution**
- ✅ Fixes: "MANIFEST_UNKNOWN: manifest tagged by 'latest' is not found"
- PowerShell build script for local development
- GitHub Actions CI/CD pipeline
- ACR image building and caching
- Full troubleshooting guide

#### [QUICK_START_BUILD_DEPLOY.md](QUICK_START_BUILD_DEPLOY.md)
**Quick reference for automated deployments**
- 3-option deployment process
- One-command deploy setup
- GitHub Actions setup
- Performance metrics
- Common troubleshooting

#### [DEPLOYMENT_AUTOMATION_COMPLETE.md](DEPLOYMENT_AUTOMATION_COMPLETE.md)
**Complete solution overview**
- Architecture diagrams
- File structure and purposes
- Workflow examples
- Security considerations
- Best practices

#### [BUILD_AND_DEPLOY_AUTOMATION_SUMMARY.md](BUILD_AND_DEPLOY_AUTOMATION_SUMMARY.md)
**Problem/solution summary**
- Problem identification
- Complete solution overview
- Files included and their purposes

#### [GITHUB_ACTIONS_ARCHITECTURE.md](GITHUB_ACTIONS_ARCHITECTURE.md) ⭐ NEW
**Complete CI/CD workflow architecture**
- 3-phase optimized workflow pattern
- Parallel execution with matrix builds
- Federated credentials & security
- Timing estimates & debugging

#### [WORKFLOWS_VISUAL_DIAGRAMS.md](WORKFLOWS_VISUAL_DIAGRAMS.md) ⭐ NEW
**Visual diagrams for GitHub Actions workflows**
- ASCII diagrams for workflow flows
- Timeline visualizations
- Job dependency graphs
- Failure scenario handling

#### [WORKFLOWS_REFACTORING_OCTOBER_31.md](WORKFLOWS_REFACTORING_OCTOBER_31.md) ⭐ NEW
**Modern Docker BuildX & metadata actions refactoring**
- Docker Build Push Action v5 implementation
- Automatic metadata tagging
- Test artifact collection
- Deployment summary reporting

---

### [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md)
**Complete deployment to Azure**
- Prerequisites and prerequisites
- Deployment steps
- Configuration validation
- Post-deployment verification
- Troubleshooting deployment issues

### [ENVIRONMENTS.md](ENVIRONMENTS.md)
**Environment setup and management**
- Development environment
- Staging environment
- Production environment
- Environment parity
- Environment-specific configuration

### [AZURE_DEPLOYMENT.md](AZURE_DEPLOYMENT.md)
**Azure-specific deployment procedures**
- Container Apps deployment
- Database setup
- Key Vault configuration
- Managed identities setup
- Monitoring and observability setup

### [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
**Common deployment issues**
- Deployment failures
- Configuration errors
- Networking issues
- Permission problems
- Resource quota exceeded

---

## 🎯 Quick Start

### Deploy to Azure

```bash
# 1. Authenticate to Azure
az login

# 2. Set subscription
az account set --subscription "subscription-id"

# 3. Create resource group
az group create -n erp-rg -l eastus

# 4. Deploy infrastructure
az deployment group create \
  -g erp-rg \
  -f infra/main.bicep \
  -p infra/main.parameters.json

# 5. Deploy services
kubectl apply -f deploy/kubernetes/
```

---

## 📊 Environment Comparison

| Aspect | Dev | Staging | Production |
|--------|-----|---------|-----------|
| **Region** | eastus | eastus | eastus2 |
| **Replicas** | 1 | 2 | 3+ |
| **CPU/Memory** | 0.5/1Gi | 1/2Gi | 2/4Gi |
| **Database** | Standard | General Purpose | Premium |
| **Cache** | Basic | Standard | Premium |
| **Rate Limit** | 100/min | 500/min | 1000/min |
| **Backups** | Manual | Daily | Hourly |
| **SLA** | None | 99.5% | 99.95% |

---

## 🔄 Deployment Process

```
1. Code Commit
   ↓
2. Build Pipeline (CI)
   - Compile code
   - Run tests
   - Build container images
   - Push to registry
   ↓
3. Deploy to Staging (CD)
   - Deploy infrastructure
   - Deploy services
   - Run smoke tests
   - Run integration tests
   ↓
4. Approval Gate
   - Manual approval required
   ↓
5. Deploy to Production
   - Blue-green deployment
   - Canary testing
   - Rollback ready
   ↓
6. Monitor & Alert
   - Watch metrics
   - Check logs
   - Verify services
```

---

## 📚 Related Categories

- **Infrastructure:** [Infrastructure Guide](../infrastructure/README.md) - Bicep & IaC
- **Operations:** [Operations Guide](../operations/README.md) - Production support
- **Security:** [Security Documentation](../security/README.md) - Secure deployment
- **Development:** [Development Setup](../development/README.md) - Local testing

---

## 🔐 Pre-Deployment Checklist

Before deploying to production:

- [ ] All tests passing locally
- [ ] Code reviewed and approved
- [ ] Security scan completed
- [ ] Secrets in Key Vault (not in code)
- [ ] Staging deployment validated
- [ ] Performance tests passed
- [ ] Disaster recovery plan reviewed
- [ ] Runbooks updated
- [ ] Team notified of deployment
- [ ] Monitoring/alerts configured

---

## 🆘 Common Issues

| Issue | Solution |
|-------|----------|
| Deployment timeout | Check resource quotas |
| Authentication failed | Verify credentials |
| Port conflicts | Check existing resources |
| Insufficient quota | Request increase |
| DNS resolution fails | Check DNS settings |
| Database connection fails | Verify connection string |

---

## ✅ Post-Deployment Verification

After deployment:

- [ ] All services running (`az containerapp list`)
- [ ] Health checks passing
- [ ] Can access API gateway
- [ ] Database tables created
- [ ] Caching working
- [ ] Logging configured
- [ ] Monitoring active
- [ ] Alerts configured
- [ ] Backups scheduled
- [ ] Disaster recovery tested

---

## 📊 Deployment Tracking

| Phase | Time | Status |
|-------|------|--------|
| Infrastructure creation | 15 min | ✅ |
| Service deployment | 10 min | ✅ |
| Database migration | 5 min | ✅ |
| Cache initialization | 2 min | ✅ |
| Health checks | 3 min | ✅ |
| Smoke tests | 5 min | ✅ |
| Total | ~40 min | ✅ |

---

## 📞 Next Steps

- **🚀 Just starting?** → [QUICK_START_BUILD_DEPLOY.md](QUICK_START_BUILD_DEPLOY.md) - Get deploying in minutes!
- **Need automation?** → [BUILD_AND_DEPLOY_AUTOMATION.md](BUILD_AND_DEPLOY_AUTOMATION.md) - Complete technical guide
- **Understanding the solution?** → [DEPLOYMENT_AUTOMATION_COMPLETE.md](DEPLOYMENT_AUTOMATION_COMPLETE.md) - Full overview
- **First deployment?** → [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md)
- **Setup environments?** → [ENVIRONMENTS.md](ENVIRONMENTS.md)
- **Azure-specific?** → [AZURE_DEPLOYMENT.md](AZURE_DEPLOYMENT.md)
- **Having issues?** → [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
- **Infrastructure?** → [Infrastructure Guide](../infrastructure/README.md)

---

## 🔗 Full Document Map

```
deployment/
├── README.md (this file)
├── BUILD_AND_DEPLOY_AUTOMATION.md ⭐ NEW
├── QUICK_START_BUILD_DEPLOY.md ⭐ NEW
├── DEPLOYMENT_AUTOMATION_COMPLETE.md ⭐ NEW
├── BUILD_AND_DEPLOY_AUTOMATION_SUMMARY.md
├── GITHUB_ACTIONS_ARCHITECTURE.md ⭐ NEW
├── DEPLOYMENT.md
├── DEPLOYMENT_CHECKLIST.md
├── DEPLOYMENT_OPERATIONS_GUIDE.md
├── PRE_DEPLOYMENT_CHECKLIST.md
├── ENVIRONMENTS.md
├── AZURE_DEPLOYMENT.md
└── TROUBLESHOOTING.md
```

---

**Last Updated:** October 31, 2025  
**Category Status:** ✅ Complete  
**Documents:** 11 files (3 new for Build & Deploy automation)  
**Environments:** 3 total (Dev, Staging, Prod)

# ⚙️ Infrastructure & Infrastructure as Code

**Bicep Templates, Phase-Based Implementation & Validation**  
Last Updated: October 27, 2025

---

## 📍 Overview

This category covers infrastructure as code using Bicep. From module templates to phased implementation guides for managed identities, secrets, RBAC, and Azure resources. Designed for infrastructure engineers and DevOps teams.

---

## 📚 Documents in This Category

### [BICEP_OVERVIEW.md](BICEP_OVERVIEW.md)
**Bicep and Infrastructure as Code concepts**
- Bicep language basics
- Module structure and reusability
- Parameter files
- Outputs and dependencies
- Best practices

### [BICEP_MODULES.md](BICEP_MODULES.md)
**Available Bicep modules and templates**
- Module catalog
- Resource coverage
- Module parameters
- Usage examples
- Extending modules

### [VALIDATION.md](VALIDATION.md)
**Bicep template validation**
- Linting and validation
- Testing templates
- Parameter validation
- Output verification
- Common validation issues

### PHASE_GUIDES/ (5 files)
**Phased implementation approach**

#### [PHASE_GUIDES/README.md](PHASE_GUIDES/README.md)
- Phase overview
- Dependencies between phases
- Timeline
- Rollback procedures

#### [PHASE_1_MANAGED_IDENTITIES.md](PHASE_GUIDES/PHASE_1_MANAGED_IDENTITIES.md)
- System-assigned identities
- User-assigned identities
- Identity assignment
- Verification steps

#### [PHASE_2_SECRETS.md](PHASE_GUIDES/PHASE_2_SECRETS.md)
- Key Vault creation
- Secret migration
- Secret rotation
- Emergency rotation

#### [PHASE_3_RBAC.md](PHASE_GUIDES/PHASE_3_RBAC.md)
- Role definitions
- Role assignments
- Custom roles
- Least privilege

#### [PHASE_4_SQL_RBAC.md](PHASE_GUIDES/PHASE_4_SQL_RBAC.md)
- SQL Server setup
- Database-level RBAC
- User management
- Permission verification

#### [PHASE_5_CONFIGURATION.md](PHASE_GUIDES/PHASE_5_CONFIGURATION.md)
- App Configuration setup
- Configuration migration
- Feature flags
- Configuration updates

---

## 🎯 Infrastructure Architecture

```
┌─────────────────────────────────────┐
│  Resource Group                     │
├─────────────────────────────────────┤
│  ├─ Container Registry (ACR)        │
│  ├─ Container Apps Environment      │
│  │  ├─ Auth Service                │
│  │  ├─ Inventory Service           │
│  │  ├─ Orders Service              │
│  │  ├─ Sales Service               │
│  │  ├─ Billing Service             │
│  │  └─ Purchasing Service          │
│  ├─ API Gateway Service            │
│  ├─ SQL Database                   │
│  ├─ Redis Cache                    │
│  ├─ Key Vault                      │
│  ├─ App Configuration              │
│  ├─ Application Insights           │
│  ├─ Log Analytics Workspace        │
│  └─ Managed Identities             │
└─────────────────────────────────────┘
```

---

## 📊 Infrastructure Components

| Component | Type | Purpose |
|-----------|------|---------|
| **Container Apps** | Compute | Host microservices |
| **API Gateway** | Networking | External access |
| **SQL Database** | Data | Persistent storage |
| **Redis Cache** | Cache | State & caching |
| **Key Vault** | Security | Secrets management |
| **App Configuration** | Config | Configuration center |
| **Application Insights** | Monitoring | Application telemetry |
| **Log Analytics** | Logging | Centralized logging |

---

## 🔄 Phased Approach

### Phase 1: Managed Identities
- Create identities
- Assign to services
- Test connectivity
- **Duration:** 1 day

### Phase 2: Secrets Management
- Create Key Vault
- Migrate secrets
- Setup rotation
- **Duration:** 2 days

### Phase 3: RBAC
- Define roles
- Assign permissions
- Verify access
- **Duration:** 2 days

### Phase 4: SQL RBAC
- Create SQL logins
- Grant permissions
- Test access
- **Duration:** 1 day

### Phase 5: Configuration
- Setup App Configuration
- Migrate settings
- Configure features
- **Duration:** 1 day

**Total Timeline:** 1 week

---

## 📚 Related Categories

- **Deployment:** [Deployment Guide](../deployment/README.md) - Deployment procedures
- **Security:** [Security Guide](../security/README.md) - Security architecture
- **Operations:** [Operations Guide](../operations/README.md) - Production support
- **Development:** [Development Setup](../development/README.md) - Local development

---

## 🔄 Reading Order

1. Start with [BICEP_OVERVIEW.md](BICEP_OVERVIEW.md) to understand IaC
2. Review [BICEP_MODULES.md](BICEP_MODULES.md) for available modules
3. Read [PHASE_GUIDES/README.md](PHASE_GUIDES/README.md) for implementation plan
4. Follow each phase guide in order
5. Reference [VALIDATION.md](VALIDATION.md) for validation

---

## 📋 Key Bicep Files

| File | Purpose |
|------|---------|
| `infra/main.bicep` | Main orchestration |
| `infra/main.parameters.json` | Parameter values |
| `infra/core/main.bicep` | Core resources |
| `infra/*/main.bicep` | Service-specific resources |
| `infra/resources.bicep` | Shared resources |

---

## ✅ Infrastructure Checklist

### Before Deployment
- [ ] All Bicep files validated
- [ ] Parameters configured correctly
- [ ] Prerequisites met
- [ ] Resource quotas sufficient
- [ ] Naming conventions followed
- [ ] Tags applied consistently

### After Deployment
- [ ] All resources created
- [ ] Identities assigned
- [ ] Secrets configured
- [ ] RBAC applied
- [ ] Monitoring enabled
- [ ] Backup configured

### Ongoing
- [ ] Templates version controlled
- [ ] Changes peer-reviewed
- [ ] Documentation updated
- [ ] Disaster recovery tested
- [ ] Cost monitoring active

---

## 💡 Key Concepts

### Infrastructure as Code
- Version controlled
- Repeatable deployments
- Consistent environments
- Rollback capability
- Documentation

### Managed Identities
- No credential management
- Automatic token refresh
- Azure-native security
- Audit trails
- Least privilege

### Key Vault
- Centralized secrets
- Access control
- Audit logging
- Automatic rotation
- Disaster recovery

### RBAC
- Fine-grained control
- Custom roles
- Built-in roles
- Service principal support
- Time-limited access

---

## 🆘 Common Issues

| Issue | Solution |
|-------|----------|
| Template validation fails | [VALIDATION.md](VALIDATION.md) |
| Deployment timeout | Check resource quotas |
| Permission denied | Check RBAC setup |
| Identity not working | See [PHASE_1](PHASE_GUIDES/PHASE_1_MANAGED_IDENTITIES.md) |
| Secret access fails | See [PHASE_2](PHASE_GUIDES/PHASE_2_SECRETS.md) |

---

## 📊 Resource Status

| Resource | Deployed | Status |
|----------|----------|--------|
| Container Apps | ✅ | All 6 services |
| API Gateway | ✅ | Running |
| SQL Database | ✅ | With RBAC |
| Redis Cache | ✅ | Authenticated |
| Key Vault | ✅ | 10+ secrets |
| Managed Identity | ✅ | 6 services |
| App Configuration | ✅ | 20+ settings |
| Monitoring | ✅ | Full telemetry |

---

## 📞 Next Steps

- **Learning Bicep?** → [BICEP_OVERVIEW.md](BICEP_OVERVIEW.md)
- **Need modules?** → [BICEP_MODULES.md](BICEP_MODULES.md)
- **Validating templates?** → [VALIDATION.md](VALIDATION.md)
- **Phase 1: Identities?** → [PHASE_1](PHASE_GUIDES/PHASE_1_MANAGED_IDENTITIES.md)
- **Phase 2: Secrets?** → [PHASE_2](PHASE_GUIDES/PHASE_2_SECRETS.md)
- **Phase 3: RBAC?** → [PHASE_3](PHASE_GUIDES/PHASE_3_RBAC.md)
- **Phase 4: SQL RBAC?** → [PHASE_4](PHASE_GUIDES/PHASE_4_SQL_RBAC.md)
- **Phase 5: Configuration?** → [PHASE_5](PHASE_GUIDES/PHASE_5_CONFIGURATION.md)

---

## 🔗 Full Document Map

```
infrastructure/
├── README.md (this file)
├── BICEP_OVERVIEW.md
├── BICEP_MODULES.md
├── VALIDATION.md
└── PHASE_GUIDES/
    ├── README.md
    ├── PHASE_1_MANAGED_IDENTITIES.md
    ├── PHASE_2_SECRETS.md
    ├── PHASE_3_RBAC.md
    ├── PHASE_4_SQL_RBAC.md
    └── PHASE_5_CONFIGURATION.md
```

---

**Last Updated:** October 27, 2025  
**Category Status:** ✅ Complete  
**Documents:** 8 files (1 + 1 + 1 + 5 phases)  
**Phased Approach:** 5 phases, 1 week total

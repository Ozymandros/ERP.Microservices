# 📚 ERP Microservices Documentation

**Version:** 1.0.0  
**Last Updated:** October 27, 2025  
**Status:** ✅ Production Ready

Welcome to the complete documentation for the ERP Microservices platform. This documentation is organized by role, topic, and use case for easy navigation.

---

## 🎯 Quick Navigation by Role

### 👨‍💻 **For Developers**
- [Development Setup](development/DEVELOPMENT_SETUP.md) - Get your local environment running
- [Coding Standards](development/CODING_STANDARDS.md) - Follow our conventions
- [Testing Guide](development/TESTING.md) - How to test your code
- [Debugging Guide](development/DEBUGGING.md) - Debug techniques and tools

### 🏗️ **For DevOps/Infrastructure**
- [Deployment Guide](deployment/DEPLOYMENT_GUIDE.md) - Deploy to Azure
- [Infrastructure as Code](infrastructure/BICEP_OVERVIEW.md) - Bicep modules
- [Environment Configuration](deployment/ENVIRONMENTS.md) - Dev/Staging/Prod setup
- [Phase Implementation Guides](infrastructure/PHASE_GUIDES/README.md) - Phased rollout

### 🔒 **For Security Team**
- [Security Overview](security/SECURITY_OVERVIEW.md) - Security model
- [Authentication Flows](security/AUTHENTICATION.md) - How auth works
- [Authorization & RBAC](security/AUTHORIZATION.md) - Permission model
- [Secrets Management](security/SECRETS_MANAGEMENT.md) - Key Vault setup
- [Best Practices](security/BEST_PRACTICES.md) - Security guidelines

### 📊 **For Operations/SRE**
- [Monitoring & Observability](operations/MONITORING.md) - Observability setup
- [Centralized Logging](operations/LOGGING.md) - Log aggregation
- [Health Checks](operations/HEALTH_CHECKS.md) - Health monitoring
- [Auto-Scaling](operations/SCALING.md) - Scaling configuration
- [Disaster Recovery](operations/BACKUP_RECOVERY.md) - Backup & recovery
- [Operational Runbooks](operations/RUNBOOKS.md) - Common procedures

### 👤 **For Architects/Leads**
- [Architecture Overview](architecture/ARCHITECTURE_OVERVIEW.md) - System design
- [Microservices Design](architecture/MICROSERVICES_DESIGN.md) - Service patterns
- [Data Flow](architecture/DATA_FLOW.md) - Data patterns

---

## 📑 **Complete Documentation Index**

### 🏗️ Architecture & Design
- [Architecture Overview](architecture/ARCHITECTURE_OVERVIEW.md)
- [Microservices Design Patterns](architecture/MICROSERVICES_DESIGN.md)
- [Data Flow Diagrams](architecture/DATA_FLOW.md)
- [System Diagrams](architecture/DIAGRAMS.md)

### 🚀 Getting Started
- [5-Minute Quickstart](../QUICKSTART.md)
- [Local Development Setup](development/DEVELOPMENT_SETUP.md)
- [Docker Compose Guide](docker-compose/DOCKER_COMPOSE_GUIDE.md)

### ☁️ Infrastructure & Deployment
- [Azure Deployment Guide](deployment/DEPLOYMENT_GUIDE.md)
- [Bicep Infrastructure as Code](infrastructure/BICEP_OVERVIEW.md)
- [Bicep Modules Explained](infrastructure/BICEP_MODULES.md)
- [Phase Implementation](infrastructure/PHASE_GUIDES/README.md)
  - [Phase 1: Managed Identities](infrastructure/PHASE_GUIDES/PHASE_1_MANAGED_IDENTITIES.md)
  - [Phase 2: Secrets Management](infrastructure/PHASE_GUIDES/PHASE_2_SECRETS.md)
  - [Phase 3: RBAC](infrastructure/PHASE_GUIDES/PHASE_3_RBAC.md)
  - [Phase 4: SQL RBAC](infrastructure/PHASE_GUIDES/PHASE_4_SQL_RBAC.md)
  - [Phase 5: Configuration](infrastructure/PHASE_GUIDES/PHASE_5_CONFIGURATION.md)
- [Environment Configuration](deployment/ENVIRONMENTS.md)
- [Bicep Validation](infrastructure/VALIDATION.md)

### 🐳 Local Development
- [Docker Compose Guide](docker-compose/DOCKER_COMPOSE_GUIDE.md)
- [Services Reference](docker-compose/SERVICES_REFERENCE.md)
- [DAPR Local Setup](docker-compose/DAPR_LOCAL_SETUP.md)
- [Docker Compose Troubleshooting](docker-compose/TROUBLESHOOTING.md)

### 🌐 API Gateway
- [Ocelot Overview](api-gateway/OCELOT_OVERVIEW.md)
- [Ocelot Configuration](api-gateway/OCELOT_CONFIGURATION.md)
- [Authentication & JWT](api-gateway/AUTHENTICATION.md)
- [Rate Limiting & QoS](api-gateway/RATE_LIMITING.md)
- [Gateway Troubleshooting](api-gateway/TROUBLESHOOTING.md)

### 🔧 Microservices
- [Service Architecture](microservices/SERVICE_TEMPLATES.md)
- [DAPR Integration](microservices/DAPR_INTEGRATION.md)
- [Database Access Patterns](microservices/DATABASE_ACCESS.md)
- [Caching with Redis](microservices/CACHING.md)
- [Event-Driven Pub/Sub](microservices/PUB_SUB.md)

### 🔒 Security
- [Security Model Overview](security/SECURITY_OVERVIEW.md)
- [Authentication Flows](security/AUTHENTICATION.md)
- [Authorization & RBAC](security/AUTHORIZATION.md)
- [Secrets Management](security/SECRETS_MANAGEMENT.md)
- [Security Best Practices](security/BEST_PRACTICES.md)

### 📊 Operations & Monitoring
- [Monitoring & Observability](operations/MONITORING.md)
- [Centralized Logging](operations/LOGGING.md)
- [Health Checks](operations/HEALTH_CHECKS.md)
- [Auto-Scaling Configuration](operations/SCALING.md)
- [Disaster Recovery](operations/BACKUP_RECOVERY.md)
- [Operational Runbooks](operations/RUNBOOKS.md)

### 👨‍💻 Development Guidelines
- [Development Setup](development/DEVELOPMENT_SETUP.md)
- [Coding Standards](development/CODING_STANDARDS.md)
- [Testing Guide](development/TESTING.md)
- [Git Workflow](development/GIT_WORKFLOW.md)
- [Debugging Guide](development/DEBUGGING.md)

### 📖 Reference
- [Glossary](reference/GLOSSARY.md)
- [Environment Variables](reference/ENVIRONMENT_VARIABLES.md)
- [Port Mapping Reference](reference/PORT_MAPPING.md)
- [API Endpoints Reference](reference/API_ENDPOINTS.md)
- [Troubleshooting Index](reference/TROUBLESHOOTING_INDEX.md)

---

## 🔍 **Troubleshooting Quick Links**

Having issues? Find your problem:

- **Docker Compose won't start** → [Docker Troubleshooting](docker-compose/TROUBLESHOOTING.md)
- **Deployment failed** → [Deployment Troubleshooting](deployment/TROUBLESHOOTING.md)
- **Gateway returns 401/429/503** → [Gateway Troubleshooting](api-gateway/TROUBLESHOOTING.md)
- **Service can't connect to database** → [Service Database Guide](microservices/DATABASE_ACCESS.md)
- **Can't find what you need** → [Troubleshooting Index](reference/TROUBLESHOOTING_INDEX.md)

---

## 📋 **Document Status Matrix**

| Category | Document | Status | Last Updated |
|----------|----------|--------|--------------|
| **Architecture** | Overview | ✅ | 2025-10-27 |
| | Design Patterns | ✅ | 2025-10-27 |
| | Data Flow | ✅ | 2025-10-27 |
| **Deployment** | Azure Guide | ✅ | 2025-10-27 |
| | Environments | ✅ | 2025-10-27 |
| | Troubleshooting | ✅ | 2025-10-27 |
| **Infrastructure** | Bicep Overview | ✅ | 2025-10-27 |
| | Phase Guides | ✅ | 2025-10-27 |
| **Docker** | Compose Guide | ✅ | 2025-10-27 |
| | DAPR Setup | ✅ | 2025-10-27 |
| **Gateway** | Ocelot Overview | ✅ | 2025-10-27 |
| | Authentication | ✅ | 2025-10-27 |
| **Security** | Overview | ✅ | 2025-10-27 |
| | Best Practices | ✅ | 2025-10-27 |
| **Operations** | Monitoring | ✅ | 2025-10-27 |
| | Runbooks | ✅ | 2025-10-27 |

---

## 🎓 **Learning Paths**

### Path 1: New to the Project (30 minutes)
1. Read: [Architecture Overview](architecture/ARCHITECTURE_OVERVIEW.md)
2. Read: [QUICKSTART](../QUICKSTART.md)
3. Do: [Local Development Setup](development/DEVELOPMENT_SETUP.md)

### Path 2: Setting Up Locally (1 hour)
1. Read: [Docker Compose Guide](docker-compose/DOCKER_COMPOSE_GUIDE.md)
2. Do: [Development Setup](development/DEVELOPMENT_SETUP.md)
3. Explore: [Services Reference](docker-compose/SERVICES_REFERENCE.md)

### Path 3: Deploying to Azure (2 hours)
1. Read: [Deployment Guide](deployment/DEPLOYMENT_GUIDE.md)
2. Review: [Phase Guides](infrastructure/PHASE_GUIDES/README.md)
3. Do: [Azure Deployment](deployment/AZURE_DEPLOYMENT.md)
4. Verify: [Health Checks](operations/HEALTH_CHECKS.md)

### Path 4: Understanding Security (1.5 hours)
1. Read: [Security Overview](security/SECURITY_OVERVIEW.md)
2. Study: [Authentication](security/AUTHENTICATION.md)
3. Learn: [RBAC Model](security/AUTHORIZATION.md)
4. Review: [Best Practices](security/BEST_PRACTICES.md)

### Path 5: Operations & Monitoring (1.5 hours)
1. Setup: [Monitoring](operations/MONITORING.md)
2. Configure: [Logging](operations/LOGGING.md)
3. Learn: [Runbooks](operations/RUNBOOKS.md)

---

## 🔗 **Key External Links**

- [.NET 9 Documentation](https://learn.microsoft.com/en-us/dotnet/core/)
- [Azure Container Apps](https://learn.microsoft.com/en-us/azure/container-apps/)
- [DAPR Documentation](https://dapr.io/docs/)
- [Ocelot API Gateway](https://ocelot.readthedocs.io/)
- [Bicep Documentation](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/)

---

## 💬 **Documentation Conventions**

- **✅ Status:** ✅ Complete | ⏳ In Progress | 📌 Planned
- **🔗 Links:** Cross-references to related docs
- **💡 Tips:** Important information highlighted
- **⚡ Quick Commands:** Code snippets for common tasks
- **⚠️ Warnings:** Critical information
- **📊 Diagrams:** Mermaid diagrams included

---

## 🤝 **Contributing to Documentation**

When adding or updating documentation:

1. **Follow Structure** - Place docs in appropriate folder
2. **Use Templates** - Follow the document format
3. **Link Cross-References** - Link to related docs
4. **Update Index** - Add to this README
5. **Mark Status** - Include status badge
6. **Test Links** - Verify all links work

---

## 📞 **Getting Help**

- **Can't find something?** Check [Troubleshooting Index](reference/TROUBLESHOOTING_INDEX.md)
- **Have a question?** See [FAQ](#faq) below
- **Report an issue?** Update the relevant troubleshooting guide
- **Suggest improvement?** Submit a documentation PR

---

## ❓ **FAQ**

**Q: Where do I start?**  
A: Depends on your role! See "Quick Navigation by Role" above.

**Q: How do I set up locally?**  
A: Follow [Development Setup](development/DEVELOPMENT_SETUP.md) or [Docker Compose Guide](docker-compose/DOCKER_COMPOSE_GUIDE.md)

**Q: How do I deploy to Azure?**  
A: See [Deployment Guide](deployment/DEPLOYMENT_GUIDE.md)

**Q: How do I debug an issue?**  
A: Check [Debugging Guide](development/DEBUGGING.md) or [Troubleshooting Index](reference/TROUBLESHOOTING_INDEX.md)

**Q: How does authentication work?**  
A: See [Authentication](security/AUTHENTICATION.md) and [API Gateway Auth](api-gateway/AUTHENTICATION.md)

---

**Last Updated:** October 27, 2025  
**Maintained By:** Development Team  
**Feedback:** Welcome! Submit improvements via pull requests.

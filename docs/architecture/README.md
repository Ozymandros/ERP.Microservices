# 🏗️ Architecture & Design

**Understanding the ERP Microservices Architecture**  
Last Updated: October 27, 2025

---

## 📍 Overview

This category covers the complete system architecture, design patterns, and system diagrams. Start here if you want to understand the "big picture" of how all components fit together.

---

## 📚 Documents in This Category

### [ARCHITECTURE_OVERVIEW.md](ARCHITECTURE_OVERVIEW.md)
**Complete system architecture and component relationships**
- System-wide architecture diagrams
- Component interactions
- Data flow across services
- Technology stack overview
- Service communication patterns

### [MICROSERVICES_DESIGN.md](MICROSERVICES_DESIGN.md)
**Microservices design patterns and principles**
- Service boundaries and responsibilities
- Database per service pattern
- Event-driven architecture
- DAPR integration points
- Service mesh considerations

### [DATA_FLOW.md](DATA_FLOW.md)
**Data movement and transformations**
- Request/response flows
- Event publication and subscription
- State management patterns
- Caching strategies
- Database access patterns

### [DIAGRAMS.md](DIAGRAMS.md)
**Visual representations of architecture**
- Service interaction diagrams
- Deployment architecture
- Data flow diagrams
- Network topology
- Security boundaries

---

## 🎯 Quick Links

**I want to understand...**

| Goal | Document | Time |
|------|----------|------|
| The overall system | [Architecture Overview](ARCHITECTURE_OVERVIEW.md) | 15 min |
| How services work together | [Microservices Design](MICROSERVICES_DESIGN.md) | 20 min |
| How data flows | [Data Flow](DATA_FLOW.md) | 15 min |
| Visual diagrams | [Diagrams](DIAGRAMS.md) | 10 min |

---

## 🔄 Reading Order

1. Start with [ARCHITECTURE_OVERVIEW.md](ARCHITECTURE_OVERVIEW.md) for the big picture
2. Read [MICROSERVICES_DESIGN.md](MICROSERVICES_DESIGN.md) to understand service patterns
3. Study [DATA_FLOW.md](DATA_FLOW.md) for data interactions
4. Review [DIAGRAMS.md](DIAGRAMS.md) for visual understanding

---

## 📚 Related Categories

- **Infrastructure:** [Infrastructure Documentation](../infrastructure/README.md) - Deploying architecture to cloud
- **Development:** [Development Guidelines](../development/README.md) - Building within this architecture
- **API Gateway:** [Gateway Documentation](../api-gateway/README.md) - How external requests enter system
- **Microservices:** [Microservices Guide](../microservices/README.md) - Individual service development

---

## 🎓 Key Concepts

### 6 Microservices
- **Auth Service** - Authentication & authorization
- **Inventory Service** - Product inventory management
- **Orders Service** - Order processing
- **Sales Service** - Sales management
- **Billing Service** - Billing & invoicing
- **Purchasing Service** - Procurement

### Core Technologies
- **.NET Aspire** - Microservices orchestration
- **DAPR** - Distributed Application Runtime
- **Redis** - Caching & state store
- **SQL Server** - Persistent storage
- **Ocelot** - API Gateway
- **Azure Container Apps** - Production hosting

### Key Patterns
- Service-to-service communication via HTTP & DAPR
- Event-driven architecture with Redis Pub/Sub
- Database per service (polyglot persistence)
- API gateway for external access
- Circuit breaker for resilience
- Health checks for observability

---

## 💡 Common Questions

**Q: How do services communicate?**  
A: Services use HTTP for synchronous calls and DAPR Pub/Sub (Redis) for asynchronous events. See [Microservices Design](MICROSERVICES_DESIGN.md) for details.

**Q: How is data stored?**  
A: Each service has its own database. See [Data Flow](DATA_FLOW.md) for data access patterns.

**Q: What is DAPR?**  
A: DAPR (Distributed Application Runtime) provides cross-platform APIs for building distributed systems. See [ARCHITECTURE_OVERVIEW.md](ARCHITECTURE_OVERVIEW.md).

**Q: How does the API gateway work?**  
A: Ocelot routes all external requests to appropriate services with authentication and rate limiting. See [Gateway Documentation](../api-gateway/README.md).

---

## 🔗 Full Document Map

```
architecture/
├── README.md (this file)
├── ARCHITECTURE_OVERVIEW.md
├── MICROSERVICES_DESIGN.md
├── DATA_FLOW.md
└── DIAGRAMS.md
```

---

## ✅ When to Read This Category

- ✅ Starting a new role in the project
- ✅ Proposing architectural changes
- ✅ Understanding system design
- ✅ Onboarding new team members
- ✅ Reviewing deployment strategy

---

## 📞 Next Steps

- **Ready to code?** → [Development Setup](../development/DEVELOPMENT_SETUP.md)
- **Deploying to Azure?** → [Deployment Guide](../deployment/DEPLOYMENT_GUIDE.md)
- **Understanding specific service?** → [Microservices Guide](../microservices/README.md)
- **Working with gateway?** → [Gateway Documentation](../api-gateway/README.md)

---

**Last Updated:** October 27, 2025  
**Category Status:** ✅ Complete  
**Documents:** 4 files

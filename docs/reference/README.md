# 📖 Reference & Quick Lookup

**Glossary, Environment Variables, Port Mapping, API Endpoints & Troubleshooting Index**  
Last Updated: October 27, 2025

---

## 📍 Overview

This category is a quick reference for common lookups. From terminology and environment variables to port mappings and API endpoints. Designed for quick access when you need to look something up.

---

## 📚 Documents in This Category

### [GLOSSARY.md](GLOSSARY.md)
**Technical terms and definitions**
- Architecture terms
- Technology terms
- Service terms
- Acronyms and abbreviations
- Cross-references to detailed docs

### [ENVIRONMENT_VARIABLES.md](ENVIRONMENT_VARIABLES.md)
**Environment variables reference**
- All variables used
- Default values
- Required vs optional
- Local vs production
- Setup instructions

### [PORT_MAPPING.md](PORT_MAPPING.md)
**Port allocation reference**
- All ports used
- Service-to-port mapping
- Health check ports
- Management ports
- Port conflict resolution

### [API_ENDPOINTS.md](API_ENDPOINTS.md)
**API endpoint reference**
- Gateway endpoints
- Service endpoints
- Authentication endpoints
- Health check endpoints
- Example requests

### [TROUBLESHOOTING_INDEX.md](TROUBLESHOOTING_INDEX.md)
**Master troubleshooting index**
- Cross-reference to all troubleshooting guides
- Problem-to-document mapping
- Common error codes
- Resolution strategies

---

## 🎯 Quick Lookups

### "What does this term mean?"
→ [GLOSSARY.md](GLOSSARY.md)

### "What ports are used?"
→ [PORT_MAPPING.md](PORT_MAPPING.md)

### "What environment variable do I need?"
→ [ENVIRONMENT_VARIABLES.md](ENVIRONMENT_VARIABLES.md)

### "How do I call this API?"
→ [API_ENDPOINTS.md](API_ENDPOINTS.md)

### "I'm getting an error, where's the fix?"
→ [TROUBLESHOOTING_INDEX.md](TROUBLESHOOTING_INDEX.md)

---

## 🔍 Reference Tables

### Services Quick Reference

| Service | Port | Database | Health | Docs |
|---------|------|----------|--------|------|
| Auth | 6001 | auth-db | `/health` | [Auth API](API_ENDPOINTS.md#auth-service) |
| Billing | 6002 | billing-db | `/health` | [Billing API](API_ENDPOINTS.md#billing-service) |
| Inventory | 6004 | inventory-db | `/health` | [Inventory API](API_ENDPOINTS.md#inventory-service) |
| Orders | 6005 | orders-db | `/health` | [Orders API](API_ENDPOINTS.md#orders-service) |
| Purchasing | 6006 | purchasing-db | `/health` | [Purchasing API](API_ENDPOINTS.md#purchasing-service) |
| Sales | 6007 | sales-db | `/health` | [Sales API](API_ENDPOINTS.md#sales-service) |
| CRM | 6003 | crm-db | `/health` | [CRM API](API_ENDPOINTS.md#crm-service) |
| SemanticKernel | 6008 | (no DB) | `/health` | [SK API](API_ENDPOINTS.md#semantickernel-service) |
| Gateway | 8000 | - | `/health` | [Gateway API](API_ENDPOINTS.md#gateway) |

### Key Environment Variables

| Variable | Local | Production | See |
|----------|-------|-----------|-----|
| `REDIS_PASSWORD` | `Redis@Secure123!` | Key Vault | [ENVIRONMENT_VARIABLES.md](ENVIRONMENT_VARIABLES.md) |
| `SQL_SA_PASSWORD` | Set in compose | Key Vault | [ENVIRONMENT_VARIABLES.md](ENVIRONMENT_VARIABLES.md) |
| `DAPR_PLACEMENT_PORT` | 50005 | 50005 | [PORT_MAPPING.md](PORT_MAPPING.md) |
| `JWT_SIGNING_KEY` | Dev key | Key Vault | [ENVIRONMENT_VARIABLES.md](ENVIRONMENT_VARIABLES.md) |

### Common Ports

| Port | Service | Type | Use |
|------|---------|------|-----|
| 8000 | API Gateway | HTTP | External requests |
| 6001-6008 | Services | HTTP | Inter-service |
| 5540 | Redis Insight | HTTP | Redis monitoring |
| 8081 | Redis Commander | HTTP | Redis CLI |
| 50005 | DAPR Placement | gRPC | Service discovery |
| 50001 | DAPR Sentry | gRPC | mTLS authority |
| 1433 | SQL Server | SQL | Database |
| 6379 | Redis | Redis | Cache & state |

---

## 📚 Related Categories

All documentation categories provide more detailed information:

- **Architecture:** [Architecture Guide](../architecture/README.md) - System design
- **Development:** [Development Setup](../development/README.md) - Coding reference
- **API Gateway:** [Gateway Documentation](../api-gateway/README.md) - Endpoint details
- **Security:** [Security Guide](../security/README.md) - Authentication details
- **Operations:** [Operations Guide](../operations/README.md) - Monitoring reference

---

## 🔄 Reading Order

Reference documents are designed for lookup, not sequential reading:

1. Use [TROUBLESHOOTING_INDEX.md](TROUBLESHOOTING_INDEX.md) when you have an error
2. Check [GLOSSARY.md](GLOSSARY.md) when you need a definition
3. Reference [PORT_MAPPING.md](PORT_MAPPING.md) for network info
4. Consult [ENVIRONMENT_VARIABLES.md](ENVIRONMENT_VARIABLES.md) for setup
5. Use [API_ENDPOINTS.md](API_ENDPOINTS.md) for API calls

---

## 🔗 Navigation Shortcuts

**By Role:**

| Role | Start With |
|------|-----------|
| **Developer** | [API_ENDPOINTS.md](API_ENDPOINTS.md) + [GLOSSARY.md](GLOSSARY.md) |
| **DevOps** | [PORT_MAPPING.md](PORT_MAPPING.md) + [ENVIRONMENT_VARIABLES.md](ENVIRONMENT_VARIABLES.md) |
| **Debugger** | [TROUBLESHOOTING_INDEX.md](TROUBLESHOOTING_INDEX.md) |
| **New Team Member** | [GLOSSARY.md](GLOSSARY.md) for terms |

**By Task:**

| Task | Document |
|------|----------|
| "What does X mean?" | [GLOSSARY.md](GLOSSARY.md) |
| "What's on port 6001?" | [PORT_MAPPING.md](PORT_MAPPING.md) |
| "How do I set X?" | [ENVIRONMENT_VARIABLES.md](ENVIRONMENT_VARIABLES.md) |
| "How do I call X?" | [API_ENDPOINTS.md](API_ENDPOINTS.md) |
| "X is broken, help!" | [TROUBLESHOOTING_INDEX.md](TROUBLESHOOTING_INDEX.md) |

---

## 💡 Using This Category

### As a Glossary
Look up unfamiliar terms to understand system concepts.

**Example:**
> "What does DAPR mean?"
> → [GLOSSARY.md](GLOSSARY.md) → Distributed Application Runtime → [DAPR Integration](../microservices/DAPR_INTEGRATION.md)

### As a Cheat Sheet
Quick reference without deep documentation.

**Example:**
> "What's the Redis password?"
> → [ENVIRONMENT_VARIABLES.md](ENVIRONMENT_VARIABLES.md) → `REDIS_PASSWORD` → Found!

### As a Troubleshooter
Find issues and link to solutions.

**Example:**
> "Getting 401 Unauthorized"
> → [TROUBLESHOOTING_INDEX.md](TROUBLESHOOTING_INDEX.md) → 401 Unauthorized → [Authentication](../security/AUTHENTICATION.md)

### As an API Reference
Look up endpoints and parameters.

**Example:**
> "How do I list inventory items?"
> → [API_ENDPOINTS.md](API_ENDPOINTS.md) → Inventory Service → GET /api/inventory/items

---

## 🆘 Troubleshooting Quick Links

See [TROUBLESHOOTING_INDEX.md](TROUBLESHOOTING_INDEX.md) for:

- **401/403 Errors** → Authentication/Authorization issues
- **429 Errors** → Rate limiting
- **503 Errors** → Service unavailable
- **Database Errors** → Connection issues
- **Docker Errors** → Container issues
- **And many more...**

---

## ✅ Quick Check

### Before Deployment
- [ ] All environment variables in [ENVIRONMENT_VARIABLES.md](ENVIRONMENT_VARIABLES.md)
- [ ] All ports mapped in [PORT_MAPPING.md](PORT_MAPPING.md)
- [ ] All endpoints documented in [API_ENDPOINTS.md](API_ENDPOINTS.md)
- [ ] All terms understood from [GLOSSARY.md](GLOSSARY.md)

### When Stuck
- [ ] Check [GLOSSARY.md](GLOSSARY.md) for term definitions
- [ ] Check [TROUBLESHOOTING_INDEX.md](TROUBLESHOOTING_INDEX.md) for issue
- [ ] Check [API_ENDPOINTS.md](API_ENDPOINTS.md) for endpoint format
- [ ] Check [ENVIRONMENT_VARIABLES.md](ENVIRONMENT_VARIABLES.md) for setup

---

## 📊 Documentation Coverage

| Category | Items | Status |
|----------|-------|--------|
| **Glossary** | 50+ terms | ✅ |
| **Environment Variables** | 20+ variables | ✅ |
| **Ports** | 15+ ports | ✅ |
| **API Endpoints** | 20+ endpoints | ✅ |
| **Error Codes** | 30+ codes | ✅ |
| **Troubleshooting** | 25+ issues | ✅ |

---

## 🔗 Full Document Map

```
reference/
├── README.md (this file)
├── GLOSSARY.md
├── ENVIRONMENT_VARIABLES.md
├── PORT_MAPPING.md
├── API_ENDPOINTS.md
└── TROUBLESHOOTING_INDEX.md
```

---

## 📞 Next Steps

- **Need a definition?** → [GLOSSARY.md](GLOSSARY.md)
- **Need environment variables?** → [ENVIRONMENT_VARIABLES.md](ENVIRONMENT_VARIABLES.md)
- **Need port info?** → [PORT_MAPPING.md](PORT_MAPPING.md)
- **Need API docs?** → [API_ENDPOINTS.md](API_ENDPOINTS.md)
- **Have a problem?** → [TROUBLESHOOTING_INDEX.md](TROUBLESHOOTING_INDEX.md)

---

**Last Updated:** October 27, 2025  
**Category Status:** ✅ Complete  
**Documents:** 5 files  
**Quick Lookups:** 200+ entries

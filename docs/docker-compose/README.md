# 🐳 Docker-Compose & Local Development

**Local Development Environment Configuration**  
Last Updated: October 27, 2025

---

## 📍 Overview

This category documents the complete local development environment using Docker Compose. All services, databases, caches, and supporting infrastructure run locally with full DAPR integration.

---

## 📚 Documents in This Category

### [DOCKER_COMPOSE_GUIDE.md](DOCKER_COMPOSE_GUIDE.md)
**Complete Docker Compose setup and operations**
- Docker Compose file structure and components
- Service configuration details
- Container networking
- Volume management
- Environment configuration

### [SERVICES_REFERENCE.md](SERVICES_REFERENCE.md)
**Individual service reference guide**
- Each service's role and dependencies
- Service-specific configuration
- Port mappings
- Health check endpoints
- Service startup order

### [DAPR_LOCAL_SETUP.md](DAPR_LOCAL_SETUP.md)
**DAPR configuration for local development**
- DAPR CLI installation and setup
- Sidecar configuration
- Component setup (state store, pub/sub, config)
- Local development patterns
- Testing DAPR functionality

### [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
**Common Docker Compose issues and solutions**
- Container won't start
- Services can't communicate
- Port conflicts
- Database connection issues
- DAPR sidecar problems

---

## 🎯 Quick Start

### 5-Minute Setup

1. **Verify Prerequisites**
   ```bash
   docker --version  # Docker Desktop 4.0+
   docker compose version  # Docker Compose 2.0+
   ```

2. **Start Services**
   ```bash
   cd /path/to/project
   docker compose up -d
   ```

3. **Verify Running**
   ```bash
   docker compose ps
   curl http://localhost:5000/health
   ```

4. **Stop Services**
   ```bash
   docker compose down
   ```

---

## 📊 Services Overview

| Service | Port | Role | Status |
|---------|------|------|--------|
| **SQL Server** | 1433 | Database | ✅ Running |
| **Redis** | 6379 | Cache & State | ✅ Running |
| **Auth Service** | 5001 | Authentication | ✅ Running |
| **Inventory Service** | 5002 | Inventory Mgmt | ✅ Running |
| **Orders Service** | 5003 | Order Processing | ✅ Running |
| **Sales Service** | 5004 | Sales Mgmt | ✅ Running |
| **Billing Service** | 5005 | Billing | ✅ Running |
| **Purchasing Service** | 5006 | Procurement | ✅ Running |
| **API Gateway** | 8000 | Request Router | ✅ Running |
| **DAPR Placement** | 50005 | Service Discovery | ✅ Running |
| **DAPR Sentry** | 50001 | mTLS Authority | ✅ Running |
| **Redis Commander** | 8081 | Redis UI | ✅ Running |
| **Redis Insight** | 5540 | Redis Analytics | ✅ Running |

---

## 🔍 Common Tasks

| Task | Command | See Also |
|------|---------|----------|
| Start everything | `docker compose up -d` | [DOCKER_COMPOSE_GUIDE.md](DOCKER_COMPOSE_GUIDE.md) |
| Stop everything | `docker compose down` | [DOCKER_COMPOSE_GUIDE.md](DOCKER_COMPOSE_GUIDE.md) |
| View logs | `docker compose logs -f [service]` | [DOCKER_COMPOSE_GUIDE.md](DOCKER_COMPOSE_GUIDE.md) |
| Restart service | `docker compose restart [service]` | [SERVICES_REFERENCE.md](SERVICES_REFERENCE.md) |
| Recreate service | `docker compose up -d --force-recreate [service]` | [TROUBLESHOOTING.md](TROUBLESHOOTING.md) |
| Check service health | `curl http://localhost:PORT/health` | [SERVICES_REFERENCE.md](SERVICES_REFERENCE.md) |
| Access database | `docker exec -it sqlserver /opt/mssql-tools/bin/sqlcmd` | [DATABASE_ACCESS.md](../microservices/DATABASE_ACCESS.md) |
| Clean everything | `docker compose down -v` | [DOCKER_COMPOSE_GUIDE.md](DOCKER_COMPOSE_GUIDE.md) |

---

## 📂 Key Files

| File | Purpose |
|------|---------|
| `docker-compose.yml` | Main configuration (local dev) |
| `docker-compose.override.yml` | Local overrides |
| `.env.example` | Environment variables template |
| `deploy/dapr/components/*.yaml` | DAPR component definitions |
| `Dockerfile` | Application container image |
| `docker-compose.dcproj` | Docker project file |

---

## 🔄 Reading Order

1. Start with [DOCKER_COMPOSE_GUIDE.md](DOCKER_COMPOSE_GUIDE.md) to understand the setup
2. Review [SERVICES_REFERENCE.md](SERVICES_REFERENCE.md) for individual services
3. Read [DAPR_LOCAL_SETUP.md](DAPR_LOCAL_SETUP.md) to understand DAPR
4. Bookmark [TROUBLESHOOTING.md](TROUBLESHOOTING.md) for common issues

---

## 📚 Related Categories

- **Development:** [Development Setup](../development/DEVELOPMENT_SETUP.md) - Get your first service running
- **Infrastructure:** [Infrastructure Guide](../infrastructure/README.md) - Production deployment
- **Microservices:** [Microservices Guide](../microservices/README.md) - Service development
- **DAPR Integration:** [DAPR Guide](../microservices/DAPR_INTEGRATION.md) - Distributed patterns

---

## 🐛 Troubleshooting Quick Links

**Problem** | **Solution**
---|---
Docker won't start | [TROUBLESHOOTING.md](TROUBLESHOOTING.md#docker-wont-start)
Services can't communicate | [TROUBLESHOOTING.md](TROUBLESHOOTING.md#services-cant-communicate)
Port already in use | [TROUBLESHOOTING.md](TROUBLESHOOTING.md#port-already-in-use)
Database connection failed | [TROUBLESHOOTING.md](TROUBLESHOOTING.md#database-connection-failed)
DAPR not working | [TROUBLESHOOTING.md](TROUBLESHOOTING.md#dapr-issues)
Service keeps crashing | [TROUBLESHOOTING.md](TROUBLESHOOTING.md#service-crash)

---

## 💡 Key Concepts

### Service Discovery
- DAPR Placement service handles service discovery
- Services discover each other via DAPR, not direct DNS
- Service location transparent to application code

### State Management
- DAPR State Store backed by Redis
- Provides distributed state across services
- Automatic persistence and consistency

### Event System
- DAPR Pub/Sub backed by Redis
- Services publish events to topics
- Other services subscribe and react
- Asynchronous, decoupled communication

### Security
- Services communicate on private bridge network
- Database and Redis password-protected
- DAPR mTLS via Sentry service
- Health checks for service status

---

## ✅ Verification Checklist

When starting local development:

- [ ] Docker Desktop running
- [ ] All containers running (`docker compose ps`)
- [ ] API Gateway health check passes (`curl http://localhost:8000/health`)
- [ ] Can access Redis Commander (`http://localhost:8081`)
- [ ] Can access Redis Insight (`http://localhost:5540`)
- [ ] Auth service running (`curl http://localhost:5001/health`)
- [ ] Database accessible

---

## 🎓 Environment Variables

Key variables (see `.env.example` for complete list):

| Variable | Default | Purpose |
|----------|---------|---------|
| `REDIS_PASSWORD` | `Redis@Secure123!` | Redis authentication |
| `SQL_SA_PASSWORD` | (set in compose) | SQL Server admin password |
| `DAPR_PLACEMENT_PORT` | `50005` | DAPR placement service |
| `DAPR_SENTRY_PORT` | `50001` | DAPR mTLS authority |

---

## 📞 Next Steps

- **First time?** → [Development Setup](../development/DEVELOPMENT_SETUP.md)
- **Want to code?** → [Service Templates](../microservices/SERVICE_TEMPLATES.md)
- **Understanding DAPR?** → [DAPR Integration](../microservices/DAPR_INTEGRATION.md)
- **Deploying to Azure?** → [Deployment Guide](../deployment/DEPLOYMENT_GUIDE.md)

---

## 🔗 Full Document Map

```
docker-compose/
├── README.md (this file)
├── DOCKER_COMPOSE_GUIDE.md
├── SERVICES_REFERENCE.md
├── DAPR_LOCAL_SETUP.md
└── TROUBLESHOOTING.md
```

---

**Last Updated:** October 27, 2025  
**Category Status:** ✅ Complete  
**Documents:** 4 files  
**Services:** 13 total (6 business + 7 infrastructure)

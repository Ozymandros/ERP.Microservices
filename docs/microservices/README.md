# 🔧 Microservices Development

**Service Architecture, DAPR Integration, Database Access & Caching Patterns**  
Last Updated: October 27, 2025

---

## 📍 Overview

This category covers building and maintaining individual microservices. From service templates and DAPR integration to database access patterns and event-driven communication. Designed for service developers and architects.

---

## 📚 Documents in This Category

### [SERVICE_TEMPLATES.md](SERVICE_TEMPLATES.md)
**Microservice architecture and templates**
- Service structure and layers
- Project layout
- Class templates
- API endpoint templates
- Dependency injection setup

### [DAPR_INTEGRATION.md](DAPR_INTEGRATION.md)
**DAPR patterns and implementation**
- Service invocation patterns
- State management
- Configuration retrieval
- Service binding
- Error handling and resilience

### [DATABASE_ACCESS.md](DATABASE_ACCESS.md)
**Data access patterns and best practices**
- Entity Framework Core usage
- Repository pattern
- Migrations and versioning
- Connection pooling
- Transaction management

### [CACHING.md](CACHING.md)
**Redis caching patterns**
- Cache invalidation
- TTL configuration
- Distributed caching
- Cache coherence
- Performance considerations

### [PUB_SUB.md](PUB_SUB.md)
**Event-driven architecture**
- Event publishing
- Event subscription
- Event handlers
- Dead letter queues
- Saga pattern implementation

---

## 🎯 Service Architecture

```
┌──────────────────────────────────────┐
│      API Layer (Controllers)          │
│  - HTTP endpoints                     │
│  - Request validation                 │
│  - Response formatting                │
├──────────────────────────────────────┤
│     Application Layer (Services)      │
│  - Business logic                     │
│  - DAPR calls                         │
│  - Event publishing                   │
├──────────────────────────────────────┤
│      Domain Layer (Models)            │
│  - Domain entities                    │
│  - Business rules                     │
│  - Aggregates                         │
├──────────────────────────────────────┤
│     Infrastructure Layer              │
│  - Database access                    │
│  - Cache interaction                  │
│  - External APIs                      │
└──────────────────────────────────────┘
```

---

## 📊 6 Microservices

| Service | Port | Database | Purpose |
|---------|------|----------|---------|
| **Auth Service** | 5001 | auth-db | Authentication & authorization |
| **Inventory Service** | 5002 | inventory-db | Product inventory |
| **Orders Service** | 5003 | orders-db | Order processing |
| **Sales Service** | 5004 | sales-db | Sales management |
| **Billing Service** | 5005 | billing-db | Billing & invoicing |
| **Purchasing Service** | 5006 | purchasing-db | Procurement |

---

## 🔄 Common Service Patterns

### Pattern 1: Synchronous Call
```csharp
// Service A calls Service B via HTTP (direct call)
var response = await _httpClient.GetAsync(
    "http://inventory-service/api/products/123");
```

### Pattern 2: DAPR Service Invocation
```csharp
// Service A calls Service B via DAPR (resilient)
var response = await _daprClient.InvokeMethodAsync(
    HttpMethod.Get, 
    "inventory-service", 
    "/api/products/123");
```

### Pattern 3: Event Publishing
```csharp
// Service publishes event
await _daprClient.PublishEventAsync(
    "pubsub", 
    "order-created", 
    orderEvent);
```

### Pattern 4: Event Subscription
```csharp
// Service subscribes to events
[Topic("pubsub", "order-created")]
public async Task HandleOrderCreated(OrderCreatedEvent @event)
{
    // Handle event
}
```

---

## 📚 Related Categories

- **Architecture:** [Architecture Guide](../architecture/README.md) - System design
- **Docker:** [Docker Compose](../docker-compose/README.md) - Local dev
- **Development:** [Development Setup](../development/README.md) - Coding practices
- **Operations:** [Operations Guide](../operations/README.md) - Production support

---

## 🔄 Reading Order

1. Start with [SERVICE_TEMPLATES.md](SERVICE_TEMPLATES.md) for structure
2. Read [DAPR_INTEGRATION.md](DAPR_INTEGRATION.md) for communication
3. Study [DATABASE_ACCESS.md](DATABASE_ACCESS.md) for data
4. Learn [CACHING.md](CACHING.md) for performance
5. Reference [PUB_SUB.md](PUB_SUB.md) for events

---

## 💡 Key Concepts

### Service Boundaries
- Each service owns its data
- Services communicate via APIs
- No shared databases
- Separate deployments

### DAPR Benefits
- Language-agnostic service invocation
- Built-in service discovery
- Automatic retries and timeouts
- Distributed tracing
- Security (mTLS)

### Event-Driven
- Services publish domain events
- Other services subscribe
- Asynchronous, decoupled
- Improved scalability
- Better resilience

### Caching Strategy
- Cache for read-heavy operations
- TTL based on data freshness
- Invalidate on updates
- Distributed across services
- Performance improvement

---

## ✅ Service Checklist

When creating a new service:

- [ ] Project structure created
- [ ] API endpoints defined
- [ ] Database schema designed
- [ ] Connection string configured
- [ ] DAPR integration added
- [ ] Events defined and published
- [ ] Event handlers implemented
- [ ] Caching strategy implemented
- [ ] Error handling added
- [ ] Logging configured
- [ ] Health check endpoint added
- [ ] Integration tests written
- [ ] Documentation complete

---

## 🚀 Service Development Workflow

```
1. Define Service Contract
   - API endpoints
   - Events to publish
   - Events to subscribe
   ↓
2. Create Project Structure
   - Use template
   - Setup dependencies
   ↓
3. Implement API Layer
   - Controllers
   - Request/response models
   ↓
4. Implement Services
   - Business logic
   - DAPR calls
   - Event publishing
   ↓
5. Implement Repository
   - Database access
   - Migrations
   ↓
6. Add Caching
   - Identify hot paths
   - Implement cache layer
   ↓
7. Event Handlers
   - Subscribe to events
   - Handle async processing
   ↓
8. Testing
   - Unit tests
   - Integration tests
   ↓
9. Documentation
   - API documentation
   - Configuration guide
```

---

## 📞 Next Steps

- **New service?** → [SERVICE_TEMPLATES.md](SERVICE_TEMPLATES.md)
- **DAPR integration?** → [DAPR_INTEGRATION.md](DAPR_INTEGRATION.md)
- **Database access?** → [DATABASE_ACCESS.md](DATABASE_ACCESS.md)
- **Caching needed?** → [CACHING.md](CACHING.md)
- **Events?** → [PUB_SUB.md](PUB_SUB.md)

---

## 🔗 Full Document Map

```
microservices/
├── README.md (this file)
├── SERVICE_TEMPLATES.md
├── DAPR_INTEGRATION.md
├── DATABASE_ACCESS.md
├── CACHING.md
└── PUB_SUB.md
```

---

**Last Updated:** October 27, 2025  
**Category Status:** ✅ Complete  
**Documents:** 5 files  
**Services:** 6 total

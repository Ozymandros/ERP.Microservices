# GitHub Copilot Instructions - ERP Microservices

**Purpose:** Guide AI coding agents to be immediately productive in this .NET microservices platform.  
**Last Updated:** October 31, 2025  
**Status:** ✅ Complete

---

## 🏗️ Architecture Overview

This is a **cloud-native ERP system** with 6 independent microservices deployed on Azure Container Apps.

### Key Components
- **Services:** Auth, Billing, Inventory, Orders, Purchasing, Sales (6 total)
- **API Gateway:** Ocelot-based reverse proxy (external HTTPS endpoint)
- **Message Broker:** Azure Redis (DAPR pub/sub, state store)
- **Database:** SQL Server with 1 database per microservice (database per service pattern)
- **Orchestration:** .NET Aspire for local dev, Container Apps for production
- **Service Mesh:** DAPR (Distributed Application Runtime) for communication

### Critical Data Flow
```
Client → API Gateway (Ocelot) → Service (via HTTP or DAPR)
                                    ↓
                              Service DB (SQL)
                              ↓
                              Redis (cache/state)
```

All services run inside a private Container Apps Environment. Only the Gateway has external ingress (HTTPS).

---

## 📁 Project Structure Pattern

Every microservice follows **Clean Architecture with 4 layers:**

```
MyApp.[Service]/
├── MyApp.[Service].API/           # Web API layer (Controllers)
├── MyApp.[Service].Application/   # Business logic (Services)
├── MyApp.[Service].Domain/        # Domain entities (no dependencies)
└── MyApp.[Service].Infrastructure/ # Data access (DbContext, Repos)
```

**Key files:**
- `Program.cs` - DI registration, middleware setup, health checks
- `[Service]DbContext.cs` - Entity Framework configuration
- `Repositories/` - Data access patterns (Repository pattern)
- `Services/` - Application logic, DAPR integration
- `Controllers/` - HTTP endpoints

**Shared code lives in** `MyApp.Shared/`:
- `MyApp.Shared.Domain` - Shared entities, interfaces
- `MyApp.Shared.CQRS` - CQRS helpers
- `MyApp.Shared.Infrastructure` - Database utilities, caching

---

## 🎯 Developer Workflows

### Local Development (Primary)
```bash
cd AppHost
dotnet run
```
This launches **Aspire dashboard** with all 7 services (6 microservices + API Gateway) running locally in Docker. Aspire auto-discovers services and ports.

### Service-Specific Development
```bash
cd MyApp.Auth/MyApp.Auth.API
dotnet run
```
Runs single service with local dependencies (Redis, SQL Server from docker-compose or Aspire). Port: 5000 + service offset.

### Build & Test
```bash
dotnet build                    # Build all projects
dotnet test                     # Run all tests
dotnet test MyApp.Auth.Tests/   # Run service tests
```

### Deployment to Azure
```bash
azd up
```
Uses `azure.yaml` configuration to deploy entire stack. One command provisions infrastructure (Bicep) and deploys services.

---

## 🔌 Integration Patterns

### Service-to-Service Communication

**Synchronous (HTTP):**
```csharp
// Use DAPR service invocation within Container Apps
var httpClient = _daprClient.CreateInvokeHttpClient("billing-service");
var result = await httpClient.GetAsync("/api/billing/invoices");
```

**Asynchronous (Event-Driven):**
```csharp
// Publish event to Redis pub/sub via DAPR
await _daprClient.PublishEventAsync("pubsub", "order-created", new OrderCreatedEvent { OrderId = 123 });

// Subscribe in another service (Program.cs)
app.MapPost("/order-created", OrderCreatedHandler);
```

### Database Access
- Each service has **own SQL database** and **own DbContext**
- Connection strings from `appsettings.json` or Azure Container Apps secrets
- Use **Entity Framework Core** for all data access (no raw SQL)
- Migrations per service: `dotnet ef migrations add MigrationName`

### Caching (Redis)
```csharp
// Registered in DI: ICacheService
public class MyService
{
    private readonly ICacheService _cache;
    
    public async Task<User> GetUserAsync(int id)
    {
        var cacheKey = $"user:{id}";
        return await _cache.GetAsync<User>(cacheKey) 
            ?? await _cache.SetAsync(cacheKey, FetchFromDb(id));
    }
}
```

### Configuration Management
- **Local:** `appsettings.Development.json`
- **Production:** Azure Container Apps secrets + Azure App Configuration
- JWT secrets stored in Azure Key Vault (never committed)

---

## 🛠️ Project-Specific Conventions

### Naming
- **Namespaces:** `MyApp.[Service].[Layer]`
- **DbContexts:** `[Service]DbContext` (e.g., `AuthDbContext`)
- **Repositories:** `I[Entity]Repository` interfaces (e.g., `IUserRepository`)
- **Services:** `[Entity]Service` or `[Action]Service` (e.g., `UserService`, `AuthenticationService`)
- **Controllers:** `[Entity]Controller` (e.g., `UsersController`)

### Database Patterns
- Each service has **one DbContext** per database
- Entities inherit from `AggregateRoot` or domain base classes
- Use `DbSet<T>` for each aggregate root
- `OnModelCreating()` for Fluent API configuration
- No cross-database queries (data flows via events or API calls)

### API Endpoints
- Route prefix: `/api/[service]/` (e.g., `/api/auth/users`, `/api/billing/invoices`)
- Return `IActionResult` with proper HTTP status codes
- Use `[Authorize]` for protected endpoints
- Return DTOs (Data Transfer Objects), not domain entities

### DAPR Integration
- **App ID** matches service name (auth-service, billing-service, etc.)
- `DaprClient` injected as singleton in DI
- Use `InvokeMethodAsync` for service-to-service calls
- Use `PublishEventAsync` for pub/sub events
- Event names are kebab-case: "order-created", "invoice-paid"

---

## ✅ Critical Patterns (Do This)

### ✅ Clean Architecture Layers
- **Domain:** Pure business logic, no DB/HTTP/external deps
- **Application:** Use cases, DAPR calls, orchestration
- **Infrastructure:** EF Core, repositories, external APIs
- **API:** Controllers only, thin HTTP layer

### ✅ Dependency Injection
Register all services in `Program.cs`:
```csharp
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddDbContext<AuthDbContext>(options => 
    options.UseSqlServer(connectionString));
```

### ✅ Entity Framework Migrations
Per-service migrations in each Infrastructure project:
```bash
cd MyApp.Auth/MyApp.Auth.Infrastructure
dotnet ef migrations add AddUserRoles
dotnet ef database update
```

### ✅ Health Checks
Register in `Program.cs`:
```csharp
builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString)
    .AddRedis(redisConnection);
```
Aspire and Container Apps use this for readiness/liveness probes.

### ✅ Error Handling
```csharp
try
{
    // Operation
}
catch (InvalidOperationException ex)
{
    // Log and return appropriate HTTP status
    return BadRequest(new { error = ex.Message });
}
```

---

## ❌ Anti-Patterns (Avoid This)

### ❌ Cross-Service Database Access
**Wrong:** Querying another service's database directly  
**Right:** Call that service's API or wait for event

### ❌ Storing Secrets in Code
**Wrong:** JWT keys in `appsettings.json`  
**Right:** Azure Key Vault (local: User Secrets, prod: Container Apps secrets)

### ❌ Tight Coupling Between Services
**Wrong:** Direct entity references between services  
**Right:** DTOs and DAPR events

### ❌ Synchronous for Everything
**Wrong:** All service calls via HTTP with timeout risks  
**Right:** Use HTTP for commands, pub/sub for notifications

### ❌ Skipping Health Checks
**Wrong:** Services without health endpoints  
**Right:** All services expose `/health` with DB/Redis checks

---

## 📚 Key Files to Understand

### Infrastructure (Bicep)
- **`infra/main.bicep`** - Orchestrates all Azure resources
- **`infra/core/`** - Reusable modules (Container Apps, SQL, Redis, Key Vault)
- **`infra/services/`** - Per-service deployment modules
- **`infra/main.parameters.json`** - Environment-specific values

### Aspire Orchestration
- **`AppHost/Program.cs`** - Defines 6 services, Redis, SQL Server, gateway routing
- **`AppHost/AspireProjectBuilder.cs`** - Custom builder for port management
- **`appsettings.Development.json`** - JWT config, frontend CORS

### API Gateway
- **`ErpApiGateway/ocelot.json`** - Route mappings (local dev)
- **`ErpApiGateway/ocelot.Production.json`** - Production routes
- **`ErpApiGateway/Program.cs`** - Ocelot setup with HTTPS, rate limiting, auth

### CI/CD
- **`.github/workflows/azure-deploy.yml`** - Deploys on push to `main`
- **`.github/workflows/dotnet.yml`** - Runs tests on PR
- **`azure.yaml`** - Azure Developer CLI configuration

### Documentation
- **`docs/architecture/ARCHITECTURE_DOCUMENTATION.md`** - Complete system overview
- **`docs/deployment/DEPLOYMENT.md`** - Deployment procedures
- **`docs/development/add-dependencies.prompt.md`** - Project reference rules

---

## 🚀 Common Tasks

### Adding a New Service Endpoint
1. Create controller in `[Service].API/Controllers/`
2. Add service logic in `[Service].Application/Services/`
3. Add repository in `[Service].Infrastructure/Repositories/`
4. Register in DI (`Program.cs`)
5. Add route in API Gateway config (`ocelot.json`)

### Publishing an Event
```csharp
// Service publishes event
await _daprClient.PublishEventAsync("pubsub", "invoice-created", new InvoiceCreatedEvent { InvoiceId = 123 });

// Another service subscribes in Program.cs
app.MapPost("/events/invoice-created", HandleInvoiceCreated);
```

### Adding a Database Migration
```bash
cd [Service]/[Service].Infrastructure
dotnet ef migrations add DescriptiveNameHere
dotnet ef database update
```

### Calling Another Service
```csharp
var httpClient = _daprClient.CreateInvokeHttpClient("billing-service");
var invoices = await httpClient.GetAsync("/api/billing/invoices");
```

### Configuring a New Environment Variable
1. Add to `azure.yaml` `parameters` section
2. Add to `appsettings.Development.json` (local)
3. Add to Azure Container Apps secret/config (prod)

---

## 🔍 Code Navigation Tips

- **Find service endpoints:** Look for `[ApiController]` in `[Service].API/Controllers/`
- **Find database models:** Look for `DbSet<T>` in `[Service]DbContext.cs`
- **Find business logic:** Look in `[Service].Application/Services/`
- **Find routes:** Gateway routes in `ErpApiGateway/ocelot.json`
- **Find infrastructure config:** Main infrastructure in `infra/main.bicep`

---

## 📖 Documentation to Read First

**For understanding the system:**
1. `docs/architecture/ARCHITECTURE_DOCUMENTATION.md` - System overview
2. `README.md` - Project features and quick start

**For deployment:**
1. `docs/deployment/DEPLOYMENT.md` - How to deploy
2. `azure.yaml` - Configuration for Azure deployment

**For development:**
1. `docs/development/add-dependencies.prompt.md` - Project reference rules
2. `docs/CONVENTIONS.md` - Code formatting and documentation standards

---

## 💡 Quick Reference

| Task | Command | Location |
|------|---------|----------|
| Run locally | `cd AppHost && dotnet run` | Root |
| Build all | `dotnet build` | Root |
| Run tests | `dotnet test` | Root |
| Deploy | `azd up` | Root |
| Add migration | `dotnet ef migrations add Name` | Service Infrastructure |
| View architecture | See `ARCHITECTURE_DOCUMENTATION.md` | `docs/architecture/` |
| Check routes | See `ocelot.json` | `ErpApiGateway/` |
| Update DB | `dotnet ef database update` | Service Infrastructure |

---

## 🆘 When Stuck

- **Service won't start?** Check `appsettings.Development.json` and connection strings
- **DAPR call failing?** Verify `app-id` matches service name
- **Port conflicts?** Aspire auto-assigns ports; check dashboard
- **DB migration issues?** Run in service's Infrastructure project folder
- **Route not working?** Update `ocelot.json` and restart gateway
- **Tests failing?** Ensure test project references are correct (see `add-dependencies.prompt.md`)

---

**Questions?** Check `docs/` directory or see `README.md` for support links.

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A .NET 10 (C# 13) ERP system built as independent microservices, orchestrated locally with .NET Aspire and deployed to Azure Container Apps. Services communicate through Dapr (HTTP invocation + pub/sub) and are fronted by an Ocelot API gateway. Each service owns its own SQL Server database — never access another service's database or DbContext directly.

**Microservices** (defined in `src/AppHost/Program.cs`): Auth, Billing, Crm, Inventory, Orders, Purchasing, Sales, Agentic, Audit — each gets a `{Service}DB` SQL Server database. Note: `Program.cs` comments claim Agentic and Audit have "no DB", but both are registered with `hasDatabase: true`; this is a known inconsistency in the source, not settled fact — verify against `AspireProjectBuilder.cs` if it matters for your task.

Data flow: `Client → API Gateway (Ocelot) → Microservice → Dapr Sidecar → Database/Redis/Other Services`.

## Commands

```powershell
# Run full stack (Aspire dashboard orchestrates all services + gateway)
cd src/AppHost
dotnet run

# Run a single service for focused debugging
cd src/MyApp.Auth/MyApp.Auth.API
dotnet run

# Build / test everything
dotnet build
dotnet test ERP.Microservices.sln
dotnet build -c Release

# Run a single test project
dotnet test src/MyApp.Billing/test/MyApp.Billing.Application.Tests

# Run a single test by name/filter
dotnet test --filter "FullyQualifiedName~OrderServiceTests.CreateAsync_WithValidDto_CreatesOperationalOrder"

# Coverage (matches CI)
dotnet test ERP.Microservices.sln --collect:"XPlat Code Coverage" --results-directory ./TestResults/Coverage

# EF Core migrations — MUST be run from the service's Infrastructure project directory
cd src/MyApp.[Service]/MyApp.[Service].Infrastructure
dotnet ef migrations add <MigrationName>
dotnet ef database update

# Deploy / manage Azure environment
azd up
azd monitor
azd down
```

npm-style aliases exist in root `package.json` (`npm test`, `npm run build`) — they just shell out to the `dotnet` commands above.

## Architecture

### Clean Architecture per service

Every microservice under `src/MyApp.[Service]/` follows a strict 4-project layout, and dependencies flow inward only:

```
MyApp.[Service].API/              # Controllers, Program.cs (DI/middleware), appsettings
MyApp.[Service].Application/      # Services/ (business logic), Mapping/ (AutoMapper profiles)
MyApp.[Service].Application.Contracts/  # Service interfaces, DTOs
MyApp.[Service].Domain/           # Entities/, Repositories/ (interfaces only) — zero external deps
MyApp.[Service].Infrastructure/   # DbContext, Repositories/ (impl), Configurations/, external clients
```

Unit tests live alongside each service in `src/MyApp.[Service]/test/MyApp.[Service].{Domain,Application,Infrastructure,API}.Tests/`. `src/AppHost.Tests/Tests/` holds Aspire-based **integration** tests that spin up the whole distributed application and hit it through the gateway (`DistributedApplicationTestingBuilder`).

Shared cross-cutting code lives in `src/MyApp.Shared/`:
- `MyApp.Shared.Domain` — base entities (`AuditableEntity<TId>`), repository interfaces, `QuerySpec` (filtering/sorting/pagination DTO), permissions, caching abstractions
- `MyApp.Shared.Infrastructure` — logging, Redis caching, EF repository base, Excel/PDF export, messaging, security
- `MyApp.Shared.SignalR` — real-time communication
- `MyApp.Shared.CQRS` — currently an empty stub project (no source files); don't assume CQRS base classes exist there without checking first

Only genuinely cross-cutting concerns belong in `Shared`; service-specific logic stays in that service.

### Bounded context separation — the most important rule in this repo

Operational and commercial concerns must never mix. This has been a recurring source of real bugs (see `docs/AGENT_INSTRUCTIONS_AUDIT_REPORT.md`), so check it explicitly before adding fields to an entity:

| Service | Type | CustomerId? | SupplierId? | Pricing? | Purpose |
|---|---|---|---|---|---|
| `MyApp.Orders` | Operational | ❌ NEVER | ❌ NEVER | ❌ NEVER | Physical logistics movement |
| `MyApp.Sales` | Commercial | ✅ ALWAYS | ❌ NO | ✅ YES | Customer sales transactions |
| `MyApp.Purchasing` | Supply chain | ❌ NO | ✅ ALWAYS | ✅ YES | Supplier procurement |
| `MyApp.Inventory` | Core | ❌ NO | ❌ NO | ✅ YES (UnitPrice) | Stock management |
| `MyApp.Auth` | Infrastructure | N/A | N/A | ❌ NO | AuthN/AuthZ |
| `MyApp.Billing` | Commercial | ✅ YES | ✅ YES | ✅ YES | Invoicing/payments |

`Orders` tracks *where a shipment is*, not *who paid what*. It uses `OrderType` (Transfer/Inbound/Outbound/Return), `SourceId`/`TargetId` (warehouses), and `ExternalOrderId` to link back to the `SalesOrder`/`PurchaseOrder` that caused the movement. It must never gain `CustomerId`, `SupplierId`, `TotalAmount`, `UnitPrice`, `LineTotal`, or other pricing/commercial fields — those belong in Sales, Purchasing, or Billing. Before adding a field anywhere, ask: is this about physical movement (Orders), selling (Sales), buying (Purchasing), stock levels (Inventory), invoicing (Billing), or users/permissions (Auth)?

### Service communication (Dapr)

Service-to-service calls always go through Dapr, never direct HTTP to another service's URL and never a shared DbContext:

```csharp
var httpClient = _daprClient.CreateInvokeHttpClient("inventory-service");
var response = await httpClient.GetAsync($"/api/inventory/products/{productId}");
```

App-IDs are `{service}-service` (e.g. `auth-service`, `billing-service`, `orders-service`). Fire-and-forget notifications use Dapr pub/sub (`PublishEventAsync` with component name `pubsub`, kebab-case topic names like `order-created`); subscribers are minimal-API endpoints registered in `Program.cs`. Prefer Dapr HTTP invocation when an immediate response is needed, pub/sub for notifications/background processing.

Port allocation is centralized in `src/AppHost/AspireProjectBuilder.cs`: HTTP ports start at 6001 and increment per service in registration order (see `src/AppHost/Program.cs` comments for the current mapping), Dapr HTTP sidecar ports start at 3501, gRPC at 46001, metrics at 9091.

### Gateway routing

The gateway (`src/ErpApiGateway`) uses **Ocelot** (not YARP, despite what the root README says — a commented-out YARP config exists in `AppHost/Program.cs` as a historical alternative but is not active). Routes live in `src/ErpApiGateway/ocelot.json` (+ `ocelot.Development.json` / `ocelot.Production.json`), mapping `UpstreamPathTemplate` (`/[service]/api/...`) to each service's `DownstreamHostAndPorts`. New controllers need a corresponding route added here.

### Entity / DTO / controller conventions

- Entities inherit from `AuditableEntity<TId>` using primary constructors (`public class Order(Guid id) : AuditableEntity<Guid>(id)`), giving `Id`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` for free.
- DTOs are `record` types with `init` accessors. Read DTOs inherit `AuditableGuidDto`. Enums are the enum type in Create/Update DTOs, but mapped to `string` in Read DTOs (AutoMapper `ForMember(... MapFrom(s => s.Status.ToString()))`) — a mapping profile without this conversion will fail.
- Repositories: interface in `Domain/Repositories/`, implementation in `Infrastructure/Data/Repositories/`, always `Include()` navigation properties needed by callers, always call `SaveChangesAsync()` after mutations, use `AsNoTracking()` for read-only queries.
- Controllers: `[ApiController]`, `[Route("api/[controller]")]`, `[Authorize]` by default with `[AllowAnonymous]` overrides where needed. Every controller with list operations needs `export-xlsx`/`export-pdf` endpoints (via `MyApp.Shared.Infrastructure.Export`), placed before the CRUD endpoints.
- New endpoint workflow: Domain entity → DTOs in `Application.Contracts` → repository interface (Domain) + implementation (Infrastructure) → service interface + implementation (Application, inject repo/mapper/cache/logger, cache-aside pattern with `ICacheService`) → controller → DI registration in `Program.cs` → gateway route in `ocelot.json` → migration.

### Caching, logging, security

- Cache-aside via `ICacheService` (Redis-backed): try cache → miss → fetch → map → `SetAsync` with a TTL; invalidate (`RemoveAsync`) on writes.
- Structured logging only (`_logger.LogInformation("... {Field}", value)`), never string-interpolate PII/secrets (especially passwords/tokens) into log messages.
- Never hardcode secrets — pull from `IConfiguration`/Key Vault and throw if missing. Never use `.Result`/`.Wait()` on async code. EF Core parameterized queries only (no raw string-concatenated SQL).
- JWT Bearer auth is configured per-service in `Program.cs`; the signing secret is injected as an Aspire secret parameter (`Jwt__SecretKey`) from `src/AppHost/Program.cs`, not per-service config, in local dev.

## Editing existing agent-instruction files

This repo has several overlapping AI-agent instruction files: `.cursor/rules/*.mdc`, `.agent/rules/RULES.md` (Antigravity), `.github/copilot-instructions.md`, and `AGENTS.md`. `docs/AGENT_INSTRUCTIONS_AUDIT_REPORT.md` documents known drift between them (e.g. `.cursor/rules/00-context-architecture.mdc` and `project-rules.mdc` still describe Orders as sales-order processing, which contradicts the bounded-context rule above — treat `.agent/rules/RULES.md` as the more accurate/current source when they disagree). If asked to update agent instructions, keep them consistent with each other rather than editing just one.

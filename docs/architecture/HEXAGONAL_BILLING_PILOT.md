# Hexagonal architecture pilot (Billing)

Billing is the reference implementation for explicit **ports + use cases** while keeping existing project names and Dapr messaging.

## Layering (unchanged project names)

| Project | Role |
|---------|------|
| `MyApp.Billing.API` | Driving adapters: controllers, Dapr event handlers |
| `MyApp.Billing.Application.Contracts` | Public façade interfaces (`IInvoiceService`), DTOs, **driven port** interfaces (`Ports/`) |
| `MyApp.Billing.Application` | Use cases, thin facades, mapping |
| `MyApp.Billing.Domain` | Aggregates, repository **ports** |
| `MyApp.Billing.Infrastructure` | EF repositories, **port adapters** (e.g. Sales lookup via Dapr) |

## Rules

1. **Controllers and event handlers** call `IInvoiceService` / `ISupplierInvoiceService` only (stable HTTP and pub/sub surface).
2. **Writes and integration flows** live in `Application/UseCases/**` as plain DI classes (no in-process mediator).
3. **Domain behavior** stays on aggregates (`Invoice.Issue`, `RecordPayment`, …); use cases orchestrate persistence and `IEventPublisher`.
4. **Cross-service reads** use Billing-owned snapshots in `Application.Contracts` + `IService*Port` implemented in `Infrastructure/Adapters` (no reference to other services' Contracts assemblies from Application).
5. **Dapr pub/sub** unchanged: use cases publish via `IEventPublisher`; topic names and payloads are not renamed in this pattern.
6. **Shared Dapr ports** (`IServiceInvoker`, `IUnitOfWork`, `IEventPublisher`) remain in `MyApp.Shared.*`.

## Folder layout (Billing)

```text
Application.Contracts/
  DTOs/                    # includes anti-corruption snapshots (e.g. SalesOrderSnapshot)
  Ports/                   # ISalesOrderLookupPort, ...
  Services/                # IInvoiceService driving port

Application/
  UseCases/Invoices/       # CreateInvoiceUseCase, ...
  Services/                # thin facades delegating to use cases
  Mapping/                 # BillingInvoiceMapper
  DependencyInjection/     # AddBillingApplication()
  Internals/               # BillingTransactionBoundary (UoW + audit)

Infrastructure/
  Adapters/                # SalesOrderLookupAdapter
  Repositories/            # EF implementations of domain repository ports
```

## DI (API `Program.cs`)

- Register repository + `IUnitOfWork` implementations (existing).
- Register driven adapters: `ISalesOrderLookupPort` → `SalesOrderLookupAdapter`.
- Call `services.AddBillingApplication()` (use cases + `IBillingTransactionBoundary`).
- Register facades: `IInvoiceService`, `ISupplierInvoiceService`.

## Checklist for the next service

1. Identify cross-service **Contracts leaks** in Application; replace with local snapshot DTOs + port + Infrastructure adapter.
2. Extract **mutating** methods from fat `*Service` into `UseCases/{Aggregate}/`.
3. Keep **read/query** methods on the façade until a dedicated query port is needed.
4. Leave **repository interfaces** in Domain; add **integration ports** in `Application.Contracts/Ports`.
5. Add **façade tests** (mock ports) and **use-case tests** for critical flows.
6. Do not change Dapr topics, gateway routes, or HTTP DTOs in the porting PR.
7. Remove unused local Command/Query stub files if present (they are not Dapr).

## Known residuals (Billing)

- `QueryInvoicesAsync(ISpecification<Invoice>)` still exposes a domain type on `IInvoiceService`; tightening this requires an API-level query DTO pass.
- Redis caching remains in API controllers/event handlers.
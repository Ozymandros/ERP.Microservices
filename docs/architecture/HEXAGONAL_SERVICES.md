# Hexagonal architecture — ERP services rollout

Reference: [Billing pilot detail](HEXAGONAL_BILLING_PILOT.md).

## Per-service status

| Service | Status | Ports (outbound) | Notes |
|---------|--------|------------------|--------|
| Billing | Done | `ISalesOrderLookupPort` | Template service |
| Orders | Done | `IInventoryStockPort` | Use cases + thin `OrderService` |
| Sales | Done | `IOrdersFulfillmentPort`, `IInventoryAvailabilityPort` | No foreign Contracts/Domain on Application |
| Purchasing | Done | `IOrdersOperationalPort`, `ISupplierInvoiceQueryPort` | Receive/match use cases |
| Projects | Done | `ISalesCustomerLookupPort`, `IInventoryProductLookupPort`, `IOrdersCreationPort` | Integration → Infrastructure/Adapters |
| Inventory | Done (products) | — | Product CRUD use cases; WarehouseStock façade unchanged (follow-up) |
| Crm | Done | `ISalesQuoteCreationPort` | Opportunity → Sales quote ACL |
| Auth / Audit / Agentic | Deferred | — | Wave 7 |

## Copy-paste checklist (every service)

1. Add `Application.Contracts/Ports/` + ACL snapshots in `DTOs/`.
2. Add `Infrastructure/Adapters/` implementing ports via `IServiceInvoker` (wire DTOs, no foreign project refs on Application).
3. Add `{Service}TransactionBoundary` in `Application/Internals/` (mirror Billing).
4. Extract **write/integration** flows to `Application/UseCases/{Aggregate}/`.
5. Thin `*Service` facades; controllers keep `I*Service`.
6. `Add{Service}Application()` + register ports in API `Program.cs`.
7. Remove foreign `*.Application.Contracts` / `*.Domain` from Application `.csproj`.
8. Tests: façade mocks ports; at least one adapter test with `InternalsVisibleTo` if wire types are internal.
9. Trim service `Dockerfile` foreign COPY layers.
10. Do not rename Dapr topics or HTTP routes.

## Port catalog (cross-service)

| Port | Owner service | Calls |
|------|---------------|--------|
| `ISalesOrderLookupPort` | Billing | Sales GET order |
| `IInventoryStockPort` | Orders | Inventory reserve / release reservation |
| `IOrdersFulfillmentPort` | Sales | Orders create with reservation, return |
| `IInventoryAvailabilityPort` | Sales | Inventory stock check |
| `IOrdersOperationalPort` | Purchasing | Orders create + fulfill inbound |
| `ISupplierInvoiceQueryPort` | Purchasing | Billing supplier invoices by PO |
| `ISalesCustomerLookupPort` | Projects | Sales GET customer |
| `IInventoryProductLookupPort` | Projects | Inventory GET product |
| `IOrdersCreationPort` | Projects | Orders create with reservation |
| `ISalesQuoteCreationPort` | Crm | Sales POST quote |

## Residual patterns (acceptable until API pass)

- `Query*Async(ISpecification<TEntity>)` on façade interfaces exposes domain types.
- Redis cache in API controllers / event handlers.
- `WarehouseStockService` still monolithic (Inventory wave 5b).
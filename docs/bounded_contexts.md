# Domain Inter-Communication Model (Bounded Contexts)

This document describes the interaction between the different microservices according to the "Constitution" of the ERP.

## 1. Bounded Context Map

```mermaid
graph TD
    subgraph COMMERCIAL_ZONE [Commercial Zone]
        Sales[MyApp.Sales <br/><i>(Revenue, CustomerId, Pricing)</i>]
        Purchasing[MyApp.Purchasing <br/><i>(Supply, SupplierId, Cost)</i>]
        Billing[MyApp.Billing <br/><i>(Invoices, Taxes, Payments)</i>]
    end

    subgraph OPERATIONAL_ZONE [Operational Zone]
        Orders[MyApp.Orders <br/><i>(Logistics, Movements, Tracking)</i>]
    end

    subgraph CORE_ZONE [Core Zone]
        Inventory[MyApp.Inventory <br/><i>(Stock, Products, Warehouses)</i>]
    end

    subgraph INFRA_ZONE [Infrastructure Zone]
        Auth[MyApp.Auth <br/><i>(Users, Roles, Permissions)</i>]
    end

    %% Interactions
    Sales -- "1. Confirm Quote" --> Orders
    Purchasing -- "1. Approve PO" --> Orders
    
    Orders -- "2. Reserve/Adjust" --> Inventory
    Orders -- "3. Fulfill/Ship" --> Billing
    
    Billing -- "Payment Received" --> Sales
    Inventory -- "Low Stock Alert" --> Purchasing
```

## 2. Communication Patterns

### A. Sales to Orders (Outbound Movement)

When a Quote is confirmed in **Sales**, it triggers the creation of an **Outbound Order** in **Orders**.

- **Commercial Data (Sales)**: CustomerId, Final Price, Discounts.
- **Operational Data (Orders)**: Source Warehouse, Target Logistics ID, Destination Address.
- **Link**: `ExternalOrderId` in Orders refers to the `SalesOrderId`.

### B. Purchasing to Orders (Inbound Movement)

When a Purchase Order is approved for receipt in **Purchasing**, it should create an **Inbound Order** in **Orders**.

- **Commercial Data (Purchasing)**: SupplierId, Cost Price, Payment Terms.
- **Operational Data (Orders)**: Source Provider ID, Target Warehouse.
- **Link**: `ExternalOrderId` in Orders refers to the `PurchaseOrderId`.

### C. Orders to Inventory (Stock Management)

**Orders** is the only microservice that should trigger "physical" changes in **Inventory**.

- **Picking**: Reserves stock.
- **Shipping**: Decrements stock.
- **Receiving**: Increments stock.

## 3. Jerarquia de Traçabilitat (Traceability)

Per mantenir la independència de dominis, la traçabilitat segueix una cadena de referències:

1. **Capa Comercial (Sales/Purchasing)**: Té la lògica de negoci, preus i clients/proveïdors.
2. **Capa Operacional (Orders)**: Actua de pont. Guarda l'ID del document comercial a `ExternalOrderId`.
3. **Capa Core (Inventory)**: Registra el moviment físic. Només coneix l'ID de l'ordre operacional a `OrderId`.

**Flux de Referències:**
`InventoryTransaction` → `Order (Operational)` → `SalesOrder / PurchaseOrder`

---

## 4. Communication Protocol (Technical)

We use **Dapr Service Invocation** for synchronous orchestration and **Dapr Pub/Sub** for asynchronous notifications.

| Event | Publisher | Subscriber | Action |
| :--- | :--- | :--- | :--- |
| `sales.order.confirmed` | Sales | Orders | Create Outbound Order |
| `purchasing.order.approved` | Purchasing | Orders | Create Inbound Order |
| `orders.inventory.reserve` | Orders | Inventory | Reserve Stock |
| `orders.order.completed` | Orders | Billing | Generate Invoice |
| `billing.invoice.paid` | Billing | Sales | Update Sales Status |

---

## 5. Current Audit (Updated 2026-01-23)

### 1. Orders Refactoring ✅

- **Removed**: `CustomerId`, `TotalAmount`, `UnitPrice`.
- **Added**: `OrderType` (Transfer, Inbound, Outbound, Return), `SourceId`, `TargetId`, `ExternalOrderId`.
- **Result**: Orders is now purely operational.

### 2. Sales Connectivity ✅

- **Corrected**: `SalesOrderService` now correctly calls the Operational Orders API.
- **Refactor**: Replaced `customerId` with `targetId` and removed pricing data from the logistics request.

### 3. Purchasing Integration ✅

- **Corrected**: `PurchaseOrderService` now creates an **Inbound Order** in `Orders` instead of adjusting inventory directly.
- **Improved**: The workflow follows the complete operational lifecycle: PO -> Inbound Order -> Inventory Transaction.
- **Result**: Complete domain separation and automated stock receipt tracking.

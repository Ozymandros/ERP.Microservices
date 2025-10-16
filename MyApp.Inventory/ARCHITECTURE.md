# Inventory Module Clean Architecture Scaffold

## Overview
The Inventory module has been successfully scaffolded following Clean Architecture principles with full separation of concerns across Domain, Application, Infrastructure, and API layers.

## Directory Structure

```
MyApp.Inventory/
├── MyApp.Inventory.Domain/
│   ├── Entities/
│   │   ├── Product.cs
│   │   ├── Warehouse.cs
│   │   └── InventoryTransaction.cs (with TransactionType enum)
│   └── Repositories/
│       ├── IProductRepository.cs
│       ├── IWarehouseRepository.cs
│       └── IInventoryTransactionRepository.cs
├── MyApp.Inventory.Infrastructure/
│   └── Data/
│       ├── InventoryDbContext.cs
│       ├── Configurations/
│       │   ├── ProductConfiguration.cs
│       │   ├── WarehouseConfiguration.cs
│       │   └── InventoryTransactionConfiguration.cs
│       └── Repositories/
│           ├── ProductRepository.cs
│           ├── WarehouseRepository.cs
│           └── InventoryTransactionRepository.cs
├── MyApp.Inventory.Application.Contracts/
│   ├── DTOs/
│   │   ├── ProductDtos.cs
│   │   ├── WarehouseDtos.cs
│   │   └── InventoryTransactionDtos.cs
│   └── Services/
│       ├── IProductService.cs
│       ├── IWarehouseService.cs
│       └── IInventoryTransactionService.cs
├── MyApp.Inventory.Application/
│   ├── Services/
│   │   ├── ProductService.cs
│   │   ├── WarehouseService.cs
│   │   └── InventoryTransactionService.cs
│   └── Mappings/
│       └── InventoryMappingProfile.cs
└── MyApp.Inventory.API/
    ├── Controllers/
    │   ├── ProductsController.cs
    │   ├── WarehousesController.cs
    │   └── InventoryTransactionsController.cs
    └── Program.cs (configured with DI)
```

## Layer Responsibilities

### Domain Layer (MyApp.Inventory.Domain)

**Entities:**
- `Product`: Id, SKU (unique), Name, Description, UnitPrice, QuantityInStock, ReorderLevel
- `Warehouse`: Id, Name, Location
- `InventoryTransaction`: Id, ProductId, WarehouseId, QuantityChange, TransactionType, TransactionDate
- `TransactionType` Enum: Inbound, Outbound, Adjustment

**Repository Interfaces:**
- `IProductRepository`: GetBySkuAsync(), GetLowStockProductsAsync(), + base CRUD
- `IWarehouseRepository`: GetByNameAsync(), + base CRUD
- `IInventoryTransactionRepository`: GetByProductIdAsync(), GetByWarehouseIdAsync(), GetByTransactionTypeAsync(), + base CRUD

**Database Constraints:**
- SKU is unique and indexed
- ProductId and WarehouseId are indexed
- TransactionDate is indexed
- Foreign key relationships configured with OnDelete(DeleteBehavior.Restrict)

### Infrastructure Layer (MyApp.Inventory.Infrastructure)

**DbContext:**
- `InventoryDbContext` with DbSet for Product, Warehouse, InventoryTransaction
- Connection string: `"Server=localhost;Database=InventoryDb;Trusted_Connection=True;"`

**EF Core Configurations:**
- Decimal precision set to (18,2) for prices
- Default values for QuantityInStock (0) and ReorderLevel (0)
- String length constraints applied

**Repositories:**
- `ProductRepository`: Implements IProductRepository with Linq queries for SKU lookup and low stock detection
- `WarehouseRepository`: Implements IWarehouseRepository
- `InventoryTransactionRepository`: Implements IInventoryTransactionRepository with filtering capabilities

### Application.Contracts Layer

**DTOs:**
- `ProductDto`, `CreateUpdateProductDto`
- `WarehouseDto`, `CreateUpdateWarehouseDto`
- `InventoryTransactionDto`, `CreateUpdateInventoryTransactionDto`

**Validation Attributes:**
- SKU/Name: Required, StringLength
- UnitPrice: Range (0 to MaxValue)
- QuantityInStock: Range (0 to MaxValue)
- QuantityChange: Range (-1000000 to 1000000)

**Service Interfaces:**
- All operations are async
- Comprehensive CRUD with specialized queries
- Exception handling with KeyNotFoundException and InvalidOperationException

### Application Layer (MyApp.Inventory.Application)

**Services with Business Logic:**

1. **ProductService**
   - ✅ Prevents duplicate SKUs
   - ✅ Validates product existence before updates
   - ✅ Provides GetLowStockProductsAsync() for reorder alerts

2. **WarehouseService**
   - ✅ Prevents duplicate warehouse names
   - ✅ Standard CRUD operations with validation

3. **InventoryTransactionService**
   - ✅ **Prevents negative stock levels** - throws exception if stock would go negative
   - ✅ **Auto-updates product stock** when transactions are created/updated/deleted
   - ✅ **Generates reorder alerts** via GetLowStockProductsAsync()
   - ✅ Transaction reversals on update/delete to maintain stock accuracy
   - ✅ Verifies product and warehouse existence

**AutoMapper Profile:**
- `InventoryMappingProfile`: Bidirectional mappings for all DTOs
- Nested object mapping (Product/Warehouse in InventoryTransactionDto)

### API Layer (MyApp.Inventory.API)

**Controllers (RESTful):**

1. **ProductsController** (`/api/inventory/products`)
   - GET / - List all products
   - GET /{id} - Get by ID
   - GET /sku/{sku} - Get by SKU
   - GET /low-stock - Get products below reorder level
   - POST - Create (returns 201 Created)
   - PUT /{id} - Update
   - DELETE /{id} - Delete

2. **WarehousesController** (`/api/inventory/warehouses`)
   - GET / - List all
   - GET /{id} - Get by ID
   - POST - Create
   - PUT /{id} - Update
   - DELETE /{id} - Delete

3. **InventoryTransactionsController** (`/api/inventory/transactions`)
   - GET / - List all
   - GET /{id} - Get by ID
   - GET /product/{productId} - Filter by product
   - GET /warehouse/{warehouseId} - Filter by warehouse
   - GET /type/{type} - Filter by transaction type
   - POST - Create (validates stock levels)
   - PUT /{id} - Update (reverses old & applies new)
   - DELETE /{id} - Delete (reverses transaction)

**Response Codes:**
- 200 OK
- 201 Created (for POST)
- 204 No Content (for DELETE)
- 400 Bad Request (validation errors)
- 404 Not Found
- 409 Conflict (business rule violations)

**Features:**
- Comprehensive logging via ILogger
- Model validation before processing
- Structured exception handling
- Swagger/OpenAPI documentation with ProducesResponseType attributes

## Dependency Injection Configuration

**Program.cs Setup:**
```csharp
// Database
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlServer(connectionString));

// Repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IWarehouseRepository, WarehouseRepository>();
builder.Services.AddScoped<IInventoryTransactionRepository, InventoryTransactionRepository>();

// Services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IInventoryTransactionService, InventoryTransactionService>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(InventoryMappingProfile));
```

## Key Features

### Business Logic Implementation

1. **Stock Level Management**
   - Prevents outbound transactions that would result in negative stock
   - Automatically updates product stock on transaction create/update/delete
   - Validates stock availability before allowing transactions

2. **Reorder Management**
   - GetLowStockProductsAsync() returns products where QuantityInStock < ReorderLevel
   - Accessible via API endpoint `/api/inventory/products/low-stock`

3. **Data Integrity**
   - Unique SKU constraint at database level
   - Foreign key relationships with restrict delete behavior
   - Transaction reversals maintain data consistency

4. **Async/Await Throughout**
   - All operations are fully async
   - Repository pattern for data access
   - Clean separation of concerns

## Conventions Applied

- ✅ Async/await everywhere
- ✅ RESTful route conventions
- ✅ Validation attributes on all DTOs
- ✅ Thin controllers (business logic in Application layer)
- ✅ Dependency injection throughout
- ✅ Comprehensive error handling
- ✅ Structured logging
- ✅ XML documentation ready for Swagger
- ✅ Clean Architecture separation of concerns

## Build Status

✅ **Build Successful**
- All projects compile without errors
- 0 Warnings (after version alignment)
- Ready for database migration and testing

## Next Steps

1. Create initial migration: `dotnet ef migrations add Initial -p MyApp.Inventory.Infrastructure`
2. Update database: `dotnet ef database update`
3. Run API: `dotnet run --project MyApp.Inventory.API`
4. Test endpoints via Swagger UI: `https://localhost:5001/swagger`
5. Add integration tests for business logic validation
6. Implement additional domain-specific queries as needed

## Technical Stack

- .NET 8.0
- Entity Framework Core 8.0.0
- AutoMapper 12.0.1
- SQL Server
- ASP.NET Core Web API
- Swagger/OpenAPI for documentation

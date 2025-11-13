# QuerySpec Service Method Implementation Guide

## Overview
All controllers now have `/search` endpoints that accept `QuerySpec` parameters. To complete the implementation, each service needs a `QueryAsync` method that accepts specifications.

## Required Service Methods

###  Auth Service (MyApp.Auth.Application.Contracts.Services)

#### IUserService
```csharp
Task<PaginatedResult<UserDto>> QueryUsersAsync(ISpecification<ApplicationUser> spec);
```

#### IRoleService  
```csharp
Task<PaginatedResult<RoleDto>> QueryRolesAsync(ISpecification<ApplicationRole> spec);
```

#### IPermissionService
```csharp
Task<PaginatedResult<PermissionDto>> QueryPermissionsAsync(ISpecification<Permission> spec);
```

---

### Inventory Service (MyApp.Inventory.Application.Contracts.Services)

#### IProductService
```csharp
Task<PaginatedResult<ProductDto>> QueryProductsAsync(ISpecification<Product> spec);
```

#### IWarehouseService
```csharp
Task<PaginatedResult<WarehouseDto>> QueryWarehousesAsync(ISpecification<Warehouse> spec);
```

#### IInventoryTransactionService
```csharp
Task<PaginatedResult<InventoryTransactionDto>> QueryTransactionsAsync(ISpecification<InventoryTransaction> spec);
```

---

### Purchasing Service (MyApp.Purchasing.Application.Contracts.Services)

#### ISupplierService
```csharp
Task<PaginatedResult<SupplierDto>> QuerySuppliersAsync(ISpecification<Supplier> spec);
```

#### IPurchaseOrderService
```csharp
Task<PaginatedResult<PurchaseOrderDto>> QueryPurchaseOrdersAsync(ISpecification<PurchaseOrder> spec);
```

---

### Sales Service (MyApp.Sales.Application.Contracts.Services)

#### ICustomerService
```csharp
Task<PaginatedResult<CustomerDto>> QueryCustomersAsync(ISpecification<Customer> spec);
```

#### ISalesOrderService
```csharp
Task<PaginatedResult<SalesOrderDto>> QuerySalesOrdersAsync(ISpecification<SalesOrder> spec);
```

---

## Implementation Pattern

For each service, the implementation should:

1. **Accept the specification** - ISpecification<TEntity> parameter
2. **Get the repository** - From DI or stored field
3. **Call QueryAsync** - Pass specification to repository
4. **Return paginated result** - Convert to DTO and return

### Example Implementation

```csharp
// In UserService
public async Task<PaginatedResult<UserDto>> QueryUsersAsync(ISpecification<ApplicationUser> spec)
{
    try
    {
        var result = await _userRepository.QueryAsync(spec);
        
        // Convert entities to DTOs
        var dtos = result.Items.Select(u => new UserDto
        {
            Id = u.Id,
            UserName = u.UserName,
            Email = u.Email,
            FirstName = u.FirstName,
            LastName = u.LastName,
            IsActive = u.IsActive,
            IsExternalLogin = u.IsExternalLogin,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        }).ToList();
        
        return new PaginatedResult<UserDto>(
            items: dtos,
            pageNumber: spec.Query.Page,
            pageSize: spec.Query.PageSize,
            totalCount: result.TotalCount
        );
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error querying users");
        throw;
    }
}
```

---

## Priority Order

1. **Auth Service** (3 methods) - UsersController, RolesController, PermissionsController
2. **Inventory Service** (3 methods) - ProductsController, WarehousesController, InventoryTransactionsController  
3. **Purchasing Service** (2 methods) - SuppliersController, PurchaseOrdersController
4. **Sales Service** (2 methods) - CustomersController, SalesOrdersController

**Total: 10 service methods**

---

## Validation Checklist

After implementing each service method:

- [ ] Method signature matches interface contract
- [ ] Accepts ISpecification<TEntity> parameter
- [ ] Calls repository.QueryAsync(spec)
- [ ] Converts entities to DTOs  
- [ ] Returns PaginatedResult<TDto>
- [ ] Includes error logging
- [ ] Throws on error (don't swallow exceptions)
- [ ] No caching (caching handled separately)

---

## Build Verification

After implementing service methods:

```bash
# Test Auth service
dotnet build src/MyApp.Auth/MyApp.Auth.API/MyApp.Auth.API.csproj -c Release

# Test Inventory service
dotnet build src/MyApp.Inventory/MyApp.Inventory.API/MyApp.Inventory.API.csproj -c Release

# Test Purchasing service
dotnet build src/MyApp.Purchasing/MyApp.Purchasing.API/MyApp.Purchasing.API.csproj -c Release

# Test Sales service
dotnet build src/MyApp.Sales/MyApp.Sales.API/MyApp.Sales.API.csproj -c Release

# Build all
dotnet build ERP.Microservices.sln -c Release
```

---

## Next Steps

1. Implement the 10 service methods listed above
2. Run builds for each service
3. Test `/search` endpoints with various query parameters
4. Document API endpoint examples in OpenAPI/Swagger


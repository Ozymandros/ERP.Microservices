# Quick Copy-Paste Service Method Templates

Use these templates to quickly implement the 10 service methods. Simply replace:
- `[ServiceName]` - e.g., UserService, ProductService  
- `[Entity]` - e.g., ApplicationUser, Product
- `[DTO]` - e.g., UserDto, ProductDto
- `[Property mapping]` - Map entity properties to DTO properties

---

## Auth Service - Users

### Interface Method (IUserService)
```csharp
/// <summary>
/// Query users with filtering, sorting, and pagination
/// </summary>
Task<PaginatedResult<UserDto>> QueryUsersAsync(ISpecification<ApplicationUser> spec);
```

### Implementation (UserService)
```csharp
public async Task<PaginatedResult<UserDto>> QueryUsersAsync(ISpecification<ApplicationUser> spec)
{
    try
    {
        var result = await _userRepository.QueryAsync(spec);
        
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
        
        return new PaginatedResult<UserDto>(dtos, spec.Query.Page, spec.Query.PageSize, result.TotalCount);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error querying users");
        throw;
    }
}
```

---

## Auth Service - Roles

### Interface Method (IRoleService)
```csharp
/// <summary>
/// Query roles with filtering, sorting, and pagination
/// </summary>
Task<PaginatedResult<RoleDto>> QueryRolesAsync(ISpecification<ApplicationRole> spec);
```

### Implementation (RoleService)
```csharp
public async Task<PaginatedResult<RoleDto>> QueryRolesAsync(ISpecification<ApplicationRole> spec)
{
    try
    {
        var result = await _roleRepository.QueryAsync(spec);
        
        var dtos = result.Items.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList();
        
        return new PaginatedResult<RoleDto>(dtos, spec.Query.Page, spec.Query.PageSize, result.TotalCount);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error querying roles");
        throw;
    }
}
```

---

## Auth Service - Permissions

### Interface Method (IPermissionService)
```csharp
/// <summary>
/// Query permissions with filtering, sorting, and pagination
/// </summary>
Task<PaginatedResult<PermissionDto>> QueryPermissionsAsync(ISpecification<Permission> spec);
```

### Implementation (PermissionService)
```csharp
public async Task<PaginatedResult<PermissionDto>> QueryPermissionsAsync(ISpecification<Permission> spec)
{
    try
    {
        var result = await _permissionRepository.QueryAsync(spec);
        
        var dtos = result.Items.Select(p => new PermissionDto
        {
            Id = p.Id,
            Module = p.Module,
            Action = p.Action,
            Description = p.Description,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).ToList();
        
        return new PaginatedResult<PermissionDto>(dtos, spec.Query.Page, spec.Query.PageSize, result.TotalCount);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error querying permissions");
        throw;
    }
}
```

---

## Inventory Service - Products

### Interface Method (IProductService)
```csharp
/// <summary>
/// Query products with filtering, sorting, and pagination
/// </summary>
Task<PaginatedResult<ProductDto>> QueryProductsAsync(ISpecification<Product> spec);
```

### Implementation (ProductService)
```csharp
public async Task<PaginatedResult<ProductDto>> QueryProductsAsync(ISpecification<Product> spec)
{
    try
    {
        var result = await _productRepository.QueryAsync(spec);
        
        var dtos = result.Items.Select(p => new ProductDto
        {
            Id = p.Id,
            SKU = p.SKU,
            Name = p.Name,
            Description = p.Description,
            Category = p.Category,
            UnitPrice = p.UnitPrice,
            Stock = p.Stock,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).ToList();
        
        return new PaginatedResult<ProductDto>(dtos, spec.Query.Page, spec.Query.PageSize, result.TotalCount);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error querying products");
        throw;
    }
}
```

---

## Inventory Service - Warehouses

### Interface Method (IWarehouseService)
```csharp
/// <summary>
/// Query warehouses with filtering, sorting, and pagination
/// </summary>
Task<PaginatedResult<WarehouseDto>> QueryWarehousesAsync(ISpecification<Warehouse> spec);
```

### Implementation (WarehouseService)
```csharp
public async Task<PaginatedResult<WarehouseDto>> QueryWarehousesAsync(ISpecification<Warehouse> spec)
{
    try
    {
        var result = await _warehouseRepository.QueryAsync(spec);
        
        var dtos = result.Items.Select(w => new WarehouseDto
        {
            Id = w.Id,
            Name = w.Name,
            Location = w.Location,
            City = w.City,
            Country = w.Country,
            PostalCode = w.PostalCode,
            IsActive = w.IsActive,
            CreatedAt = w.CreatedAt,
            UpdatedAt = w.UpdatedAt
        }).ToList();
        
        return new PaginatedResult<WarehouseDto>(dtos, spec.Query.Page, spec.Query.PageSize, result.TotalCount);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error querying warehouses");
        throw;
    }
}
```

---

## Inventory Service - Inventory Transactions

### Interface Method (IInventoryTransactionService)
```csharp
/// <summary>
/// Query inventory transactions with filtering, sorting, and pagination
/// </summary>
Task<PaginatedResult<InventoryTransactionDto>> QueryTransactionsAsync(ISpecification<InventoryTransaction> spec);
```

### Implementation (InventoryTransactionService)
```csharp
public async Task<PaginatedResult<InventoryTransactionDto>> QueryTransactionsAsync(ISpecification<InventoryTransaction> spec)
{
    try
    {
        var result = await _transactionRepository.QueryAsync(spec);
        
        var dtos = result.Items.Select(t => new InventoryTransactionDto
        {
            Id = t.Id,
            ProductId = t.ProductId,
            WarehouseId = t.WarehouseId,
            TransactionType = t.TransactionType,
            Quantity = t.Quantity,
            ReferenceNumber = t.ReferenceNumber,
            Notes = t.Notes,
            TransactionDate = t.TransactionDate,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        }).ToList();
        
        return new PaginatedResult<InventoryTransactionDto>(dtos, spec.Query.Page, spec.Query.PageSize, result.TotalCount);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error querying transactions");
        throw;
    }
}
```

---

## Purchasing Service - Suppliers

### Interface Method (ISupplierService)
```csharp
/// <summary>
/// Query suppliers with filtering, sorting, and pagination
/// </summary>
Task<PaginatedResult<SupplierDto>> QuerySuppliersAsync(ISpecification<Supplier> spec);
```

### Implementation (SupplierService)
```csharp
public async Task<PaginatedResult<SupplierDto>> QuerySuppliersAsync(ISpecification<Supplier> spec)
{
    try
    {
        var result = await _supplierRepository.QueryAsync(spec);
        
        var dtos = result.Items.Select(s => new SupplierDto
        {
            Id = s.Id,
            Name = s.Name,
            Email = s.Email,
            PhoneNumber = s.PhoneNumber,
            ContactPerson = s.ContactPerson,
            Address = s.Address,
            City = s.City,
            Country = s.Country,
            PostalCode = s.PostalCode,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        }).ToList();
        
        return new PaginatedResult<SupplierDto>(dtos, spec.Query.Page, spec.Query.PageSize, result.TotalCount);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error querying suppliers");
        throw;
    }
}
```

---

## Purchasing Service - Purchase Orders

### Interface Method (IPurchaseOrderService)
```csharp
/// <summary>
/// Query purchase orders with filtering, sorting, and pagination
/// </summary>
Task<PaginatedResult<PurchaseOrderDto>> QueryPurchaseOrdersAsync(ISpecification<PurchaseOrder> spec);
```

### Implementation (PurchaseOrderService)
```csharp
public async Task<PaginatedResult<PurchaseOrderDto>> QueryPurchaseOrdersAsync(ISpecification<PurchaseOrder> spec)
{
    try
    {
        var result = await _purchaseOrderRepository.QueryAsync(spec);
        
        var dtos = result.Items.Select(po => new PurchaseOrderDto
        {
            Id = po.Id,
            OrderNumber = po.OrderNumber,
            SupplierId = po.SupplierId,
            Status = po.Status,
            OrderDate = po.OrderDate,
            ExpectedDeliveryDate = po.ExpectedDeliveryDate,
            TotalAmount = po.TotalAmount,
            Notes = po.Notes,
            CreatedAt = po.CreatedAt,
            UpdatedAt = po.UpdatedAt
        }).ToList();
        
        return new PaginatedResult<PurchaseOrderDto>(dtos, spec.Query.Page, spec.Query.PageSize, result.TotalCount);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error querying purchase orders");
        throw;
    }
}
```

---

## Sales Service - Customers

### Interface Method (ICustomerService)
```csharp
/// <summary>
/// Query customers with filtering, sorting, and pagination
/// </summary>
Task<PaginatedResult<CustomerDto>> QueryCustomersAsync(ISpecification<Customer> spec);
```

### Implementation (CustomerService)
```csharp
public async Task<PaginatedResult<CustomerDto>> QueryCustomersAsync(ISpecification<Customer> spec)
{
    try
    {
        var result = await _customerRepository.QueryAsync(spec);
        
        var dtos = result.Items.Select(c => new CustomerDto
        {
            Id = c.Id,
            Name = c.Name,
            Email = c.Email,
            PhoneNumber = c.PhoneNumber,
            ContactPerson = c.ContactPerson,
            Address = c.Address,
            City = c.City,
            Country = c.Country,
            PostalCode = c.PostalCode,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        }).ToList();
        
        return new PaginatedResult<CustomerDto>(dtos, spec.Query.Page, spec.Query.PageSize, result.TotalCount);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error querying customers");
        throw;
    }
}
```

---

## Sales Service - Sales Orders

### Interface Method (ISalesOrderService)
```csharp
/// <summary>
/// Query sales orders with filtering, sorting, and pagination
/// </summary>
Task<PaginatedResult<SalesOrderDto>> QuerySalesOrdersAsync(ISpecification<SalesOrder> spec);
```

### Implementation (SalesOrderService)
```csharp
public async Task<PaginatedResult<SalesOrderDto>> QuerySalesOrdersAsync(ISpecification<SalesOrder> spec)
{
    try
    {
        var result = await _salesOrderRepository.QueryAsync(spec);
        
        var dtos = result.Items.Select(so => new SalesOrderDto
        {
            Id = so.Id,
            OrderNumber = so.OrderNumber,
            CustomerId = so.CustomerId,
            Status = so.Status,
            OrderDate = so.OrderDate,
            DeliveryDate = so.DeliveryDate,
            TotalAmount = so.TotalAmount,
            Notes = so.Notes,
            CreatedAt = so.CreatedAt,
            UpdatedAt = so.UpdatedAt
        }).ToList();
        
        return new PaginatedResult<SalesOrderDto>(dtos, spec.Query.Page, spec.Query.PageSize, result.TotalCount);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error querying sales orders");
        throw;
    }
}
```

---

## Implementation Steps

1. **For each service:**
   - Open the interface file in `[Service].Application.Contracts/Services/I[Service]Service.cs`
   - Add the method signature from the "Interface Method" section above
   - Open the implementation file `[Service].Application/Services/[Service]Service.cs`  
   - Add the method implementation from the "Implementation" section above
   - Adjust property mappings to match your actual entity and DTO properties

2. **After implementing all 10 methods:**
   ```bash
   dotnet build ERP.Microservices.sln -c Release
   ```

3. **Verify each service builds:**
   ```bash
   dotnet build src/MyApp.Auth/MyApp.Auth.API/MyApp.Auth.API.csproj -c Release
   dotnet build src/MyApp.Inventory/MyApp.Inventory.API/MyApp.Inventory.API.csproj -c Release
   dotnet build src/MyApp.Purchasing/MyApp.Purchasing.API/MyApp.Purchasing.API.csproj -c Release
   dotnet build src/MyApp.Sales/MyApp.Sales.API/MyApp.Sales.API.csproj -c Release
   ```

---

## Important Notes

- Adjust property names in the DTOs mapping to match your actual entity and DTO properties
- Each service may have different property names (e.g., SKU vs Sku)
- Ensure repository fields are properly initialized in service constructor
- Add `using` for specification namespaces if not already present
- Maintain consistent error logging across all implementations
- All methods follow the same pattern: repository.QueryAsync() → map to DTO → return result

---

## Quick Checklist

For each of the 10 methods:

- [ ] Interface method added to IService
- [ ] Implementation added to Service class
- [ ] Entity type correct (e.g., ApplicationUser, Product)
- [ ] DTO properties properly mapped
- [ ] Repository field exists in service
- [ ] Error handling included
- [ ] Method uses QueryAsync
- [ ] Returns PaginatedResult<TDto>
- [ ] Compiles without errors
- [ ] All 10 methods complete before running full build

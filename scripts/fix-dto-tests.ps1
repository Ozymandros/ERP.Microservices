# DTO Test Fix Script
# This script provides the correct patterns for fixing test DTO instantiations

Write-Host "DTO Test Fix Patterns" -ForegroundColor Green
Write-Host "=====================" -ForegroundColor Green
Write-Host ""

Write-Host "1. All DTOs now use object initializer syntax (not positional constructors)" -ForegroundColor Yellow
Write-Host ""

Write-Host "CORRECT PATTERNS:" -ForegroundColor Cyan
Write-Host ""

Write-Host "ProductDto:" -ForegroundColor White
Write-Host 'new ProductDto(Guid.NewGuid()) { SKU = "PRD-001", Name = "Test Product" }'
Write-Host ""

Write-Host "WarehouseDto:" -ForegroundColor White
Write-Host 'new WarehouseDto(Guid.NewGuid()) { Name = "Main Warehouse", Location = "NY" }'
Write-Host ""

Write-Host "CustomerDto:" -ForegroundColor White
Write-Host 'new CustomerDto(Guid.NewGuid()) { Name = "Test Customer", Email = "test@example.com" }'
Write-Host ""

Write-Host "SupplierDto:" -ForegroundColor White
Write-Host 'new SupplierDto(Guid.NewGuid()) { Name = "Test Supplier", ContactName = "Contact", Email = "test@supplier.com" }'
Write-Host ""

Write-Host "SalesOrderDto:" -ForegroundColor White
Write-Host 'new SalesOrderDto(Guid.NewGuid()) { OrderDate = DateTime.UtcNow, OrderNumber = "SO-001", CustomerId = customerId, Status = 0, TotalAmount = 100.00m }'
Write-Host ""

Write-Host "OrderDto:" -ForegroundColor White  
Write-Host 'new OrderDto(Guid.NewGuid()) { OrderDate = DateTime.UtcNow, OrderNumber = "ORD-001", CustomerId = customerId, Status = "Draft", TotalAmount = 50.00m, Lines = new List<OrderLineDto>() }'
Write-Host ""

Write-Host "OrderLineDto:" -ForegroundColor White
Write-Host 'new OrderLineDto(Guid.NewGuid()) { ProductId = productId, Quantity = 5, UnitPrice = 10.00m, LineTotal = 50.00m }'
Write-Host ""

Write-Host "LoginDto (positional - this one is CORRECT as-is):" -ForegroundColor White
Write-Host 'new LoginDto("email@example.com", "password")'
Write-Host ""

Write-Host "INCORRECT (DO NOT USE):" -ForegroundColor Red
Write-Host 'new ProductDto(id, createdAt, createdBy, ..., sku, name) // OLD POSITIONAL STYLE'
Write-Host ""

Write-Host "Run dotnet build to see remaining errors after manual fixes" -ForegroundColor Green

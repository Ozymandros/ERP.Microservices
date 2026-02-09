# Antigravity Agent Rules - ERP Microservices Backend

## Project Context

This is a **Domain-Driven Design (DDD) microservices-based ERP system** built with .NET 10, following Clean Architecture and CQRS principles. Each microservice represents a distinct bounded context with clear domain boundaries.

---

## 🚨 Critical Architectural Constraints

### Bounded Context Separation (NEVER VIOLATE)

**Rule**: Operational and Commercial concerns MUST remain strictly separated.

| Domain | Type | Has CustomerId? | Has SupplierId? | Has Pricing? | Purpose |
|--------|------|-----------------|-----------------|--------------|---------|
| `MyApp.Orders` | **OPERATIONAL** | ❌ NEVER | ❌ NEVER | ❌ NEVER | Physical logistics movements |
| `MyApp.Sales` | **COMMERCIAL** | ✅ ALWAYS | ❌ NO | ✅ YES | Customer sales transactions |
| `MyApp.Purchasing` | **SUPPLY CHAIN** | ❌ NO | ✅ ALWAYS | ✅ YES | Supplier procurement |
| `MyApp.Inventory` | **CORE** | ❌ NO | ❌ NO | ✅ YES (UnitPrice) | Stock management |
| `MyApp.Auth` | **INFRASTRUCTURE** | N/A | N/A | ❌ NO | Authentication/Authorization |
| `MyApp.Billing` | **COMMERCIAL** | ✅ YES | ✅ YES | ✅ YES | Invoicing and payments |

### Orders Domain - Operational Definition

**The `Orders` microservice tracks PHYSICAL MOVEMENTS, not commercial transactions.**

**✅ ALWAYS include**:
```csharp
public class Order(Guid id) : AuditableEntity<Guid>(id)
{
    public OrderType Type { get; set; }        // Transfer|Inbound|Outbound|Return
    public Guid? SourceId { get; set; }         // Origin (warehouse/supplier)
    public Guid? TargetId { get; set; }         // Destination (warehouse/customer)
    public Guid? ExternalOrderId { get; set; }  // Link to SalesOrder/PurchaseOrder
    public OrderStatus Status { get; set; }     // Draft|Approved|InTransit|Received|Completed|Cancelled
    public int PickedQuantity { get; set; }     // Operational tracking
}

```

---

## 🛠️ Skills List

- **.NET 10**: Proficient in developing applications using the latest .NET framework.
- **C# 13**: Experienced in writing clean, efficient, and maintainable C# code.
- **Microservices Architecture**: Knowledgeable in designing and implementing microservices.
- **Dapr**: Familiar with using Dapr for service-to-service communication and state management.
- **Azure Container Apps**: Skilled in deploying applications to Azure Container Apps.
- **Docker**: Proficient in containerizing applications using Docker.
- **Entity Framework Core**: Experienced in using EF Core for data access and management.
- **API Development**: Knowledgeable in building RESTful APIs and using API gateways.
- **Unit Testing**: Skilled in writing unit tests and integration tests for .NET applications.
- **CI/CD**: Familiar with setting up CI/CD pipelines using GitHub Actions.
- **Clean Architecture**: Knowledgeable in implementing Clean Architecture principles.
- **Logging and Monitoring**: Experienced in implementing logging and monitoring solutions.

**❌ NEVER include**:
- `CustomerId` → Use `MyApp.Sales.SalesOrder`
- `SupplierId` → Use `MyApp.Purchasing.PurchaseOrder`
- `TotalAmount`, `UnitPrice`, `LineTotal` → Use Sales/Purchasing domains
- `QuoteExpiryDate`, `Discount` → Sales-specific, not operational

**Rationale**: 
- Orders answers: "Where is this shipment? Is it picked/packed/in-transit?"
- Sales answers: "Who bought this? How much revenue?"
- Purchasing answers: "Who supplied this? What's the cost?"

---

## Code Generation Standards

### 1. Entity Design

**Pattern**: Primary constructors, inherit from `AuditableEntity<TId>`

```csharp
// ✅ CORRECT
public class Order(Guid id) : AuditableEntity<Guid>(id)
{
    public string OrderNumber { get; set; } = string.Empty;
    public OrderType Type { get; set; }
    public List<OrderLine> Lines { get; set; } = new();
}

// ❌ WRONG
public class Order
{
    public Guid Id { get; set; }  // Missing AuditableEntity base
    public Order() { }             // No primary constructor
}
```

**Mandatory base class properties** (inherited from `AuditableEntity<TId>`):
- `Id`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`

### 2. DTO Patterns

**Read DTOs** - inherit from `AuditableGuidDto`:
```csharp
public record OrderDto(Guid Id) : AuditableGuidDto(Id)
{
    public string OrderNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;  // Enum → string
    public string Type { get; init; } = string.Empty;    // Enum → string
    public List<OrderLineDto> Lines { get; init; } = new();
}
```

**Create/Update DTOs** - use `record` with validation:
```csharp
public record CreateUpdateOrderDto
{
    [Required]
    public string OrderNumber { get; init; } = string.Empty;
    
    [Required]
    public OrderType Type { get; init; }  // Enum type (AutoMapper converts)
    
    public Guid? SourceId { get; init; }
    public Guid? TargetId { get; init; }
}
```

**Rules**:
- Use `record` types for immutability
- Use `init` accessors
- Enums in DTOs: use enum type for Create/Update, string for Read
- Apply `[Required]`, `[Range]`, `[MaxLength]` validation attributes

### 3. Service Layer

**Constructor injection pattern**:
```csharp
public class OrderService : IOrderService
{
    private readonly IOrderRepository _orders;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderService> _logger;
    private readonly IEventPublisher _eventPublisher;
    
    public OrderService(
        IOrderRepository orders,
        IMapper mapper,
        ILogger<OrderService> logger,
        IEventPublisher eventPublisher)
    {
        _orders = orders;
        _mapper = mapper;
        _logger = logger;
        _eventPublisher = eventPublisher;
    }
}
```

**Logging pattern**:
```csharp
_logger.LogInformation(
    "Creating operational order: OrderNumber={OrderNumber}, Type={Type}",
    dto.OrderNumber, dto.Type);
```

**Event publishing**:
```csharp
await _eventPublisher.PublishAsync("orders.order.created", 
    new OrderCreatedEvent(order.Id, order.OrderNumber, order.Type.ToString()));
```

### 4. Controller Patterns

**Standard structure** (export endpoints FIRST):
```csharp
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class OrdersController : ControllerBase
{
    // 1. Export endpoints FIRST
    [HttpGet("export-xlsx")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> ExportToXlsx() { ... }
    
    [HttpGet("export-pdf")]
    [Produces("application/pdf")]
    public async Task<IActionResult> ExportToPdf() { ... }
    
    // 2. Then CRUD operations
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetAll() { ... }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetById(Guid id) { ... }
    
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateUpdateOrderDto dto) { ... }
}
```

**Required using statements**:
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.{Domain}.Application.Contracts;
using MyApp.{Domain}.Application.Contracts.Dtos;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Infrastructure.Export;  // For Excel/PDF
```

### 5. Repository Pattern

**Interface** (in Domain layer):
```csharp
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<IEnumerable<Order>> ListAsync();
    Task AddAsync(Order entity);
    Task UpdateAsync(Order entity);
    Task DeleteAsync(Guid id);
}
```

**Implementation** (in Infrastructure layer):
```csharp
public class OrderRepository : IOrderRepository
{
    private readonly OrdersDbContext _context;
    
    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _context.Orders
            .Include(o => o.Lines)  // Always include navigation properties
            .FirstOrDefaultAsync(o => o.Id == id);
    }
    
    public async Task AddAsync(Order entity)
    {
        await _context.Orders.AddAsync(entity);
        await _context.SaveChangesAsync();  // Always save
    }
}
```

### 6. AutoMapper Configuration

**Mapping Profile**:
```csharp
public class OrderProfile : Profile
{
    public OrderProfile()
    {
        // Entity → DTO (enum → string)
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.Type, o => o.MapFrom(s => s.Type.ToString()));
        
        // DTO → Entity
        CreateMap<CreateUpdateOrderDto, Order>()
            .ConstructUsing(src => new Order(Guid.NewGuid()));
    }
}
```

### 7. EF Core Configuration

**Entity Configuration**:
```csharp
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderNumber).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Type).IsRequired();  // Enum stored as int
        
        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(l => l.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

---

## Workflow Guidelines

### When Adding a New Feature

**Always follow this order**:
1. **Domain Layer**: Create/modify entity in `{Service}.Domain/Entities/`
2. **Application Contracts**: Create DTOs in `{Service}.Application.Contracts/Dtos/`
3. **Repository**: Add interface in `{Service}.Domain/Repositories/`, implement in `{Service}.Infrastructure/Data/Repositories/`
4. **Service**: Implement in `{Service}.Application/Services/`
5. **AutoMapper**: Add mappings in `{Service}.Application/Mapping/`
6. **Controller**: Add endpoints in `{Service}.API/Controllers/`
7. **EF Configuration**: Add in `{Service}.Infrastructure/Data/Configurations/`
8. **Migration**: Run `dotnet ef migrations add` from Infrastructure project
9. **Tests**: Add tests in `test/{Service}.Application.Tests/` and `test/{Service}.Infrastructure.Tests/`

### When Refactoring Domains

**Before making changes**, verify:
1. ✅ Does this belong in this bounded context?
2. ✅ Am I mixing operational and commercial concerns?
3. ✅ Are CustomerId/SupplierId/Pricing fields in the right domain?
4. ✅ Will this break the domain separation?

**If removing fields from entities**:
1. Update entity class
2. Update all DTOs
3. Update AutoMapper profiles
4. Update EF Core configurations
5. Update service layer (remove calculations/references)
6. Create and review migration
7. Update all tests
8. Update API documentation

---

## Testing Requirements

### Unit Tests (xUnit + Moq)

**Naming**: `MethodName_Scenario_ExpectedResult`

```csharp
public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _mockOrders;
    private readonly Mock<IMapper> _mockMapper;
    private readonly OrderService _service;
    
    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesOperationalOrder()
    {
        // Arrange
        var dto = new CreateUpdateOrderDto 
        { 
            Type = OrderType.Transfer,
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid()
        };
        
        // Act
        await _service.CreateAsync(dto);
        
        // Assert
        _mockOrders.Verify(r => r.AddAsync(It.Is<Order>(o => 
            o.Type == OrderType.Transfer)), Times.Once);
    }
}
```

---

## Common Mistakes to Avoid

### ❌ Domain Contamination
```csharp
// ❌ WRONG - Adding commercial fields to operational domain
public class Order(Guid id) : AuditableEntity<Guid>(id)
{
    public Guid CustomerId { get; set; }     // Belongs in Sales!
    public decimal TotalAmount { get; set; }  // Belongs in Sales!
}
```

### ❌ Missing AutoMapper Configuration
```csharp
// ❌ WRONG - Enum not converted to string for DTO
CreateMap<Order, OrderDto>();  // Status/Type will fail
```

### ❌ Forgetting SaveChangesAsync
```csharp
// ❌ WRONG
public async Task AddAsync(Order entity)
{
    await _context.Orders.AddAsync(entity);
    // Missing: await _context.SaveChangesAsync();
}
```

### ❌ Not Including Navigation Properties
```csharp
// ❌ WRONG
return await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);

// ✅ CORRECT
return await _context.Orders
    .Include(o => o.Lines)
    .FirstOrDefaultAsync(o => o.Id == id);
```

### ❌ Hardcoded Values
```csharp
// ❌ WRONG
var apiKey = "sk_live_12345";

// ✅ CORRECT
var apiKey = _configuration["ExternalApi:ApiKey"]
    ?? throw new InvalidOperationException("API key required");
```

---

## Decision Tree: Where Does This Feature Belong?

```
Is it about tracking PHYSICAL MOVEMENT of goods?
├─ YES → MyApp.Orders (Operational)
└─ NO
    ├─ Is it about SELLING to customers?
    │   └─ YES → MyApp.Sales (Commercial)
    └─ NO
        ├─ Is it about BUYING from suppliers?
        │   └─ YES → MyApp.Purchasing (Supply Chain)
        └─ NO
            ├─ Is it about STOCK LEVELS?
            │   └─ YES → MyApp.Inventory (Core)
            └─ NO
                ├─ Is it about INVOICING/PAYMENTS?
                │   └─ YES → MyApp.Billing (Commercial)
                └─ Is it about USERS/PERMISSIONS?
                    └─ YES → MyApp.Auth (Infrastructure)
```

---

## Export Functionality (Required for All List Endpoints)

**Every controller with list operations MUST have Excel and PDF export**:

```csharp
[HttpGet("export-xlsx")]
[Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
[ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
public async Task<IActionResult> ExportToXlsx()
{
    try
    {
        var data = await _cacheService.GetStateAsync<IEnumerable<OrderDto>>("all_orders")
            ?? await _orderService.ListAsync();
        var bytes = data.ExportToXlsx();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Orders.xlsx");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error exporting orders to XLSX");
        return StatusCode(500, new { message = "An error occurred exporting orders" });
    }
}
```

**Required using**: `using MyApp.Shared.Infrastructure.Export;`

---

## Before Submitting Code

**Checklist**:
- ✅ Bounded context separation respected (no CustomerId in Orders, etc.)
- ✅ Clean Architecture layers followed (Domain → Application → Infrastructure → API)
- ✅ Entities inherit from `AuditableEntity<TId>` with primary constructors
- ✅ DTOs are `record` types with `init` accessors
- ✅ AutoMapper configured for enum → string conversions
- ✅ Export endpoints added (Excel/PDF) for list operations
- ✅ Navigation properties included in repository queries
- ✅ `SaveChangesAsync()` called after mutations
- ✅ Domain events published for significant actions
- ✅ Validation attributes applied to DTOs
- ✅ Tests updated (unit + integration)
- ✅ Migration created and reviewed
- ✅ No hardcoded secrets or connection strings
- ✅ Structured logging used (no string interpolation)

---

## Questions to Ask Before Coding

1. **Which bounded context does this belong to?**
   - Operational (Orders) vs Commercial (Sales/Purchasing) vs Core (Inventory)

2. **Does this entity need CustomerId?**
   - If YES → probably belongs in Sales, not Orders

3. **Does this entity need pricing (TotalAmount, UnitPrice)?**
   - If YES → belongs in Sales/Purchasing/Billing, not Orders

4. **What's the difference between Order, SalesOrder, and PurchaseOrder?**
   - Order = Logistics tracking (no commercial data)
   - SalesOrder = Customer sale (has CustomerId, pricing)
   - PurchaseOrder = Supplier purchase (has SupplierId, costs)

5. **Should this trigger a domain event?**
   - Created, Updated, Status Changed, Fulfilled, Cancelled → YES

---

**Last Updated**: 2026-01-23 (based on Orders → Operational refactoring)

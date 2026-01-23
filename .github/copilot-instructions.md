# ERP Microservices - Copilot Agent Instructions

**Mission:** Make AI agents instantly productive in this .NET 10 ERP microservices repository. Follow these patterns to write code that aligns with the existing architecture. Deviate only when you have a strong, project-specific reason.

---

## 🎯 Executive Summary

### System Architecture
- **Microservices:** 6 independent services (Auth, Billing, Inventory, Orders, Purchasing, Sales)
- **API Gateway:** Ocelot/YARP routing to services at `/[service]/api/...`
- **Service Mesh:** DAPR for service-to-service calls, pub/sub, and state management
- **Databases:** 6 SQL Server databases (1 per service) + Redis cache
- **Orchestration:** .NET Aspire (local dev), Azure Container Apps (production)
- **Shared Libraries:** `MyApp.Shared.*` projects for cross-cutting concerns
- **.NET Version:** .NET 10 with C# 13

### Data Flow Pattern
```
Client → API Gateway (Ocelot) → Microservice → DAPR Sidecar → Database/Redis/Other Services
```

### Quick Reference
| Need | Location |
|------|----------|
| **Add endpoint** | `[Service].API/Controllers/[Entity]Controller.cs` |
| **Business logic** | `[Service].Application/Services/[Entity]Service.cs` |
| **Data access** | `[Service].Infrastructure/Data/Repositories/[Entity]Repository.cs` |
| **Domain model** | `[Service].Domain/Entities/[Entity].cs` |
| **Gateway routing** | `src/ErpApiGateway/ocelot.json` |
| **Infrastructure** | `infra/main.bicep` and modules in `infra/core/` |
| **Tests** | `src/AppHost.Tests/Tests/` |

---

## 🏗️ Architecture Patterns

### Clean Architecture (Per Service)

Every microservice follows strict 4-layer Clean Architecture:

```
MyApp.[Service]/
├── MyApp.[Service].API/
│   ├── Controllers/          # HTTP endpoints, DTOs, OpenAPI
│   ├── Program.cs            # DI, middleware, authentication
│   └── appsettings.*.json    # Configuration
│
├── MyApp.[Service].Application/
│   ├── Services/            # Business logic, orchestration
│   ├── Contracts/           # Service interfaces, DTOs
│   └── Mappings/            # AutoMapper profiles
│
├── MyApp.[Service].Domain/
│   ├── Entities/            # Domain models (pure C# classes)
│   └── Repositories/        # Repository interfaces
│
└── MyApp.[Service].Infrastructure/
    ├── Data/
    │   ├── [Service]DbContext.cs      # EF Core context
    │   ├── Repositories/               # Repository implementations
    │   └── Seeders/                    # Data seeders
    └── Services/                       # External API clients, infra services
```

**Key Rule:** Dependencies flow inward. Domain has ZERO dependencies. API depends on Application. Application depends  on Domain. Infrastructure implements interfaces from Application/Domain.

### Shared Projects

```
src/MyApp.Shared/
├── MyApp.Shared.Domain/         # Domain primitives, interfaces
├── MyApp.Shared.Infrastructure/ # Logging, caching, messaging
├── MyApp.Shared.CQRS/          # CQRS base classes
└── MyApp.Shared.SignalR/       # Real-time communication
```

**Rule:** Shared code MUST be genuinely cross-cutting. Service-specific logic stays in that service.

---

## 🔧 Development Workflows

### Running Locally

**All services with Aspire dashboard:**
```powershell
cd src/AppHost
dotnet run
```
Access at: `http://localhost:15000` (Aspire Dashboard)

**Single service (for debugging):**
```powershell
cd src/MyApp.Auth/MyApp.Auth.API
dotnet run
```

**With Docker Compose (alternative):**
```powershell
docker-compose up
```

### Building and Testing

```powershell
# Build everything
dotnet build

# Run all tests
dotnet test

# Run specific service tests
dotnet test src/AppHost.Tests/Tests/AuthServiceTests.cs

# Build with release configuration
dotnet build -c Release
```

### Database Migrations

**IMPORTANT:** Always run migrations from the Infrastructure project directory.

```powershell
# Example: Add migration to Auth service
cd src/MyApp.Auth/MyApp.Auth.Infrastructure
dotnet ef migrations add AddUserEmailVerification

# Apply migrations
dotnet ef database update

# Generate SQL script (for production)
dotnet ef migrations script --output migrations.sql
```

### Deployment

```powershell
# Deploy to Azure (one command)
azd up

# Check deployment status
azd monitor

# Clean up resources
azd down
```

---

## 📝 Coding Patterns & Examples

### 1. Adding a New Endpoint

**Full workflow example: Add `GET /api/customers/{id}` to Inventory service**

#### Step 1: Define DTO (API layer)
```csharp
// src/MyApp.Inventory/MyApp.Inventory.Application.Contracts/DTOs/CustomerDto.cs
namespace MyApp.Inventory.Application.Contracts.DTOs;

public record CustomerDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
```

#### Step 2: Add Repository Method
```csharp
// MyApp.Inventory.Domain/Repositories/ICustomerRepository.cs
namespace MyApp.Inventory.Domain.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}

// MyApp.Inventory.Infrastructure/Data/Repositories/CustomerRepository.cs
namespace MyApp.Inventory.Infrastructure.Data.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly InventoryDbContext _context;

    public CustomerRepository(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
}
```

#### Step 3: Add Service Method
```csharp
// MyApp.Inventory.Application/Services/CustomerService.cs
namespace MyApp.Inventory.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(
        ICustomerRepository repository,
        IMapper mapper,
        ICacheService cache,
        ILogger<CustomerService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _cache = cache;
        _logger = logger;
    }

    public async Task<CustomerDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        // Try cache first
        var cacheKey = $"customer:{id}";
        var cached = await _cache.GetAsync<CustomerDto>(cacheKey);
        if (cached != null) return cached;

        var customer = await _repository.GetByIdAsync(id, cancellationToken);
        if (customer == null)
        {
            _logger.LogWarning("Customer {CustomerId} not found", id);
            return null;
        }

        var dto = _mapper.Map<CustomerDto>(customer);
        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5));
        return dto;
    }
}
```

#### Step 4: Add Controller Endpoint
```csharp
// MyApp.Inventory.API/Controllers/CustomersController.cs
namespace MyApp.Inventory.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        ICustomerService customerService,
        ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    /// <summary>
    /// Get customer by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CustomerDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetByIdAsync(id, cancellationToken);
        if (customer == null)
        {
            return NotFound(new { message = $"Customer {id} not found" });
        }

        return Ok(customer);
    }
}
```

#### Step 5: Register in DI (Program.cs)
```csharp
// MyApp.Inventory.API/Program.cs
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
```

#### Step 6: Update Gateway Route (if needed)
```json
// src/ErpApiGateway/ocelot.json
{
  "DownstreamPathTemplate": "/api/customers/{everything}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [
    {
      "Host": "inventory-service",
      "Port": 8080
    }
  ],
  "UpstreamPathTemplate": "/inventory/api/customers/{everything}",
  "UpstreamHttpMethod": ["GET", "POST", "PUT", "DELETE"],
  "AuthenticationOptions": { "AuthenticationProviderKey": "Bearer" },
  "Priority": 1
}
```

### 2. Service-to-Service Communication (DAPR)

**Calling another service via HTTP:**
```csharp
// In any service's Application layer
public class OrderService : IOrderService
{
    private readonly DaprClient _daprClient;

    public OrderService(DaprClient daprClient)
    {
        _daprClient = daprClient;
    }

    public async Task<ProductDto?> GetProductFromInventory(int productId)
    {
        // DAPR service invocation - "inventory-service" is the app-id
        var httpClient = _daprClient.CreateInvokeHttpClient("inventory-service");
        var response = await httpClient.GetAsync($"/api/inventory/products/{productId}");
        
        if (!response.IsSuccessStatusCode) return null;
        
        return await response.Content.ReadFromJsonAsync<ProductDto>();
    }
}
```

**Publishing events via DAPR Pub/Sub:**
```csharp
// Publishing an event
public class OrderService : IOrderService
{
    private readonly DaprClient _daprClient;

    public async Task CreateOrderAsync(CreateOrderDto dto)
    {
        // ... create order logic ...
        
        // Publish event for other services to consume
        await _daprClient.PublishEventAsync(
            "pubsub",                // Pub/sub component name
            "order-created",         // Topic name (kebab-case)
            new OrderCreatedEvent 
            { 
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                TotalAmount = order.TotalAmount,
                CreatedAt = DateTime.UtcNow
            }
        );
    }
}

// Subscribing to events (in Program.cs)
app.MapPost("/order-created", async (
    OrderCreatedEvent @event,
    ILogger<Program> logger,
    INotificationService notificationService) =>
{
    logger.LogInformation("Received order-created event: {OrderId}", @event.OrderId);
    await notificationService.SendOrderConfirmationAsync(@event.OrderId);
    return Results.Ok();
});
```

### 3. Caching Patterns

**Always use `ICacheService` for Redis caching:**
```csharp
public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly ICacheService _cache;

    // Cache-aside pattern
    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var cacheKey = $"product:{id}";
        
        // Try cache first
        var cached = await _cache.GetAsync<ProductDto>(cacheKey);
        if (cached != null) return cached;
        
        // Cache miss - fetch from DB
        var product = await _repository.GetByIdAsync(id);
        if (product == null) return null;
        
        var dto = _mapper.Map<ProductDto>(product);
        
        // Store in cache with 5-minute expiration
        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5));
        return dto;
    }

    // Always invalidate cache when updating
    public async Task UpdateProductAsync(int id, UpdateProductDto dto)
    {
        // Update in DB
        await _repository.UpdateAsync(id, dto);
        
        // Invalidate cache
        await _cache.RemoveAsync($"product:{id}");
    }
}
```

### 4. Health Checks

**Every service MUST have health checks in Program.cs:**
```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString ?? throw new InvalidOperationException("Connection string required"),
        name: "database",
        tags: new[] { "db", "sql", "sqlserver" })
    .AddRedis(
        builder.Configuration["Redis:Connection"] ?? "localhost:6379",
        name: "redis-cache",
        tags: new[] { "cache", "redis" });

// After app.Build()
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                data = e.Value.Data
            })
        });
        await context.Response.WriteAsync(result);
    }
});
```

### 5. Authentication & Authorization

**JWT configuration (in Program.cs):**
```csharp
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] 
    ?? throw new InvalidOperationException("JWT SecretKey required");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    });
```

**Using authorization in controllers:**
```csharp
[ApiController]
[Authorize] // Require authentication
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous] // Override - allow public access
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll() { ... }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")] // Require specific roles
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto) { ... }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanDeleteProducts")] // Require policy-based authorization
    public async Task<ActionResult> Delete(int id) { ... }
}
```

### 6. Logging Best Practices

**Use structured logging with sensitive data masking:**
```csharp
public class AuthService : IAuthService
{
    private readonly ILogger<AuthService> _logger;

    public async Task<TokenResponseDto?> LoginAsync(LoginDto dto)
    {
        // ✅ Good: Structured logging with sanitized data
        _logger.LogInformation(
            "Login attempt for user {Email}",
            dto.Email); // Email is okay, but never log passwords

        try
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                // ✅ Good: Log failure without exposing data
                _logger.LogWarning(
                    "Login failed: User not found for email {Email}",
                    dto.Email);
                return null;
            }

            // ... authentication logic ...

            _logger.LogInformation(
                "User {UserId} logged in successfully",
                user.Id);
            
            return tokenResponse;
        }
        catch (Exception ex)
        {
            // ✅ Good: Log exception with context
            _logger.LogError(ex,
                "Login error for user {Email}",
                dto.Email);
            throw;
        }
    }
}

// ❌ Bad: Don't do this
_logger.LogInformation($"Login with {dto.Email} and password {dto.Password}"); // NEVER!
```

---

## 🔒 Security Best Practices

### Secret Management

**❌ NEVER do this:**
```csharp
var apiKey = "sk_live_123456789"; // Hardcoded secret
```

**✅ Always do this:**
```csharp
// In appsettings.json (local dev only)
{
  "ExternalApi": {
    "ApiKey": "your-dev-key"
  }
}

// In production: use Azure Key Vault references
// In Program.cs:
var apiKey = builder.Configuration["ExternalApi:ApiKey"]
    ?? throw new InvalidOperationException("API key required");
```

### Input Validation

**Always validate DTOs with Data Annotations:**
```csharp
public record CreateCustomerDto
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; init; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone format")]
    public string? Phone { get; init; }

    [Range(0, 150, ErrorMessage = "Age must be between 0 and 150")]
    public int? Age { get; init; }
}

// In controller
[HttpPost]
public async Task<ActionResult<CustomerDto>> Create([FromBody] CreateCustomerDto dto)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState); // Returns validation errors

    // ... proceed with creation ...
}
```

### SQL Injection Prevention

**✅ Always use parameterized queries (EF Core does this by default):**
```csharp
// ✅ Good: EF Core uses parameters automatically
var users = await _context.Users
    .Where(u => u.Email == email)
    .ToListAsync();

// ✅ Good: Explicit parameters
var users = await _context.Users
    .FromSqlRaw("SELECT * FROM Users WHERE Email = {0}", email)
    .ToListAsync();

// ❌ Bad: String concatenation (SQL injection risk)
var users = await _context.Users
    .FromSqlRaw($"SELECT * FROM Users WHERE Email = '{email}'")
    .ToListAsync();
```

---

## 🧪 Testing Patterns

### Integration Tests with Aspire

```csharp
// src/AppHost.Tests/Tests/AuthServiceTests.cs
public class AuthServiceTests : IAsyncLifetime
{
    private DistributedApplication? _app;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.AppHost>();
        _app = await appHost.BuildAsync();
        await _app.StartAsync();
        
        // Always test through the gateway
        _client = _app.CreateHttpClient("gateway");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var loginDto = new { Email = "test@example.com", Password = "wrong" };
        
        var response = await _client!.PostAsJsonAsync("/auth/api/auth/login", loginDto);
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        var registerDto = new 
        { 
            Email = $"test{Guid.NewGuid()}@example.com",
            Password = "Test123!",
            Username = "testuser"
        };
        
        var response = await _client!.PostAsJsonAsync("/auth/api/auth/register", registerDto);
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var token = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
        Assert.NotNull(token);
        Assert.NotEmpty(token.AccessToken);
    }

    public async Task DisposeAsync()
    {
        if (_app != null)
            await _app.DisposeAsync();
        _client?.Dispose();
    }
}
```

### Unit Tests

```csharp
public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<ILogger<ProductService>> _loggerMock;
    private readonly ProductService _sut; // System Under Test

    public ProductServiceTests()
    {
        _repositoryMock = new Mock<IProductRepository>();
        _mapperMock = new Mock<IMapper>();
        _cacheMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<ProductService>>();
        
        _sut = new ProductService(
            _repositoryMock.Object,
            _mapperMock.Object,
            _cacheMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCached_ReturnsCachedValue()
    {
        // Arrange
        var productDto = new ProductDto { Id = 1, Name = "Test Product" };
        _cacheMock
            .Setup(c => c.GetAsync<ProductDto>("product:1"))
            .ReturnsAsync(productDto);

        // Act
        var result = await _sut.GetByIdAsync(1);

        // Assert
        Assert.Equal(productDto, result);
        _repositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }
}
```

---

## 📊 Database Patterns

### DbContext Configuration

```csharp
// MyApp.Inventory.Infrastructure/Data/InventoryDbContext.cs
public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StockItem> StockItems => Set<StockItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
        
        // Or apply specific configurations
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
    }
}

// Separate entity configuration
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(p => p.Price)
            .HasPrecision(18, 2);
        
        builder.HasIndex(p => p.Sku).IsUnique();
        
        // Relationships
        builder.HasMany(p => p.StockItems)
            .WithOne(s => s.Product)
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### Repository Pattern

```csharp
// Generic repository base class
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly DbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
```

---

## 🚨 Anti-Patterns & What to Avoid

### ❌ Cross-Service Database Access
```csharp
// ❌ NEVER do this
public class OrderService
{
    private readonly OrderDbContext _orderContext;
    private readonly InventoryDbContext _inventoryContext; // WRONG!
    
    public async Task CreateOrder(CreateOrderDto dto)
    {
        // ❌ Don't query other service's database directly
        var product = await _inventoryContext.Products.FindAsync(dto.ProductId);
    }
}

// ✅ Always use service-to-service communication
public class OrderService
{
    private readonly DaprClient _daprClient;
    
    public async Task CreateOrder(CreateOrderDto dto)
    {
        // ✅ Call the service via DAPR
        var httpClient = _daprClient.CreateInvokeHttpClient("inventory-service");
        var product = await httpClient.GetFromJsonAsync<ProductDto>(
            $"/api/inventory/products/{dto.ProductId}");
    }
}
```

### ❌ Returning Domain Entities from API
```csharp
// ❌ NEVER expose domain entities
[HttpGet("{id}")]
public async Task<ActionResult<Product>> GetProduct(int id) // Domain entity!
{
    return await _repository.GetByIdAsync(id);
}

// ✅ Always use DTOs
[HttpGet("{id}")]
public async Task<ActionResult<ProductDto>> GetProduct(int id)
{
    var product = await _repository.GetByIdAsync(id);
    return _mapper.Map<ProductDto>(product);
}
```

### ❌ Storing Secrets in Code
```csharp
// ❌ Hardcoded credentials
var connectionString = "Server=prod-server;Database=ERP;User=sa;Password=P@ssw0rd123";

// ✅ Use configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string required");
```

### ❌ Synchronous Blocking Calls
```csharp
// ❌ Blocking async code
public IActionResult GetProducts()
{
    var products = _service.GetAllAsync().Result; // Blocks thread!
    return Ok(products);
}

// ✅ Async all the way
public async Task<IActionResult> GetProducts()
{
    var products = await _service.GetAllAsync();
    return Ok(products);
}
```

---

## 🧭 Navigation & Discovery

### Finding Things Quickly

| I need to find... | Look here |
|-------------------|-----------|
| **API endpoint** | Search for `[Route("api/[controller]")]` in `[Service].API/Controllers/` |
| **Business logic** | `[Service].Application/Services/` |
| **Data access code** | `[Service].Infrastructure/Data/Repositories/` |
| **Domain model** | `[Service].Domain/Entities/` |
| **Database schema** | `[Service]DbContext.cs` and EF migrations |
| **Gateway routes** | `src/ErpApiGateway/ocelot.json` |
| **Configuration** | `appsettings.json`, `appsettings.Development.json` |
| **Infrastructure code** | `infra/main.bicep` and `infra/core/` |
| **Tests** | `src/AppHost.Tests/Tests/` |
| **Shared utilities** | `src/MyApp.Shared.*/` |

### Code Search Patterns

```bash
# Find all controllers
find . -name "*Controller.cs"

# Find all repositories
find . -name "*Repository.cs"

# Search for specific API routes
grep -r "Route\[" --include="*.cs"

# Find DAPR usage
grep -r "DaprClient" --include="*.cs"

# Find all migration files
find . -path "*/Migrations/*.cs"
```

---

## 🆘 Troubleshooting Guide

### Service Won't Start

**Problem:** Service fails to start with configuration error

**Solutions:**
1. Check `appsettings.Development.json` exists and is valid JSON
2. Verify connection strings:
   ```json
   {
     "ConnectionStrings": {
       "AuthDb": "Server=localhost;Database=AuthDb;Integrated Security=true;"
     }
   }
   ```
3. Ensure JWT secret is configured:
   ```json
   {
     "Jwt": {
       "SecretKey": "your-256-bit-secret-key-goes-here-minimum-32-characters",
       "Issuer": "MyApp.Auth",
       "Audience": "MyApp.All"
     }
   }
   ```
4. Check if ports are already in use (default: 8080 per service)

### DAPR Communication Failing

**Problem:** Service-to-service calls return 404 or timeout

**Solutions:**
1. Verify app-id matches service name in DAPR configuration
2. Check service is running and healthy: `/health` endpoint
3. Verify network connectivity between services
4. Check DAPR sidecar is running: `dapr list`
5. Review DAPR configuration in `AppHost/Program.cs`

### Database Migration Issues

**Problem:** `dotnet ef migrations add` fails

**Solutions:**
1. Ensure you're in the **Infrastructure project directory**:
   ```powershell
   cd src/MyApp.Auth/MyApp.Auth.Infrastructure
   ```
2. Verify tools are installed:
   ```powershell
   dotnet tool install --global dotnet-ef
   ```
3. Check connection string is valid
4. Ensure DbContext is properly registered in Program.cs

### Gateway Route Not Working

**Problem:** API calls through gateway return 404

**Solutions:**
1. Verify route configuration in `src/ErpApiGateway/ocelot.json`:
   - `DownstreamPathTemplate` matches service route
   - `UpstreamPathTemplate` matches client request
   - Service name in `DownstreamHostAndPorts` is correct
2. Restart gateway after ocelot.json changes
3. Check service is registered in Aspire `AppHost/Program.cs`
4. Test direct service access first: `http://service-name:8080/api/...`

### Redis Cache Not Working

**Problem:** Cache always returns null

**Solutions:**
1. Verify Redis is running:
   ```powershell
   docker ps | grep redis
   ```
2. Check Redis connection string in configuration
3. Verify `ICacheService` is registered in DI
4. Check Redis logs for connection errors
5. Test Redis connection:
   ```bash
   redis-cli ping
   ```

### Authentication Failures

**Problem:** JWT validation fails with 401 Unauthorized

**Solutions:**
1. Verify JWT secret matches between Auth service and other services
2. Check token expiration hasn't passed
3. Ensure `Authorization: Bearer {token}` header is present
4. Validate issuer and audience match configuration
5. Check ClockSkew setting if time synchronization is an issue

---

## 📚 Documentation

### Must-Read Documents
1. **[Architecture Overview](../docs/architecture/ARCHITECTURE_DOCUMENTATION.md)** - System design, patterns, infrastructure
2. **[Quick Start Guide](../docs/guides/QUICKSTART.md)** - 5-minute setup
3. **[Deployment Guide](../docs/deployment/DEPLOYMENT.md)** - Production deployment procedures
4. **[README.md](../README.md)** - Project overview and features
5. **[azure.yaml](../azure.yaml)** - Azure deployment configuration

### External Resources
- [.NET 10 Documentation](https://learn.microsoft.com/dotnet/)
- [ASP.NET Core Web API](https://learn.microsoft.com/aspnet/core/web-api/)
- [Entity Framework Core](https://learn.microsoft.com/ef/core/)
- [DAPR Documentation](https://docs.dapr.io/)
- [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/)
- [Azure Container Apps](https://learn.microsoft.com/azure/container-apps/)
- [Ocelot API Gateway](https://ocelot.readthedocs.io/)

---

## 🎓 Decision Trees

### "Should I create a new microservice?"

```
Is the feature a distinct business domain?
├─ NO → Add to existing service
└─ YES
    └─ Does it need its own database?
        ├─ NO → Add to existing service
        └─ YES
            └─ Will it scale independently?
                ├─ NO → Add to existing service
                └─ YES → Create new microservice
```

### "Where should this code go?"

```
What kind of code is this?
├─ HTTP endpoint → [Service].API/Controllers/
├─ Business logic → [Service].Application/Services/
├─ Data access → [Service].Infrastructure/Data/Repositories/
├─ Domain model → [Service].Domain/Entities/
├─ DTO → [Service].Application.Contracts/DTOs/
├─ Cross-cutting concern → MyApp.Shared.*/
└─ Configuration → appsettings.*.json or Azure Key Vault
```

### "How should services communicate?"

```
What's the use case?
├─ Need immediate response?
│   ├─ YES → Use DAPR HTTP service invocation
│   └─ NO → Continue
├─ Fire-and-forget notification?
│   └─ YES → Use DAPR Pub/Sub
├─ Need to query data from another service?
│   └─ YES → Use DAPR HTTP (consider caching)
└─ Background processing?
    └─ YES → Use DAPR Pub/Sub + worker service
```

---

## ✅ Pre-Commit Checklist

Before committing code, verify:

- [ ] Code builds without errors: `dotnet build`
- [ ] All tests pass: `dotnet test`
- [ ] No hardcoded secrets or credentials
- [ ] API changes reflected in OpenAPI/Swagger docs
- [ ] Database migrations added if schema changed
- [ ] Gateway routes updated if new endpoints added
- [ ] DTOs used for API responses (not domain entities)
- [ ] Appropriate logging added
- [ ] Error handling implemented
- [ ] Input validation on DTOs
- [ ] Health checks working
- [ ] CORS configured if needed
- [ ] Authentication/authorization applied

---

## 🎯 Quick Commands Reference

```powershell
# Development
dotnet run --project src/AppHost                    # Run all services
dotnet run --project src/MyApp.Auth/MyApp.Auth.API  # Run single service
dotnet build                                         # Build solution
dotnet test                                          # Run all tests
dotnet watch run                                     # Run with hot reload

# Database
cd src/MyApp.Auth/MyApp.Auth.Infrastructure
dotnet ef migrations add MigrationName              # Add migration
dotnet ef database update                            # Apply migrations
dotnet ef migrations script --output script.sql     # Generate SQL script
dotnet ef database drop --force                      # Drop database (dev only!)

# Deployment
azd up                                               # Deploy to Azure
azd monitor                                          # View logs
azd down                                             # Delete resources

# Docker
docker-compose up                                    # Start all containers
docker-compose down                                  # Stop all containers
docker ps                                            # List running containers
docker logs [container-name]                         # View logs

# Package Management
dotnet add package [PackageName]                     # Add NuGet package
dotnet restore                                       # Restore packages
dotnet list package --vulnerable                     # Check for vulnerabilities
```

---

**Last Updated:** 2026-01-23  
**Version:** 2.0  
**For Support:** Check `README.md` or raise an issue in the repository.

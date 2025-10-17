# Permission System Documentation

## Overview

The permission system in MyApp is based on a **Module + Action** pattern, where permissions are defined as combinations of a module (e.g., Inventory, Purchasing) and an action (e.g., Read, Write, Delete).

## Permission Format

All permissions follow the pattern: `{Module}.{Action}`

Examples:
- `Inventory.Read` - Read access to Inventory module
- `Purchasing.Write` - Write/Create/Update access to Purchasing module
- `Orders.Delete` - Delete access to Orders module

## Available Modules

| Module | Description |
|--------|-------------|
| `Inventory` | Manages products, warehouses, and stock levels |
| `Purchasing` | Manages suppliers and purchase orders |
| `Orders` | Manages customer orders |
| `Sales` | Manages sales orders and customers |
| `Billing` | Manages invoices and payments |
| `Notification` | Manages alerts and communications |
| `Auth` | Manages users, roles, and authentication |

## Available Actions

| Action | Value | Description |
|--------|-------|-------------|
| `Read` | 1 | View/retrieve data |
| `Write` | 2 | Create and modify data |
| `Delete` | 3 | Remove data |
| `Execute` | 4 | Run operations and commands |
| `Export` | 5 | Export data |
| `Import` | 6 | Import data |

## Using Permissions in Code

### Option 1: Using PermissionConstants (Recommended)

```csharp
using MyApp.Shared.Domain.Permissions;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    // Use predefined constants for type safety
    [HttpGet("products")]
    [Authorize]
    [Permission(PermissionConstants.Inventory.Read)]
    public async Task<IActionResult> GetProducts()
    {
        return Ok();
    }

    [HttpPost("products")]
    [Authorize]
    [Permission(PermissionConstants.Inventory.Write)]
    public async Task<IActionResult> CreateProduct()
    {
        return Ok();
    }

    [HttpDelete("products/{id}")]
    [Authorize]
    [Permission(PermissionConstants.Inventory.Delete)]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        return Ok();
    }
}
```

### Option 2: Using PermissionEnum

```csharp
using MyApp.Shared.Domain.Permissions;

// Create a permission enum
var permission = new PermissionEnum 
{ 
    Module = ModuleEnum.Inventory, 
    Action = ActionEnum.Read 
};

// Convert to string
string permissionString = permission.ToString(); // "Inventory.Read"
```

### Option 3: Using String Constants

```csharp
using MyApp.Shared.Domain.Permissions;

[HttpGet("products")]
[Authorize]
[Permission("Inventory.Read")]
public async Task<IActionResult> GetProducts()
{
    return Ok();
}
```

## Common Usage Patterns

### Get All Permissions for a Module

```csharp
var inventoryPermissions = PermissionConstants.GetModulePermissions(ModuleEnum.Inventory);
// Returns: ["Inventory.Read", "Inventory.Write", "Inventory.Delete", "Inventory.Execute", "Inventory.Export", "Inventory.Import"]
```

### Get All System Permissions

```csharp
var allPermissions = PermissionConstants.GetAllPermissions();
```

### Create Permission Dynamically

```csharp
var permission = PermissionConstants.Create(ModuleEnum.Inventory, ActionEnum.Read);
// Result: "Inventory.Read"
```

### Parse Permission String

```csharp
var permission = PermissionEnum.Parse("Inventory.Read");
if (permission != null)
{
    var module = permission.Module; // ModuleEnum.Inventory
    var action = permission.Action; // ActionEnum.Read
}

// Or use TryParse
if (PermissionEnum.TryParse("Inventory.Read", out var parsedPermission))
{
    // Use parsedPermission
}
```

### Extract Module or Action from String

```csharp
var module = PermissionConstants.GetModule("Inventory.Read"); // "Inventory"
var action = PermissionConstants.GetAction("Inventory.Read");  // "Read"
```

## Permission Attribute Implementation

To use the `[Permission]` attribute shown in the examples above, you need to create a custom authorization attribute:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MyApp.Shared.Domain.Permissions;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class PermissionAttribute : AuthorizeAttribute, IAsyncAuthorizationFilter
{
    private readonly string _permission;

    public PermissionAttribute(string permission)
    {
        _permission = permission;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var permissionService = context.HttpContext.RequestServices
            .GetRequiredService<IPermissionService>();

        var userId = Guid.Parse(user.FindFirst("sub")?.Value ?? "");
        var hasPermission = await permissionService.HasPermissionAsync(userId, 
            PermissionConstants.GetModule(_permission),
            PermissionConstants.GetAction(_permission));

        if (!hasPermission)
        {
            context.Result = new ForbidResult();
        }
    }
}
```

## Integration with Auth Module

The Auth module manages permission assignments through:

1. **Role Permissions**: Permissions assigned to roles (via `RolePermission` join table)
2. **User Permissions**: Direct permissions assigned to users (via `UserPermission` join table)

When a user makes a request:
1. The JWT token is validated
2. The user's ID is extracted from the token
3. Permission check queries both user-specific and role-based permissions
4. Access is granted only if at least one matching permission exists

## Database Schema

### Permission Table
```sql
CREATE TABLE [asp].[Permission] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY,
    [Module] NVARCHAR(50) NOT NULL,      -- e.g., "Inventory"
    [Action] NVARCHAR(50) NOT NULL       -- e.g., "Read"
);
```

### RolePermission Join Table
```sql
CREATE TABLE [auth].[RolePermission] (
    [RoleId] UNIQUEIDENTIFIER NOT NULL,
    [PermissionId] UNIQUEIDENTIFIER NOT NULL,
    FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles](Id),
    FOREIGN KEY ([PermissionId]) REFERENCES [asp].[Permission](Id)
);
```

### UserPermission Join Table
```sql
CREATE TABLE [auth].[UserPermission] (
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [PermissionId] UNIQUEIDENTIFIER NOT NULL,
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers](Id),
    FOREIGN KEY ([PermissionId]) REFERENCES [asp].[Permission](Id)
);
```

## Best Practices

1. **Use PermissionConstants for Type Safety**: Always use `PermissionConstants` rather than magic strings
2. **Apply Consistently**: Apply permission checks to all endpoints that modify data
3. **Combine with Roles**: Use role-based permissions for common patterns, user-specific permissions for exceptions
4. **Document Required Permissions**: Document which permissions are required for each endpoint
5. **Audit Permissions**: Log all permission checks for security audit trails

## Migration: Seeding Initial Permissions

To seed the database with initial permissions:

```csharp
// In a migration or seeding method
var permissions = new[]
{
    new Permission { Id = Guid.NewGuid(), Module = "Inventory", Action = "Read" },
    new Permission { Id = Guid.NewGuid(), Module = "Inventory", Action = "Write" },
    new Permission { Id = Guid.NewGuid(), Module = "Inventory", Action = "Delete" },
    // ... add all other module/action combinations
};

dbContext.Set<Permission>().AddRange(permissions);
await dbContext.SaveChangesAsync();
```

Or use the helper method:

```csharp
var allPermissions = PermissionConstants.GetAllPermissions()
    .Select(p => new Permission 
    { 
        Id = Guid.NewGuid(),
        Module = PermissionConstants.GetModule(p),
        Action = PermissionConstants.GetAction(p)
    })
    .ToList();

dbContext.Set<Permission>().AddRange(allPermissions);
await dbContext.SaveChangesAsync();
```

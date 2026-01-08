# Test Fix Strategy - DTO Constructor Updates

## Problem
The DTOs have been refactored from positional records to records with `init` properties:

### Old Style (Positional)
```csharp
public record UserDto(Guid Id, string Email, string Username, ...);
// Usage: new UserDto(guid, email, username, ...)
```

### New Style (Init Properties)
```csharp
public record UserDto(Guid Id) : AuditableGuidDto(Id)
{
    public string? Email { get; init; }
    public string? Username { get; init; }
    // ...
}
// Usage: new UserDto(guid) { Email = email, Username = username, ... }
```

## Solution
Update all test files to use object initializer syntax instead of positional constructors.

## Files to Fix
1. UserBuilders.cs - UserDtoBuilder.Build()
2. RoleBuilders.cs - RoleDtoBuilder.Build()
3. PermissionBuilders.cs - PermissionDtoBuilder.Build()
4. AuthServiceTests.cs - LoginDto instantiation
5. All service test files with DTO instantiations

## Pattern
Replace:
```csharp
new UserDto(id, createdAt, createdBy, updatedAt, updatedBy, email, username, firstName, lastName, ...)
```

With:
```csharp
new UserDto(id)
{
    CreatedAt = createdAt,
    CreatedBy = createdBy,
    UpdatedAt = updatedAt,
    UpdatedBy = updatedBy,
    Email = email,
    Username = username,
    FirstName = firstName,
    LastName = lastName,
    ...
}
```

For LoginDto (positional record - correct usage):
```csharp
new LoginDto(email, password)  // Correct - this IS a positional record
```

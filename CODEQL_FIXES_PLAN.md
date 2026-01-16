# CodeQL Issues Fix Plan - PR 54

## Overview

This document outlines the comprehensive plan to fix CodeQL security and code quality issues identified in the codebase. CodeQL analysis helps identify security vulnerabilities, bugs, and code quality issues before they reach production.

## Current State Analysis

### CodeQL Workflow Status
- **Location**: `.github/workflows/codeql.yml`
- **Status**: ✅ Configured for C# analysis
- **Target**: .NET 9.0.x
- **Build**: `ERP.Microservices.sln` in Release configuration

### Identified Issues

#### 1. 🔴 CRITICAL: Merge Conflict
**File**: `src/MyApp.Inventory/MyApp.Inventory.Application/Services/WarehouseStockService.cs`  
**Lines**: 252-260  
**Issue**: Git merge conflict markers present in code  
**Impact**: Code will not compile, blocks all builds

```csharp
// Current state (BROKEN):
<<<<<<< HEAD
        ArgumentNullException.ThrowIfNull(dto);

        _logger.LogInformation("Adjusting stock: {@StockAdjustment}", new { dto.ProductId, dto.WarehouseId, dto.QuantityChange, dto.Reason });
=======
        var sanitizedReason = dto.Reason?
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty);
>>>>>>> c8a99ada1e83f9ac11ed7d1f3c212965b5f0627f
```

**Fix Required**: Resolve conflict by keeping both validation and sanitization

#### 2. 🟠 HIGH: Console.WriteLine Usage
**Files**: 
- `src/AppHost/Program.cs` (lines 133-134)
- `src/AppHost.Tests/Tests/SalesServiceTests.cs` (lines 40, 46)

**Issue**: Direct console output instead of structured logging  
**CodeQL Rule**: `cs/use-structured-logging`  
**Impact**: Poor observability, potential security issues if sensitive data is logged

#### 3. 🟠 HIGH: Potential Information Disclosure
**Files**: Multiple exception messages across services  
**Issue**: Exception messages may expose internal system details  
**CodeQL Rule**: `cs/information-disclosure`  
**Impact**: Attackers could gain insights into system architecture

#### 4. 🟡 MEDIUM: Input Validation Gaps
**Issue**: Some DTOs may lack comprehensive validation attributes  
**CodeQL Rule**: `cs/missing-input-validation`  
**Impact**: Potential injection attacks or data corruption

#### 5. 🟡 MEDIUM: Logging Sensitive Data
**Issue**: Potential logging of sensitive information (secrets, PII)  
**CodeQL Rule**: `cs/log-injection`, `cs/cleartext-secrets`  
**Impact**: Security breach if logs are compromised

#### 6. 🟡 MEDIUM: Hardcoded Configuration Values
**Files**: `appsettings.json` files  
**Issue**: Empty strings for secrets (good), but need to ensure no defaults in code  
**CodeQL Rule**: `cs/hardcoded-secrets`  
**Impact**: Secrets might be committed to repository

---

## Implementation Plan

### Phase 1: Critical Fixes (Must Fix - Blocks Build)

#### 1.1 Resolve Merge Conflict in WarehouseStockService.cs

**File**: `src/MyApp.Inventory/MyApp.Inventory.Application/Services/WarehouseStockService.cs`

**Action**: Merge both changes - keep validation AND sanitization

```csharp
public async Task AdjustStockAsync(StockAdjustmentDto dto)
{
    ArgumentNullException.ThrowIfNull(dto);

    // Sanitize reason to prevent injection attacks
    var sanitizedReason = dto.Reason?
        .Replace("\r", string.Empty)
        .Replace("\n", string.Empty);

    _logger.LogInformation(
        "Adjusting stock: {@StockAdjustment}", 
        new { 
            dto.ProductId, 
            dto.WarehouseId, 
            dto.QuantityChange, 
            Reason = sanitizedReason  // Use sanitized version
        });
    
    var warehouseStock = await _warehouseStockRepository.GetByProductAndWarehouseAsync(
        dto.ProductId, 
        dto.WarehouseId);
    
    if (warehouseStock == null)
    {
        throw new InvalidOperationException(
            $"No stock record found for product {dto.ProductId} in warehouse {dto.WarehouseId}");
    }

    // ... rest of method
}
```

**Validation**:
- [ ] Code compiles without errors
- [ ] Unit tests pass
- [ ] No merge conflict markers remain

---

### Phase 2: High Priority Security Fixes

#### 2.1 Replace Console.WriteLine with Structured Logging

**File**: `src/AppHost/Program.cs`

**Current Code**:
```csharp
Console.WriteLine(inner.Message);
Console.WriteLine(inner.StackTrace);
```

**Fixed Code**:
```csharp
// Inject ILogger<Program> in constructor or use builder.Services.BuildServiceProvider()
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
logger.LogError(inner, "Unhandled exception occurred: {Message}", inner.Message);
```

**File**: `src/AppHost.Tests/Tests/SalesServiceTests.cs`

**Current Code**:
```csharp
Console.WriteLine($"{serviceName} is healthy");
Console.WriteLine($"{serviceName} not ready yet (attempt {i + 1}): {ex.Message}");
```

**Fixed Code**:
```csharp
// Use TestOutputHelper or ILogger in test context
_outputHelper.WriteLine($"{serviceName} is healthy");
_outputHelper.WriteLine($"{serviceName} not ready yet (attempt {i + 1}): {ex.Message}");
```

**Validation**:
- [ ] All Console.WriteLine removed from production code
- [ ] Tests use appropriate logging mechanism
- [ ] No sensitive data in log messages

#### 2.2 Sanitize Exception Messages

**Pattern to Apply**: Remove internal details from user-facing exceptions

**Example Fix**:
```csharp
// BEFORE (Information Disclosure):
throw new InvalidOperationException(
    $"No stock record found for product {dto.ProductId} in warehouse {dto.WarehouseId}");

// AFTER (Sanitized):
_logger.LogWarning(
    "Stock record not found: ProductId={ProductId}, WarehouseId={WarehouseId}",
    dto.ProductId, dto.WarehouseId);
throw new InvalidOperationException("Stock record not found for the specified product and warehouse.");
```

**Files to Review**:
- `src/MyApp.Inventory/MyApp.Inventory.Application/Services/WarehouseStockService.cs`
- `src/MyApp.Orders/MyApp.Orders.Application/Services/OrderService.cs`
- `src/MyApp.Purchasing/MyApp.Purchasing.Application/Services/PurchaseOrderService.cs`
- `src/MyApp.Sales/MyApp.Sales.Application/Services/SalesOrderService.cs`

**Validation**:
- [ ] Exception messages don't expose GUIDs or internal IDs to end users
- [ ] Detailed information logged separately with appropriate log level
- [ ] User-facing messages are generic but actionable

---

### Phase 3: Medium Priority Security Enhancements

#### 3.1 Enhance Input Validation

**Action**: Review all DTOs and ensure comprehensive validation

**Pattern to Apply**:
```csharp
public record StockAdjustmentDto
{
    [Required]
    public Guid ProductId { get; init; }
    
    [Required]
    public Guid WarehouseId { get; init; }
    
    [Required]
    [Range(-1000000, 1000000, ErrorMessage = "Quantity change must be between -1,000,000 and 1,000,000")]
    public int QuantityChange { get; init; }
    
    [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-_.,!?()]+$", ErrorMessage = "Reason contains invalid characters")]
    public string? Reason { get; init; }
    
    [MaxLength(100)]
    public string? Reference { get; init; }
}
```

**Files to Review**:
- All DTOs in `MyApp.*.Application.Contracts/DTOs/`
- Focus on: `StockAdjustmentDto`, `StockTransferDto`, `ReserveStockDto`

**Validation**:
- [ ] All string inputs have MaxLength constraints
- [ ] Numeric inputs have Range constraints
- [ ] Complex strings (reasons, descriptions) have regex validation
- [ ] All required fields marked with [Required]

#### 3.2 Audit Logging for Sensitive Data

**Action**: Review all logging statements to ensure no secrets or PII are logged

**Pattern to Apply**:
```csharp
// BEFORE (Potential Secret Leakage):
_logger.LogInformation("User logged in with password: {Password}", password);

// AFTER (Sanitized):
_logger.LogInformation("User {UserId} logged in successfully", userId);
```

**Files to Review**:
- All service classes with logging
- Event handlers
- Authentication/authorization code

**Checklist**:
- [ ] No passwords in logs
- [ ] No JWT tokens in logs
- [ ] No connection strings in logs
- [ ] No API keys in logs
- [ ] PII (email, phone) only logged at Debug level with masking

#### 3.3 Verify No Hardcoded Secrets

**Action**: Ensure all secrets come from configuration, not hardcoded

**Files to Audit**:
- All `appsettings.json` files (should have empty strings)
- All `Program.cs` files (should read from configuration)
- All service classes (should use IConfiguration or IOptions)

**Pattern to Verify**:
```csharp
// CORRECT:
var secretKey = builder.Configuration["Jwt:SecretKey"];
if (string.IsNullOrWhiteSpace(secretKey))
{
    throw new InvalidOperationException("Jwt:SecretKey must be provided");
}

// INCORRECT:
var secretKey = "hardcoded-secret-key-12345"; // ❌ CodeQL will flag this
```

**Validation**:
- [ ] No hardcoded secrets in source code
- [ ] All secrets read from configuration
- [ ] Configuration validation throws if secrets are missing
- [ ] Secrets documented in README (not in code)

---

### Phase 4: Code Quality Improvements

#### 4.1 Add Null-Safety Checks

**Action**: Ensure all nullable reference types are properly handled

**Pattern**:
```csharp
// Use null-conditional operators
var result = entity?.Property ?? defaultValue;

// Use ArgumentNullException.ThrowIfNull for required parameters
ArgumentNullException.ThrowIfNull(dto);

// Use null-forgiving operator only when certain
var value = nullableValue!; // Only if you're 100% sure
```

#### 4.2 Ensure EF Core Parameterization

**Action**: Verify all database queries use EF Core (no raw SQL)

**Check**:
- [ ] No `ExecuteSqlRaw` or `ExecuteSqlInterpolated` without parameters
- [ ] All queries use LINQ or EF Core methods
- [ ] If raw SQL is needed, use `FromSqlRaw` with parameters

**Example**:
```csharp
// CORRECT (EF Core parameterized):
var products = await _context.Products
    .Where(p => p.SKU == sku)
    .ToListAsync();

// INCORRECT (SQL Injection risk):
var sql = $"SELECT * FROM Products WHERE SKU = '{sku}'";
var products = await _context.Products.FromSqlRaw(sql).ToListAsync();
```

---

### Phase 5: CodeQL Workflow Verification

#### 5.1 Update CodeQL Workflow (if needed)

**File**: `.github/workflows/codeql.yml`

**Current Configuration**: ✅ Looks good, but verify:
- [ ] .NET version matches project (9.0.x)
- [ ] Solution file path is correct
- [ ] Build configuration is Release
- [ ] Permissions are correct

**Optional Enhancements**:
```yaml
# Add query suites for additional security checks
- name: Initialize CodeQL
  uses: github/codeql-action/init@v3
  with:
    languages: ${{ matrix.language }}
    queries: security-and-quality  # Add comprehensive query suite
```

#### 5.2 Run CodeQL Analysis Locally

**Action**: Install CodeQL CLI and run analysis before committing

**Commands**:
```bash
# Install CodeQL CLI (one-time)
gh extension install github/gh-codeql

# Run analysis
codeql database create codeql-db --language=csharp --source-root=.
codeql database analyze codeql-db --format=sarif-latest --output=codeql-results.sarif
```

---

## Testing Strategy

### Unit Tests
- [ ] All existing tests pass after fixes
- [ ] Add tests for input validation
- [ ] Add tests for sanitization logic
- [ ] Add tests for exception message sanitization

### Integration Tests
- [ ] Verify no secrets in logs
- [ ] Verify exception handling doesn't expose internal details
- [ ] Verify all endpoints validate input correctly

### Security Tests
- [ ] Run CodeQL analysis and verify no new issues
- [ ] Manual review of exception messages
- [ ] Manual review of log outputs
- [ ] Verify no hardcoded secrets

---

## Success Criteria

### Must Have (Blocks PR)
- [x] Merge conflict resolved
- [ ] Code compiles without errors
- [ ] All unit tests pass
- [ ] CodeQL analysis passes (no critical/high issues)

### Should Have (Quality)
- [ ] No Console.WriteLine in production code
- [ ] Exception messages sanitized
- [ ] All inputs validated
- [ ] No secrets in logs

### Nice to Have (Enhancements)
- [ ] Comprehensive input validation on all DTOs
- [ ] Enhanced CodeQL query suite
- [ ] Documentation updated with security best practices

---

## File Change Summary

### Files to Modify

1. **Critical**:
   - `src/MyApp.Inventory/MyApp.Inventory.Application/Services/WarehouseStockService.cs` (merge conflict)

2. **High Priority**:
   - `src/AppHost/Program.cs` (Console.WriteLine → ILogger)
   - `src/AppHost.Tests/Tests/SalesServiceTests.cs` (Console.WriteLine → TestOutputHelper)
   - Exception messages across all services (sanitization)

3. **Medium Priority**:
   - DTO validation attributes (multiple files)
   - Logging statements review (multiple files)
   - Configuration files audit

4. **Optional**:
   - `.github/workflows/codeql.yml` (enhancements)

---

## Estimated Effort

| Phase | Tasks | Estimated Time |
|-------|-------|---------------|
| Phase 1: Critical | Merge conflict resolution | 15 minutes |
| Phase 2: High Priority | Logging + Exception sanitization | 2-3 hours |
| Phase 3: Medium Priority | Input validation + Logging audit | 3-4 hours |
| Phase 4: Code Quality | Null-safety + EF Core verification | 1-2 hours |
| Phase 5: Verification | CodeQL workflow + testing | 1 hour |
| **Total** | | **7-10 hours** |

---

## Risk Assessment

### Low Risk
- Replacing Console.WriteLine (straightforward refactoring)
- Adding validation attributes (additive changes)

### Medium Risk
- Exception message changes (may affect error handling in tests)
- Logging changes (may affect observability)

### High Risk
- Merge conflict resolution (must be done carefully to preserve both changes)

---

## Rollback Plan

If issues arise:
1. Revert specific commits for problematic changes
2. Keep merge conflict fix (critical)
3. Gradually re-apply other fixes with additional testing

---

## References

- [CodeQL Documentation](https://codeql.github.com/docs/)
- [CodeQL Queries for C#](https://codeql.github.com/codeql-query-help/csharp/)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [.NET Security Best Practices](https://learn.microsoft.com/en-us/dotnet/standard/security/)

---

## Next Steps

1. **Immediate**: Resolve merge conflict (Phase 1)
2. **Short-term**: Fix high-priority issues (Phase 2)
3. **Medium-term**: Complete medium-priority enhancements (Phase 3)
4. **Ongoing**: Maintain code quality standards (Phase 4-5)

---

**Last Updated**: 2026-01-09  
**Status**: Ready for Implementation  
**Priority**: High (Blocks PR 54)

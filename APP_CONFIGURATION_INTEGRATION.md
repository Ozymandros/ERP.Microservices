# 🔧 App Configuration Integration Guide

**Status:** Ready for Implementation  
**Date:** October 27, 2025  
**Target:** All 7 microservices + API Gateway

---

## 📋 Overview

This guide explains how to integrate **Azure App Configuration** into all microservices. App Configuration provides:

- ✅ **Centralized Configuration** - Single source of truth for all settings
- ✅ **Real-time Updates** - No service restart needed to update settings
- ✅ **Feature Flags** - Enable/disable features dynamically
- ✅ **Environment-Specific** - Different configs for dev/staging/prod
- ✅ **Secure References** - Key Vault secret linking without exposing secrets
- ✅ **Audit Trail** - Full history of configuration changes

---

## 🏗️ Infrastructure Configuration

### App Configuration Settings Created

The Bicep infrastructure creates the following settings in Azure App Configuration:

```
✅ Jwt:Issuer              → MyApp.Auth (non-sensitive)
✅ Jwt:Audience            → MyApp.All (non-sensitive)
✅ Jwt:SecretKey           → Reference to Key Vault (secure)
✅ Frontend:Origin         → https://yourdomain.com (non-sensitive)
✅ ASPNETCORE_ENVIRONMENT  → Production (non-sensitive)
✅ Redis:Connection        → Reference to Key Vault (secure)
✅ Sql:ConnectionStrings:*Db → References to Key Vault (secure)
```

### Services Configured

- ✅ Auth Service
- ✅ Billing Service
- ✅ Inventory Service
- ✅ Orders Service
- ✅ Purchasing Service
- ✅ Sales Service
- ✅ API Gateway

All services receive the **App Configuration connection string** via environment variable at deployment time.

---

## 💻 .NET Implementation

### Step 1: Add NuGet Package

Add Azure App Configuration provider to each service:

```bash
# From service project directory
dotnet add package Azure.Identity
dotnet add package Microsoft.Extensions.Configuration.AzureAppConfiguration
```

### Step 2: Update Program.cs

Add App Configuration to the configuration builder. Here's the recommended pattern:

```csharp
using Azure.Identity;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// 1. Add App Configuration Provider
// ============================================================================
// Reads settings from centralized Azure App Configuration service
// Falls back to local appsettings.json if not configured in cloud

var appConfigConnection = builder.Configuration.GetConnectionString("AppConfiguration");
if (!string.IsNullOrEmpty(appConfigConnection))
{
    builder.Configuration.AddAzureAppConfiguration(options =>
    {
        options
            .Connect(appConfigConnection)
            // Load settings with no label (shared settings)
            .Select(KeyFilter.Any, LabelFilter.Null)
            // Load settings labeled with current environment (environment-specific override)
            .Select(KeyFilter.Any, builder.Environment.EnvironmentName)
            // Remove service-specific prefix if using hierarchical keys
            // (e.g., "Auth:Jwt:Issuer" becomes "Jwt:Issuer")
            .TrimKeyPrefix("Auth:");
    });
}

// ============================================================================
// 2. Add Key Vault Provider
// ============================================================================
// App Configuration returns references to Key Vault secrets
// This provider resolves those references to actual values
// Works automatically with App Configuration references

var keyVaultUri = builder.Configuration.GetValue<string>("KeyVault:Uri");
if (!string.IsNullOrEmpty(keyVaultUri))
{
    var credential = new DefaultAzureCredential();
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        credential);
}

// ============================================================================
// 3. Configure Services with Centralized Settings
// ============================================================================
// Services now use configuration from App Configuration (with Key Vault resolution)

// JWT Configuration (reads from App Configuration)
builder.Services.Configure<JwtOptions>(options =>
{
    options.SecretKey = builder.Configuration["Jwt:SecretKey"];     // ← From App Config → Key Vault
    options.Issuer = builder.Configuration["Jwt:Issuer"];           // ← From App Config
    options.Audience = builder.Configuration["Jwt:Audience"];       // ← From App Config
    options.ExpirationMinutes = 60;
});

// Database Configuration (reads from App Configuration)
builder.Services.Configure<DatabaseOptions>(options =>
{
    // Service-specific: Each service reads its own database connection
    var connectionString = builder.Configuration["Sql:ConnectionStrings:AuthDb"];  // or BillingDb, etc.
    options.ConnectionString = connectionString ?? "";
});

// Redis Configuration (reads from App Configuration)
builder.Services.Configure<CacheOptions>(options =>
{
    options.ConnectionString = builder.Configuration["Redis:Connection"];  // ← From App Config → Key Vault
});

// CORS Configuration (reads from App Configuration)
builder.Services.AddCors(options =>
{
    var frontendOrigin = builder.Configuration["Frontend:Origin"];   // ← From App Config
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(frontendOrigin?.Split(';') ?? new[] { "http://localhost:3000" })
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ... rest of service configuration ...

var app = builder.Build();

// ============================================================================
// 4. Middleware Configuration
// ============================================================================
app.UseCors("AllowFrontend");

app.Run();
```

### Step 3: Configuration Models

Create strong-typed configuration classes:

```csharp
// Authentication/JwtOptions.cs
public class JwtOptions
{
    public string SecretKey { get; set; } = "";
    public string Issuer { get; set; } = "MyApp.Auth";
    public string Audience { get; set; } = "MyApp.All";
    public int ExpirationMinutes { get; set; } = 60;
}

// Database/DatabaseOptions.cs
public class DatabaseOptions
{
    public string ConnectionString { get; set; } = "";
    public int CommandTimeout { get; set; } = 30;
}

// Cache/CacheOptions.cs
public class CacheOptions
{
    public string ConnectionString { get; set; } = "";
    public int DatabaseNumber { get; set; } = 0;
}

// Frontend/FrontendOptions.cs
public class FrontendOptions
{
    public string Origin { get; set; } = "http://localhost:3000";
    public string ApiBaseUrl { get; set; } = "";
}
```

### Step 4: Inject Configuration into Services

Example of using configured settings in a service:

```csharp
using Microsoft.Extensions.Options;

public class AuthenticationService
{
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IOptions<JwtOptions> jwtOptions,
        ILogger<AuthenticationService> logger)
    {
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    public string GenerateToken(string userId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtOptions.SecretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            }),
            Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes),
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        _logger.LogInformation("Token generated for user {UserId}", userId);
        return tokenHandler.WriteToken(token);
    }
}
```

---

## 🔄 Environment Variables Set by Infrastructure

When deployed to Azure Container Apps, each service receives:

```bash
# Injected by Bicep infrastructure
AppConfiguration__ConnectionString = <connection-string>
Jwt__SecretKey = <reference resolved from Key Vault>
KeyVault__Uri = https://kv-xxxxx.vault.azure.net/

# Local development (appsettings.Development.json)
AppConfiguration__ConnectionString = ""  # Will be set when connecting to cloud
```

---

## 🧪 Local Development Setup

### Option 1: Using Local App Configuration (No Cloud)

Create `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "Jwt": {
    "SecretKey": "your-secret-key-32-chars-minimum-dont-commit",
    "Issuer": "MyApp.Auth",
    "Audience": "MyApp.All",
    "ExpirationMinutes": 60
  },
  "Sql": {
    "ConnectionStrings": {
      "AuthDb": "Server=localhost;Database=AuthDB;User Id=sa;Password=Your_Password123!;TrustServerCertificate=True;"
    }
  },
  "Redis": {
    "Connection": "localhost:6380,password=your-redis-password,ssl=False"
  },
  "Frontend": {
    "Origin": "http://localhost:3000"
  },
  "ASPNETCORE_ENVIRONMENT": "Development"
}
```

### Option 2: Using Azure App Configuration Locally

To test with real Azure App Configuration locally:

```csharp
// Program.cs
var appConfigConnection = builder.Configuration["AppConfiguration:ConnectionString"];
if (!string.IsNullOrEmpty(appConfigConnection))
{
    builder.Configuration.AddAzureAppConfiguration(options =>
    {
        options.Connect(appConfigConnection);
        options.Select(KeyFilter.Any, "Development");  // Use dev label
    });
}
```

Then set environment variable:

```bash
# PowerShell
$env:AppConfiguration__ConnectionString = "Endpoint=https://appconfig-xxxxx.azureconfig.io;Id=xxxxxx;Secret=xxxxxx"

# Or in .env file
AppConfiguration__ConnectionString="Endpoint=https://appconfig-xxxxx.azureconfig.io;Id=xxxxxx;Secret=xxxxxx"
```

---

## 🔐 Security Best Practices

### ✅ DO:

- ✅ Store sensitive values (JWT secret, passwords) in Key Vault only
- ✅ Reference Key Vault secrets from App Configuration
- ✅ Use Managed Identity in production (no connection strings in code)
- ✅ Use `@secure()` decorator on sensitive parameters
- ✅ Rotate secrets regularly
- ✅ Audit configuration changes in App Configuration
- ✅ Use environment-specific labels for different configs

### ❌ DON'T:

- ❌ Store secrets in `appsettings.json`
- ❌ Commit connection strings to git
- ❌ Expose App Configuration connection strings in frontend
- ❌ Use account keys directly (use Managed Identity instead)
- ❌ Store unencrypted secrets anywhere

---

## 🚀 Deployment Scenario

### Step 1: Deploy Infrastructure

```bash
azd deploy
```

This creates:
- ✅ Azure App Configuration instance
- ✅ Key Vault with all secrets
- ✅ 7 Container Apps with connection strings injected

### Step 2: Build Container Images

```bash
# From service directory
docker build -t authservice:latest -f Dockerfile .
az acr build -r <registry-name> -t auth-service:latest .

# Repeat for all services
```

### Step 3: Service Startup

When each Container App starts:

1. **Environment Loaded**: Container has `AppConfiguration__ConnectionString` injected
2. **App Configuration Connected**: `AddAzureAppConfiguration()` reads settings
3. **Key Vault Resolved**: App Configuration references are resolved to actual values
4. **Services Configured**: Dependency injection receives resolved settings
5. **Application Ready**: Service is ready to handle requests

### Step 4: Configuration Updates

To update configuration in production **without redeploying**:

```bash
# Update App Configuration value (non-sensitive)
az appconfig kv set \
  -n appconfig-xxxxx \
  --key "Jwt:Issuer" \
  --value "NewIssuer" \
  --label "Production"

# Services pick up the change automatically on next request
```

---

## 📊 Configuration Hierarchy

When multiple sources exist, resolution order:

```
1. App Configuration (cloud) + Key Vault references
   ↓ (if not found in cloud)
2. Environment Variables (injected by Azure)
   ↓ (if not found)
3. appsettings.json (local)
   ↓ (if not found)
4. appsettings.{Environment}.json (local override)
   ↓ (if not found)
5. Default values in code (fallback)
```

---

## 🔍 Troubleshooting

### Issue: "App Configuration connection string is empty"

**Solution:** Verify environment variable is set:

```bash
# Inside container
echo $AppConfiguration__ConnectionString

# Or check deployment
az containerapp env list-secrets -n <container-app-name> -g <resource-group>
```

### Issue: "Key Vault secret not found"

**Solution:** Verify App Configuration has access to Key Vault:

```bash
# Check Key Vault access policy
az keyvault show --name kv-xxxxx --query "properties.accessPolicies"

# Verify App Configuration managed identity is listed
```

### Issue: "Configuration not updating"

**Solution:** App Configuration doesn't auto-update in-process. Options:

1. **Restart container** (clears cache, reloads config)
2. **Implement IChangeTokenProvider** (for refresh notification)
3. **Use Feature Flags** (for feature toggles without restart)

Example with refresh:

```csharp
// Enable watch for changes
builder.Configuration.AddAzureAppConfiguration(options =>
{
    options
        .Connect(appConfigConnection)
        .Select(KeyFilter.Any, LabelFilter.Null)
        .ConfigureKeyVaultOptions(kvo =>
        {
            kvo.SetCredential(new DefaultAzureCredential());
        });
});

// Refresh specific keys
var config = app.Services.GetService<IConfiguration>();
if (config is IConfigurationRoot configRoot)
{
    configRoot.Reload();  // Force reload (expensive)
}
```

---

## 📚 Complete Program.cs Example

Here's a complete, production-ready `Program.cs`:

```csharp
using Azure.Identity;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// Configuration Sources
// ============================================================================

// 1. App Configuration (centralized, overrides local files)
var appConfigConnection = builder.Configuration.GetConnectionString("AppConfiguration");
if (!string.IsNullOrEmpty(appConfigConnection))
{
    builder.Configuration.AddAzureAppConfiguration(options =>
    {
        options
            .Connect(appConfigConnection)
            .Select(KeyFilter.Any, LabelFilter.Null)
            .Select(KeyFilter.Any, builder.Environment.EnvironmentName)
            .TrimKeyPrefix(builder.Configuration["ServiceName"] + ":");
    });
}

// 2. Key Vault (resolves App Configuration references)
var keyVaultUri = builder.Configuration.GetValue<string>("KeyVault:Uri");
if (!string.IsNullOrEmpty(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential());
}

// ============================================================================
// Service Registration
// ============================================================================

// Configuration options
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Sql"));
builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection("Redis"));

// HTTP services
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add your domain services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IDatabaseService, DatabaseService>();

// CORS
var frontendOrigin = builder.Configuration["Frontend:Origin"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(frontendOrigin?.Split(';') ?? new[] { "http://localhost:3000" })
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// ============================================================================
// Build and Configure Application
// ============================================================================

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
```

---

## 📋 Deployment Checklist

- [ ] All services have `Azure.Identity` NuGet package
- [ ] All services have `Microsoft.Extensions.Configuration.AzureAppConfiguration` NuGet package
- [ ] `Program.cs` updated in all services with App Configuration provider
- [ ] Configuration models created (JwtOptions, DatabaseOptions, etc.)
- [ ] Services injected with IOptions<T> for configuration
- [ ] CORS configured with centralized Frontend:Origin value
- [ ] Local development uses `appsettings.Development.json`
- [ ] Container images built and pushed to registry
- [ ] Infrastructure deployed with `azd deploy`
- [ ] App Configuration values verified in Azure Portal
- [ ] Key Vault secrets verified and accessible
- [ ] Services started and health check responding
- [ ] Configuration values accessible from running services

---

## ✅ Validation

Test that configuration is working:

```bash
# 1. Get running container
az containerapp exec -n auth-service -g rg-myapp-prod

# 2. Inside container, verify environment variables
echo $AppConfiguration__ConnectionString
echo $KeyVault__Uri

# 3. Check application logs for successful config load
az containerapp logs show -n auth-service -g rg-myapp-prod --follow
```

Expected in logs:
```
[Information] AppConfiguration provider added successfully
[Information] Connected to Azure Key Vault: https://kv-xxxxx.vault.azure.net/
[Information] Loaded configuration: Jwt:Issuer=MyApp.Auth, Frontend:Origin=https://yourdomain.com
```

---

## 🎯 Benefits Summary

| Aspect | Before | After |
|--------|--------|-------|
| **Configuration Location** | Scattered across services | Centralized in App Configuration |
| **Updating Settings** | Redeploy all services | Update in real-time (or with refresh) |
| **Secret Management** | Potentially in code | Secure in Key Vault via references |
| **Environment-Specific** | Manual file management | Labels per environment |
| **Audit Trail** | Limited | Full history in App Configuration |
| **Number of Configs** | 7 service-specific sets | 1 centralized source for all |

---

**This completes the App Configuration integration architecture.** 🎉

Each microservice now pulls its configuration from a single, secure, centralized source with full audit trail and real-time update capabilities.

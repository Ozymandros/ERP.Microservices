# Security & Identity Best Practices Guide

## 🔐 Overview

This guide documents the security architecture and best practices for the ERP Microservices infrastructure. All services must securely connect to dependencies using managed identities, RBAC, and Key Vault.

---

## 📋 Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Container Apps                           │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │Auth Svc  │  │Billing   │  │Inventory │  │Orders    │   │
│  │ (MI)     │  │ (MI)     │  │ (MI)     │  │ (MI)     │   │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘   │
└───────┼─────────────┼─────────────┼─────────────┼──────────┘
        │             │             │             │
        └─────────────┴─────────────┴─────────────┘
                      │
        ┌─────────────┴─────────────────────────┐
        │                                       │
    ┌───┴───────┐                    ┌──────────┴─────┐
    │ Key Vault │                    │ App Config     │
    │(Secrets)  │◄───────────────────┤ (Settings)     │
    │- JWT Key  │  Managed Identity  │- JWT Issuer    │
    │- Redis PW │  Access Policy     │- CORS Origin   │
    │- SQL Keys │  (principalId)     │- Env Config    │
    └───┬───────┘                    └────────────────┘
        │
        └────────────────┬──────────────────┐
                         │                  │
                    ┌────┴────┐      ┌─────┴──────┐
                    │ Redis   │      │ SQL Server │
                    │(Secrets)│      │(Secrets)   │
                    └─────────┘      └────────────┘
```

---

## 🔑 Core Concepts

### **1. Managed Identities**

Each service has a **system-assigned managed identity** that enables:

```bicep
identity: {
  type: 'SystemAssigned'  // Azure-managed identity
}
```

**Benefits:**
- ✅ No password management
- ✅ Automatic token rotation
- ✅ RBAC-based access control
- ✅ Audit trail in Azure Activity Log

---

### **2. RBAC Policies (Role-Based Access Control)**

Services access resources through **role assignments** with minimum required permissions:

```bicep
resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(resourceGroup().id, principalId, 'Key Vault Secrets User')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: principalId  // ← Managed identity principal ID
    principalType: 'ServicePrincipal'
  }
}
```

**Principle: Least Privilege**
- Each service gets **only** the permissions it needs
- No wildcard permissions
- Regular audit of access

---

### **3. Key Vault for Secrets**

**Sensitive values stored in Key Vault:**

| Secret | Purpose | Used By |
|--------|---------|---------|
| `jwt-secret-key` | JWT signing | All services (via App Config) |
| `redis-password` | Redis authentication | All services |
| `sql-connection-strings` | SQL DB access | Services that need DB |

**Referenced via App Configuration:**

```bicep
// In App Configuration
resource jwtSecretReference 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  name: 'Jwt:SecretKey'
  properties: {
    value: '@Microsoft.KeyVault(SecretUri=https://keyvault-name.vault.azure.net/secrets/jwt-secret-key/)'
    contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
  }
}
```

---

## 🔌 Connection Security

### **Required Connections for Each Service**

#### **Auth Service**
```
Auth → Key Vault (read JWT secret)
Auth → App Configuration (read settings)
Auth → SQL Database (user auth data)
```

#### **Inventory Service**
```
Inventory → Key Vault (read secrets)
Inventory → App Configuration (read settings)
Inventory → SQL Database (inventory data)
Inventory → Redis (cache)
```

#### **Orders Service**
```
Orders → Key Vault (read secrets)
Orders → App Configuration (read settings)
Orders → SQL Database (orders data)
Orders → Redis (cache)
Orders → Auth Service (validate JWT)
```

---

## 🛠️ Implementation Checklist

### **Per Service**

- [ ] **Managed Identity**
  - [ ] System-assigned identity created
  - [ ] Principal ID captured
  - [ ] Identity enabled in container app

- [ ] **Key Vault Access**
  - [ ] Role assignment: "Key Vault Secrets User"
  - [ ] Scope: Specific Key Vault
  - [ ] Principal: Service's managed identity

- [ ] **App Configuration Access**
  - [ ] Role assignment: "App Configuration Data Reader"
  - [ ] Scope: App Configuration store
  - [ ] Principal: Service's managed identity

- [ ] **Database Access**
  - [ ] Role assignment: "SQL DB Contributor" or "SQL DB Reader"
  - [ ] Scope: Specific database
  - [ ] Principal: Service's managed identity

- [ ] **Redis Access**
  - [ ] Connection string with password in Key Vault
  - [ ] Password set during Redis creation
  - [ ] Referenced from App Configuration

---

## 📝 Bicep Implementation Patterns

### **Pattern 1: Create Managed Identity**

```bicep
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${serviceName}-identity'
  location: location
  tags: tags
}

output principalId string = managedIdentity.properties.principalId
output clientId string = managedIdentity.properties.clientId
output id string = managedIdentity.id
```

### **Pattern 2: Assign to Container App**

```bicep
resource containerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: name
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityResourceId}': {}
    }
  }
  properties: {
    // ... rest of configuration
  }
}
```

### **Pattern 3: Grant Key Vault Access**

```bicep
@description('Managed Identity Principal ID for role assignment')
param managedIdentityPrincipalId string

resource keyVaultAccessPolicy 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(resourceGroup().id, managedIdentityPrincipalId, 'Key Vault Secrets User')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}
```

### **Pattern 4: Grant App Configuration Access**

```bicep
resource appConfigAccessPolicy 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: appConfig
  name: guid(resourceGroup().id, managedIdentityPrincipalId, 'App Configuration Data Reader')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '516239f1-63e1-4108-9b7f-3ee94da9555c')
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}
```

### **Pattern 5: Grant Database Access**

```bicep
resource sqlRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: sqlDatabase
  name: guid(resourceGroup().id, managedIdentityPrincipalId, 'SQL Database Contributor')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '9b7fa17d-e63a-4465-b752-700b8ab5191a')
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}
```

---

## 🔒 Azure Role Definitions

### **Required Roles**

| Role Name | ID | Purpose | Least Privilege |
|-----------|----|---------|----|
| Key Vault Secrets User | `4633458b-17de-408a-b874-0445c86b69e6` | Read secrets | ✅ Yes |
| Key Vault Certificates Officer | `a4417e6f-fecd-4d62-9efc-8de8ce7271d0` | Manage certs | ⚠️ Only if needed |
| App Configuration Data Reader | `516239f1-63e1-4108-9b7f-3ee94da9555c` | Read settings | ✅ Yes |
| SQL DB Contributor | `9b7fa17d-e63a-4465-b752-700b8ab5191a` | Full DB access | ⚠️ Use Reader if read-only |
| SQL DB Data Reader | `0564a8ad-ffdc-4c17-8920-9a551b01b06f` | Read-only DB access | ✅ Yes |
| Redis Cache Data Contributor | `08a6b313-0511-4330-a7f3-07832e1f86be` | Full Redis access | ⚠️ Use Reader if read-only |

---

## 🧪 Testing & Validation

### **Post-Deployment Verification**

```powershell
# 1. Verify managed identity
az identity show --resource-group $rg --name auth-service-identity

# 2. Verify role assignments
az role assignment list --resource-group $rg --scope "/subscriptions/$sub" --output table

# 3. Test Key Vault access
az keyvault secret show --vault-name $kvName --name jwt-secret-key

# 4. Test App Configuration access
az appconfig kv list --name $acName

# 5. Test service connectivity
kubectl logs -n containers deployment/auth-service  # Check for connection errors
```

### **Common Issues**

| Issue | Cause | Fix |
|-------|-------|-----|
| 403 Forbidden to Key Vault | Missing role assignment | Add "Key Vault Secrets User" role |
| 403 Forbidden to App Config | Missing role assignment | Add "App Configuration Data Reader" role |
| Connection timeout to Redis | Network/security group issue | Check NSG rules, Redis firewall |
| SQL authentication failed | Missing DB user for identity | Create SQL user for managed identity |
| Secrets not resolving | Key Vault reference format incorrect | Verify format: `@Microsoft.KeyVault(SecretUri=...)` |

---

## 📚 Configuration Examples

### **C# Service Configuration**

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// 1. Add App Configuration
var credentials = new DefaultAzureCredential();
var configClient = new ConfigurationClient(
    new Uri(builder.Configuration["AppConfiguration:Endpoint"]),
    credentials
);

builder.Configuration.AddAzureAppConfiguration(options =>
{
    options.Client(configClient)
        .Select(KeyFilter.Any, LabelFilter.Null)
        .Select(KeyFilter.Any, builder.Environment.EnvironmentName);
});

// 2. Add Key Vault
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    credentials
);

// 3. Configure services
builder.Services.AddKeyVaultClient(credentials);
builder.Services.AddRedisCache(configuration);
builder.Services.AddSqlDatabase(configuration);

var app = builder.Build();
app.Run();
```

---

## 🚀 Deployment Steps

1. **Create Key Vault**
   ```bicep
   module keyVault 'core/security/keyvault.bicep' = {
     params: {
       location: location
       enableSecrets: true
     }
   }
   ```

2. **Create App Configuration**
   ```bicep
   module appConfig 'core/configuration/app-configuration.bicep' = {
     params: {
       location: location
       keyVaultName: keyVault.outputs.name
       jwtSecret: keyVault.outputs.jwtSecretId
     }
   }
   ```

3. **Create Services with Identities**
   ```bicep
   module authService 'services/auth-service.bicep' = {
     params: {
       managedIdentityPrincipalId: managedIdentity.outputs.principalId
       keyVaultName: keyVault.outputs.name
       appConfigName: appConfig.outputs.name
     }
   }
   ```

4. **Grant RBAC Permissions**
   ```bicep
   // Automatically created via RBAC module
   ```

5. **Deploy & Verify**
   ```bash
   ./validate-bicep.ps1 -ShowDetails
   ./validate-bicep--what-if.ps1 -Location "eastus"
   azd deploy
   ```

---

## ✅ Compliance & Best Practices

- ✅ **Zero Secrets in Code** - All secrets in Key Vault
- ✅ **Managed Identities** - No username/password auth
- ✅ **RBAC Least Privilege** - Minimal required permissions
- ✅ **Audit Logging** - All access logged in Activity Log
- ✅ **Encryption in Transit** - TLS/SSL for all connections
- ✅ **Encryption at Rest** - Key Vault encryption
- ✅ **Network Security** - NSG rules, firewall rules
- ✅ **Secrets Rotation** - Periodic key rotation
- ✅ **Monitoring & Alerts** - Application Insights tracking

---

## 📞 Support & Troubleshooting

For connection issues:
1. Check managed identity is created
2. Verify RBAC role assignment
3. Check network connectivity (NSG, firewall)
4. Review Application Insights logs
5. Check Azure Activity Log for authorization failures

For security concerns:
1. Review Key Vault access policies
2. Audit role assignments
3. Check App Configuration RBAC
4. Verify secrets are not logged
5. Review network configurations

# 🔒 Security & Compliance

**Authentication, Authorization, Secrets & Security Best Practices**  
Last Updated: October 27, 2025

---

## 📍 Overview

This category covers all security aspects including authentication mechanisms, authorization models, secrets management, and security best practices. Designed for security engineers, architects, and all developers.

---

## 📚 Documents in This Category

### [SECURITY_OVERVIEW.md](SECURITY_OVERVIEW.md)
**Complete security model and architecture**
- Security zones and boundaries
- Authentication & authorization flows
- Secrets management strategy
- Data protection approach
- Compliance considerations

### [RBAC_FIX_SUMMARY.md](RBAC_FIX_SUMMARY.md)
**RBAC Role Definition Fix for Key Vault**
- Issue identification and resolution
- Azure role definition corrections
- Deployment error prevention

### [AUTHENTICATION.md](AUTHENTICATION.md)
**JWT authentication implementation**
- Token structure and claims
- Token generation and validation
- Token expiration and refresh
- Multi-tenancy in tokens
- Development vs production tokens

### [AUTHORIZATION.md](AUTHORIZATION.md)
**Role-based access control (RBAC)**
- Permission model
- Role definitions
- Claims-based authorization
- Service-to-service authorization
- Resource-level permissions

### [SECRETS_MANAGEMENT.md](SECRETS_MANAGEMENT.md)
**Handling secrets securely**
- Local development secrets
- Azure Key Vault integration
- Secrets rotation
- Emergency key revocation
- Audit logging of secret access

### [BEST_PRACTICES.md](BEST_PRACTICES.md)
**Security guidelines for all developers**
- Secure coding practices
- Common vulnerabilities and prevention
- Logging without exposing secrets
- Security testing
- Incident response procedures

---

## 🎯 Security Layers

```
┌─────────────────────────────────────┐
│  External Request (HTTPS)           │
├─────────────────────────────────────┤
│  API Gateway (Ocelot)               │  Layer 1: API Security
│  - JWT Validation                   │
│  - Rate Limiting                    │
│  - Request Logging                  │
├─────────────────────────────────────┤
│  Service-to-Service Communication   │  Layer 2: Internal Security
│  - DAPR mTLS (Sentry)              │
│  - Service-to-service auth         │
│  - Encrypted channels              │
├─────────────────────────────────────┤
│  Database Access                    │  Layer 3: Data Security
│  - Credential per service           │
│  - Encrypted connections            │
│  - Secrets in Key Vault             │
├─────────────────────────────────────┤
│  Application Level                  │  Layer 4: Application Security
│  - Authorization checks             │
│  - Input validation                 │
│  - Secure error handling            │
└─────────────────────────────────────┘
```

---

## 📊 Authentication Methods

| Method | Use Case | Environment |
|--------|----------|-------------|
| **JWT Bearer Token** | Service-to-service, API clients | All |
| **Azure AD** | User authentication | Production |
| **Managed Identity** | Service-to-resource auth | Production |
| **Connection String** | Database access | All |

---

## 🔐 Key Components

### Secrets Storage

**Local Development:**
- appsettings.json files
- .env files (non-production)
- User Secrets for sensitive local data

**Production:**
- Azure Key Vault (primary)
- Managed Identities (automatic auth)
- Secrets rotation every 90 days
- Audit logging on all access

### Token Management

- Tokens signed with private key
- Tokens validated with public key
- Expiration: 1 hour (default)
- Refresh tokens: 7 days
- Claims include user ID, roles, scopes

### Authorization

- Role-Based Access Control (RBAC)
- Claims-based policies
- Service account for inter-service calls
- Admin role for operations

---

## 🔄 Reading Order

1. Start with [SECURITY_OVERVIEW.md](SECURITY_OVERVIEW.md) for the big picture
2. Read [AUTHENTICATION.md](AUTHENTICATION.md) to understand tokens
3. Study [AUTHORIZATION.md](AUTHORIZATION.md) for access control
4. Learn [SECRETS_MANAGEMENT.md](SECRETS_MANAGEMENT.md) for secrets
5. Reference [BEST_PRACTICES.md](BEST_PRACTICES.md) regularly

---

## 📚 Related Categories

- **API Gateway:** [Gateway Documentation](../api-gateway/README.md) - Where auth happens
- **Infrastructure:** [Infrastructure Guide](../infrastructure/README.md) - Key Vault setup
- **Operations:** [Operations Guide](../operations/README.md) - Monitoring security
- **Development:** [Development Setup](../development/DEVELOPMENT_SETUP.md) - Implementing auth

---

## 🛡️ Security Checklist

### Before Deployment

- [ ] All secrets in Azure Key Vault (not in code)
- [ ] JWT signing key securely generated
- [ ] Token expiration set appropriately
- [ ] HTTPS enabled on all endpoints
- [ ] Rate limiting configured
- [ ] Authentication enabled on all protected routes
- [ ] Authorization policies defined
- [ ] Secrets rotation schedule established
- [ ] Audit logging enabled
- [ ] Security testing completed

### Regular Review

- [ ] Check for leaked secrets
- [ ] Rotate secrets per schedule
- [ ] Review access logs for anomalies
- [ ] Update security policies as needed
- [ ] Patch dependencies regularly
- [ ] Review authentication events

---

## 💡 Common Scenarios

### Scenario 1: User Login
```
User submits credentials
  ↓
Auth Service validates
  ↓
JWT token generated with user claims
  ↓
Token returned to client
  ↓
Client includes token in Authorization header
  ↓
Gateway validates token
  ↓
Request forwarded to service
```

### Scenario 2: Service-to-Service Call
```
Service A needs to call Service B
  ↓
Uses DAPR service invocation
  ↓
DAPR Sentry provides mTLS certificates
  ↓
Connection encrypted with mutual authentication
  ↓
Service B verifies caller identity
  ↓
Request processed
```

### Scenario 3: Secret Access
```
Service needs database password
  ↓
Uses Managed Identity to authenticate
  ↓
Calls Azure Key Vault
  ↓
Key Vault verifies Managed Identity
  ↓
Secret returned (cached locally)
  ↓
Access logged in audit trail
```

---

## 🆘 Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| 401 Unauthorized | Invalid/expired token | [AUTHENTICATION.md](AUTHENTICATION.md) |
| 403 Forbidden | Insufficient permissions | [AUTHORIZATION.md](AUTHORIZATION.md) |
| Service-to-service failing | mTLS issue | [BEST_PRACTICES.md](BEST_PRACTICES.md) |
| Secret not found | Key Vault issue | [SECRETS_MANAGEMENT.md](SECRETS_MANAGEMENT.md) |
| Token claims missing | Token generation issue | [AUTHENTICATION.md](AUTHENTICATION.md) |

---

## 📋 Compliance Considerations

| Requirement | Approach |
|-------------|----------|
| Data encryption at rest | Azure SQL with TDE |
| Data encryption in transit | HTTPS/TLS |
| Access logging | Application Insights |
| Audit trail | Azure Audit Logs |
| Secret rotation | Automated in Key Vault |
| Least privilege | RBAC with minimal scopes |
| Network isolation | Private endpoints |

---

## ✅ Verification Checklist

When implementing security:

- [ ] Authentication enabled on protected routes
- [ ] JWT tokens validated correctly
- [ ] Authorization policies applied
- [ ] Secrets not logged or exposed
- [ ] HTTPS enforced (production)
- [ ] Rate limiting prevents abuse
- [ ] Audit logging enabled
- [ ] Secrets rotated on schedule
- [ ] Emergency procedures documented

---

## 📞 Next Steps

- **Setting up auth?** → [AUTHENTICATION.md](AUTHENTICATION.md)
- **Configuring permissions?** → [AUTHORIZATION.md](AUTHORIZATION.md)
- **Managing secrets?** → [SECRETS_MANAGEMENT.md](SECRETS_MANAGEMENT.md)
- **Need best practices?** → [BEST_PRACTICES.md](BEST_PRACTICES.md)
- **Deploying securely?** → [Infrastructure Guide](../infrastructure/README.md)

---

## 🔗 Full Document Map

```
security/
├── README.md (this file)
├── SECURITY_OVERVIEW.md
├── RBAC_FIX_SUMMARY.md
├── AUTHENTICATION.md
├── AUTHORIZATION.md
├── SECRETS_MANAGEMENT.md
└── BEST_PRACTICES.md
```

---

**Last Updated:** October 27, 2025  
**Category Status:** ✅ Complete  
**Documents:** 5 files  
**Security Layers:** 4 levels of protection

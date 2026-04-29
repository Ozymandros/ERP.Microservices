# 🌐 API Gateway & Request Routing

**Ocelot API Gateway Configuration & Operations**  
Last Updated: October 27, 2025

---

## 📍 Overview

The API Gateway (Ocelot) is the single entry point for all external requests. It handles routing, authentication, rate limiting, and resilience patterns. This category explains the complete gateway configuration and operations.

---

## 📚 Documents in This Category

### [OCELOT_OVERVIEW.md](OCELOT_OVERVIEW.md)
**Ocelot API Gateway concepts and architecture**
- Gateway responsibilities
- Route configuration
- Built-in security features
- Performance optimization
- Extensibility points

### [OCELOT_CONFIGURATION.md](OCELOT_CONFIGURATION.md)
**Complete configuration guide**
- Local development configuration (`ocelot.json`)
- Production configuration (`ocelot.Production.json`)
- Route definitions for all 6 services
- Rate limiting setup
- Circuit breaker configuration

### [AUTHENTICATION.md](AUTHENTICATION.md)
**JWT authentication and claims handling**
- Token validation flow
- Claims extraction and propagation
- Authorization policies
- Token refresh strategies
- Scopes and permissions

### [RATE_LIMITING.md](RATE_LIMITING.md)
**Rate limiting and quality of service**
- Rate limit configuration
- QoS (quality of service) settings
- Circuit breaker thresholds
- Timeout configuration
- Request/response pipelines

### [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
**Common gateway issues and solutions**
- 401 Unauthorized errors
- 429 Rate Limited responses
- 503 Service Unavailable
- Route not found
- Authentication failures

---

## 🎯 Quick Start

### Test the Gateway

```bash
# Check gateway health
curl http://localhost:8000/health

# Make authenticated request
curl -H "Authorization: Bearer YOUR_TOKEN" \
     http://localhost:8000/api/inventory

# Gateway should respond or forward to service
```

### Local vs Production

| Aspect | Local | Production |
|--------|-------|-----------|
| **Config File** | `ocelot.json` | `ocelot.Production.json` |
| **Port** | 8000 | 443 (HTTPS) |
| **Rate Limit** | 100/min | 1000/min |
| **Auth** | JWT Bearer | Azure AD + JWT |
| **Timeouts** | 10s | 30s |
| **Circuit Break** | 3 failures | 5 failures |

---

## 🔄 Request Flow

```
External Client
    ↓
Gateway (8000)
    ↓ [Authentication Check]
    ↓ [Rate Limit Check]
    ↓ [Route Match]
    ↓
Downstream Service (6001-6008)
    ↓
Database/Cache
    ↓
Response back through Gateway
    ↓
External Client
```

---

## 📊 Routed Services

| Route | Service | Port | Purpose |
|-------|---------|------|---------|
| `/api/auth` | Auth Service | 6001 | Authentication |
| `/api/billing` | Billing Service | 6002 | Billing & Invoicing |
| `/api/inventory` | Inventory Service | 6004 | Inventory Management |
| `/api/orders` | Orders Service | 6005 | Order Processing |
| `/api/purchasing` | Purchasing Service | 6006 | Procurement |
| `/api/sales` | Sales Service | 6007 | Sales Management |
| `/api/crm` | CRM Service | 6003 | CRM & Leads |
| `/api/sk` | SemanticKernel Service | 6008 | AI Semantic Kernel |
| `/health` | Gateway Health | - | Status check |

---

## 🔍 Common Tasks

| Task | How | See Also |
|------|-----|----------|
| Add a new route | Edit `ocelot.json` | [OCELOT_CONFIGURATION.md](OCELOT_CONFIGURATION.md) |
| Change rate limit | Edit rate limit settings | [RATE_LIMITING.md](RATE_LIMITING.md) |
| Debug auth failure | Check JWT token | [AUTHENTICATION.md](AUTHENTICATION.md) |
| Fix 503 error | Check circuit breaker | [TROUBLESHOOTING.md](TROUBLESHOOTING.md) |
| Test with curl | Add Auth header | [Testing Guide](#testing) |
| Monitor gateway | Check health endpoint | [OCELOT_OVERVIEW.md](OCELOT_OVERVIEW.md) |

---

## 🔐 Security Features

- **JWT Bearer Authentication** - Token validation on all routes
- **Rate Limiting** - Protect against abuse (100-1000 req/min)
- **Circuit Breaker** - Fail gracefully when service down
- **Request Tracing** - X-Request-ID for debugging
- **Claims Propagation** - Pass identity to downstream services
- **HTTPS** - TLS in production

---

## 💡 Key Concepts

### Authentication
- All requests must include valid JWT token
- Token verified before routing
- Claims extracted and added to request headers
- Tokens expire; refresh required

### Rate Limiting
- Prevents abuse and ensures fair usage
- Local: 100 requests/minute per client
- Production: 1000 requests/minute per client
- Returns 429 when exceeded

### Circuit Breaker
- Service unavailable? Circuit breaks
- Stops sending requests temporarily
- Local: 3 failures triggers break
- Production: 5 failures triggers break
- Returns 503 Service Unavailable

### Health Checks
- Gateway itself responds to `/health`
- Individual service health endpoints
- Used for monitoring and debugging

---

## 📚 Related Categories

- **Security:** [Security Documentation](../security/README.md) - Authentication deep dive
- **Development:** [Development Setup](../development/DEVELOPMENT_SETUP.md) - Testing the gateway
- **Operations:** [Monitoring Guide](../operations/MONITORING.md) - Monitor gateway performance
- **Reference:** [API Endpoints](../reference/API_ENDPOINTS.md) - All endpoints

---

## 🔄 Reading Order

1. Start with [OCELOT_OVERVIEW.md](OCELOT_OVERVIEW.md) to understand concepts
2. Review [OCELOT_CONFIGURATION.md](OCELOT_CONFIGURATION.md) for setup
3. Study [AUTHENTICATION.md](AUTHENTICATION.md) for security
4. Learn [RATE_LIMITING.md](RATE_LIMITING.md) for resilience
5. Bookmark [TROUBLESHOOTING.md](TROUBLESHOOTING.md) for issues

---

## 🆘 Troubleshooting Quick Links

**Problem** | **Solution**
---|---
Getting 401 Unauthorized | [AUTHENTICATION.md](AUTHENTICATION.md)
Getting 429 Rate Limited | [RATE_LIMITING.md](RATE_LIMITING.md)
Getting 503 Service Down | [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
Route not found | [OCELOT_CONFIGURATION.md](OCELOT_CONFIGURATION.md)
Token expired | [AUTHENTICATION.md](AUTHENTICATION.md)

---

## 🧪 Testing Examples

### Test with curl

```bash
# Get token
TOKEN=$(curl -s -X POST http://localhost:6001/auth/token \
  -H "Content-Type: application/json" \
  -d '{"username":"test","password":"test"}' \
  | jq -r '.token')

# Use token to call service
curl -H "Authorization: Bearer $TOKEN" \
     http://localhost:8000/api/inventory/items

# Should see inventory items
```

### Test rate limiting

```bash
# Rapid-fire requests
for i in {1..10}; do
  curl -H "Authorization: Bearer $TOKEN" \
       http://localhost:8000/api/inventory
done
# After ~100 requests, should get 429
```

---

## ✅ Verification Checklist

When deploying gateway:

- [ ] Gateway starts without errors
- [ ] Health check responds: `curl http://localhost:8000/health`
- [ ] Routes configured for all 6 services
- [ ] JWT authentication enabled
- [ ] Rate limiting configured per environment
- [ ] Circuit breaker thresholds set
- [ ] Health check endpoints configured
- [ ] HTTPS enabled (production only)

---

## 📊 Configuration Status

| Feature | Local | Production | Status |
|---------|-------|-----------|--------|
| Routes | All 6 services | All 6 services | ✅ |
| Authentication | JWT Bearer | JWT Bearer + Azure AD | ✅ |
| Rate Limiting | 100/min | 1000/min | ✅ |
| Circuit Breaker | 3 failures | 5 failures | ✅ |
| Health Checks | Enabled | Enabled | ✅ |
| HTTPS | Optional | Required | ✅ |

---

## 📞 Next Steps

- **First time?** → [OCELOT_OVERVIEW.md](OCELOT_OVERVIEW.md)
- **Setup locally?** → [OCELOT_CONFIGURATION.md](OCELOT_CONFIGURATION.md)
- **Need authentication?** → [AUTHENTICATION.md](AUTHENTICATION.md)
- **Performance tuning?** → [RATE_LIMITING.md](RATE_LIMITING.md)
- **Troubleshooting?** → [TROUBLESHOOTING.md](TROUBLESHOOTING.md)

---

## 🔗 Full Document Map

```
api-gateway/
├── README.md (this file)
├── OCELOT_OVERVIEW.md
├── OCELOT_CONFIGURATION.md
├── AUTHENTICATION.md
├── RATE_LIMITING.md
└── TROUBLESHOOTING.md
```

---

**Last Updated:** October 27, 2025  
**Category Status:** ✅ Complete  
**Documents:** 5 files  
**Gateway Endpoints:** 7 total (1 health + 6 services)

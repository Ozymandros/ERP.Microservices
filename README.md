# ERP Microservices

A cloud-native ERP system built with .NET 10, Aspire, Dapr, and deployed to Azure Container Apps.

## 🚀 Features

- **Microservices Architecture**: 6 independent services (Auth, Billing, Inventory, Orders, Purchasing, Sales)
- **API Gateway**: YARP-based reverse proxy with external HTTPS ingress
- **Dapr Integration**: Service invocation, pub/sub, and state management
- **Azure Container Apps**: Production-ready deployment with auto-scaling
- **Local Development**: Full stack runs locally with Aspire and Docker
- **CI/CD Ready**: GitHub Actions workflow included

## 📋 Architecture

### Services

| Service | Responsibility | Database |
|---------|---------------|----------|
| **Auth** | Authentication & Authorization | AuthDB |
| **Billing** | Invoice and billing management | BillingDB |
| **Inventory** | Stock and warehouse management | InventoryDB |
| **Orders** | Order processing | OrderDB |
| **Purchasing** | Procurement management | PurchasingDB |
| **Sales** | Sales operations | SalesDB |

### Infrastructure

- **Gateway**: External HTTPS endpoint, routes to internal services
- **Azure SQL**: One database per microservice
- **Azure Redis**: Shared distributed cache
- **Dapr**: Service mesh for microservices communication
- **Container Apps**: Managed Kubernetes-based hosting

## 🏃 Quick Start

### Local Development

```bash
# Prerequisites: .NET 10 SDK, Docker
cd AppHost
dotnet run
```

The Aspire dashboard will open automatically showing all services.

### Deploy to Azure

```bash
# Prerequisites: Azure Developer CLI (azd)
azd up
```

See [QUICKSTART.md](./docs/guides/QUICKSTART.md) for detailed 5-minute deployment guide.

## 📚 Documentation

Browse the complete documentation site at **[https://ozymandros.github.io/ERP.Microservices/](https://ozymandros.github.io/ERP.Microservices/)** (generated with DocFX)

### Quick Links

- **[Quick Start Guide](./docs/guides/QUICKSTART.md)** - Get started in 5 minutes
- **[Deployment Guide](./docs/deployment/DEPLOYMENT.md)** - Comprehensive deployment documentation
- **[Architecture Guide](./docs/architecture/ARCHITECTURE_DOCUMENTATION.md)** - System architecture and design
- **[API Reference](https://ozymandros.github.io/ERP.Microservices/api/)** - Complete API documentation

## 🛠️ Technology Stack

- **.NET 10**: Latest .NET framework
- **Aspire**: Cloud-native orchestration for local development
- **Dapr**: Distributed application runtime
- **Azure Container Apps**: Managed container hosting
- **Azure SQL Database**: Relational database
- **Azure Cache for Redis**: Distributed caching
- **Entity Framework Core**: ORM
- **Ocelot**: API Gateway
- **JWT**: Authentication

## 🏗️ Project Structure

```
ERP.Microservices/
├── AppHost/                      # Aspire orchestration
├── ErpApiGateway/               # YARP API Gateway
├── MyApp.Auth/                  # Auth microservice
├── MyApp.Billing/               # Billing microservice
├── MyApp.Inventory/             # Inventory microservice
├── MyApp.Orders/                # Orders microservice
├── MyApp.Purchasing/            # Purchasing microservice
├── MyApp.Sales/                 # Sales microservice
├── MyApp.Shared/                # Shared libraries
├── infra/                       # Azure infrastructure (Bicep)
│   ├── core/                    # Reusable Bicep modules
│   ├── main.bicep              # Main infrastructure definition
│   └── main.parameters.json    # Parameters template
├── .github/workflows/           # CI/CD pipelines
├── docs/                        # Documentation
├── azure.yaml                   # Azure Developer CLI config
└── README.md                    # This file
```

Each microservice follows Clean Architecture:
```
MyApp.[Service]/
├── MyApp.[Service].API/         # Web API layer
├── MyApp.[Service].Application/ # Application logic
├── MyApp.[Service].Domain/      # Domain entities
└── MyApp.[Service].Infrastructure/ # Data access
```

## 🔧 Development

### Prerequisites

- .NET 10.0 SDK
- Docker Desktop
- Visual Studio 2022 / VS Code / Rider
- Azure Developer CLI (for deployment)

### Build

```bash
# Restore dependencies
dotnet restore

# Build all projects
dotnet build

# Run tests
dotnet test
```

### Run Locally

```bash
# Using Aspire (recommended)
cd AppHost
dotnet run

# Or run individual services
cd MyApp.Auth/MyApp.Auth.API
dotnet run
```

### Environment Variables

Local development uses `appsettings.Development.json` in AppHost:

```json
{
  "Jwt": {
    "SecretKey": "your-secret-key",
    "Issuer": "MyApp.Auth",
    "Audience": "MyApp.All"
  },
  "Parameters": {
    "FrontendOrigin": "http://localhost:3000"
  }
}
```

Production uses Azure Container Apps secrets and environment variables.

## 🚢 Deployment

### Azure Container Apps (Recommended)

```bash
# One command deployment
azd up
```

This deploys:
- ✅ All microservices with Dapr sidecars
- ✅ API Gateway with external HTTPS
- ✅ Azure SQL with 6 databases
- ✅ Azure Cache for Redis
- ✅ Auto-scaling and health checks

See [DEPLOYMENT.md](./docs/deployment/DEPLOYMENT.md) for details.

### GitHub Actions (CI/CD)

Automated deployment on push to `main`:

1. Configure Azure credentials (OIDC recommended)
2. Set repository secrets
3. Push to main branch

Workflow: `.github/workflows/azure-deploy.yml`

## 🔐 Security

- **JWT Authentication**: Bearer token authentication
- **Role-Based Access Control**: Fine-grained permissions
- **Azure Managed Identities**: No credentials in code
- **Secrets Management**: Azure Container Apps secrets
- **HTTPS Only**: TLS termination at gateway
- **CORS**: Configurable allowed origins

## 📊 Monitoring

- **Application Insights**: Performance monitoring (coming soon)
- **Log Analytics**: Centralized logging
- **Health Checks**: Liveness and readiness probes
- **Dapr Dashboard**: Service mesh visibility

## 🤝 Contributing

Contributions welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## 📄 License

This project is licensed under the MIT License.

## 🔗 Links

- [Azure Container Apps Docs](https://learn.microsoft.com/azure/container-apps/)
- [.NET Aspire Docs](https://learn.microsoft.com/dotnet/aspire/)
- [Dapr Docs](https://docs.dapr.io/)

## 💡 Tips

- Use `azd down` to delete resources when not in use
- Check logs with `az containerapp logs show`
- Monitor costs in Azure Portal
- Scale services independently as needed

## 🆘 Support

- [Report Issues](https://github.com/Ozymandros/ERP.Microservices/issues)
- [Deployment Guide](./docs/deployment/DEPLOYMENT.md)
- [Quick Start](./docs/guides/QUICKSTART.md)

# ERP Microservices

A modern, cloud-native ERP system built with .NET microservices and Dapr, featuring a comprehensive Next.js frontend.

## Architecture

This project uses a microservices architecture with the following components:

### Backend Services
- **Auth Service**: Authentication, authorization, user/role/permission management
- **Sales Service**: Sales order management
- **Inventory Service**: Inventory and stock management
- **Billing Service**: Billing and invoicing
- **Orders Service**: Order processing
- **Purchasing Service**: Purchase order management
- **Notification Service**: Notifications and alerts

### Frontend
- **Next.js Application**: Modern React-based admin interface with TypeScript
  - Located in `/frontend` directory
  - See [Frontend README](./frontend/README.md) for detailed documentation

### Infrastructure
- **Dapr**: Distributed application runtime for service-to-service communication
- **AppHost**: .NET Aspire orchestration for local development

## Quick Start

### Prerequisites
- .NET 8.0 SDK or later
- Node.js 20+ and npm
- Docker Desktop (for Dapr and dependencies)

### Running the Frontend

1. Navigate to the frontend directory:
```bash
cd frontend
```

2. Install dependencies:
```bash
npm install
```

3. Configure environment variables:
```bash
cp .env.example .env.local
```

4. Run the development server:
```bash
npm run dev
```

Visit http://localhost:3000 to access the admin interface.

See the [Frontend README](./frontend/README.md) for more details.

### Running the Backend Services

Refer to individual service directories for specific setup instructions.

## Frontend Features

The Next.js frontend provides:

- **Authentication & Authorization**: JWT-based with refresh tokens
- **User Management**: Full CRUD operations with role assignment
- **Role Management**: Create and manage roles with permission assignment
- **Permission Management**: Fine-grained access control
- **Dual API Client**: Switch between Axios (direct) and Dapr HTTP modes
- **Responsive Design**: Modern UI with TailwindCSS
- **Type Safety**: Full TypeScript support with auto-generated types from OpenAPI

## Project Structure

```
ERP.Microservices/
├── frontend/              # Next.js frontend application
│   ├── app/              # Next.js pages
│   ├── api/              # API clients
│   ├── components/       # React components
│   ├── contexts/         # React contexts
│   └── lib/              # Utilities
├── MyApp.Auth/           # Authentication service
├── MyApp.Sales/          # Sales service
├── MyApp.Inventory/      # Inventory service
├── MyApp.Billing/        # Billing service
├── MyApp.Orders/         # Orders service
├── MyApp.Purchasing/     # Purchasing service
├── MyApp.Notification/   # Notification service
└── AppHost/              # .NET Aspire orchestration
```

## Technologies

### Backend
- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- Dapr
- PostgreSQL/SQL Server

### Frontend
- Next.js 16 (App Router)
- TypeScript
- React 19
- TailwindCSS 4
- React Hook Form + Zod
- Axios

## Development

### Frontend Development
See [Frontend README](./frontend/README.md) for detailed instructions.

### Backend Development
Each microservice is independently deployable and follows Clean Architecture principles.

## License

This project is part of the ERP Microservices system.

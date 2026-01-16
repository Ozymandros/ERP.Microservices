# ERP Microservices Documentation

Welcome to the **ERP Microservices** documentation! This is a cloud-native ERP system built with .NET 9, featuring 6 independent microservices orchestrated through Azure Container Apps with DAPR.

## Quick Navigation

### 🚀 Getting Started
- [Quick Start Guide](getting-started.html) - Set up your development environment
- [Quick Reference](guides/QUICK_REFERENCE.html) - Essential commands and patterns
- [Start Here](guides/00_START_HERE.html) - First steps guide

### 📚 Understanding the System
- [Architecture Overview](architecture/ARCHITECTURE_DOCUMENTATION.html) - Complete system design
- [Dependency Mapping](architecture/DEPENDENCY_MAPPING.html) - Service dependencies
- [Conventions & Standards](CONVENTIONS.html) - Code style and best practices

### 🛠️ Development
- [Quick Start Build & Deploy](deployment/QUICK_START_BUILD_DEPLOY.md) - Local development setup
- [Development Guide](development/README.md) - Development workflow
- [Adding Dependencies](development/add-dependencies.prompt.md) - Project reference rules

### ☁️ Infrastructure & Deployment
- [Infrastructure Overview](infrastructure/BICEP_INFRASTRUCTURE_AUDIT_SUMMARY.md) - Bicep templates overview
- [Deployment Guide](deployment/DEPLOYMENT.md) - Production deployment
- [Deployment Automation](deployment/DEPLOYMENT_AUTOMATION.md) - CI/CD pipelines
- [Pre-Deployment Checklist](deployment/PRE_DEPLOYMENT_CHECKLIST.md) - Pre-deployment verification

### 🔒 Security & Configuration
- [Security Guide](security/SECURITY_IDENTITY_BEST_PRACTICES.md) - Security patterns and practices
- [Configuration Management](configuration/APP_CONFIGURATION_INTEGRATION.md) - Environment configuration
- [Ocelot Configuration](configuration/OCELOT_CONFIGURATION_REMEDIATION.md) - API Gateway setup

### 📖 Additional Resources
- [Documentation Status](DOCUMENTATION_STATUS.html) - Documentation completeness
- [File Inventory](FILE_INVENTORY.html) - Complete file listing
- [Sitemap](SITEMAP.html) - Full documentation structure

## Key Services
- **Auth Service** - Authentication and authorization
- **Billing Service** - Invoice and billing management
- **Inventory Service** - Stock and inventory tracking
- **Orders Service** - Customer order management
- **Purchasing Service** - Supplier and procurement management
- **Sales Service** - Sales transactions and reporting
- **API Gateway** - Ocelot-based reverse proxy

## Technology Stack
- **.NET 9** - Backend framework
- **Entity Framework Core** - Database access
- **DAPR** - Microservices communication and state management
- **Azure Container Apps** - Cloud hosting
- **Azure Redis** - Caching and state store
- **SQL Server** - Relational databases
- **Bicep** - Infrastructure as Code

## Getting Help
- Check the [Quick Reference](guides/QUICK_REFERENCE.md) for common tasks
- Review [Conventions](CONVENTIONS.md) for coding standards
- See [Operations Guide](operations/BEST_PRACTICES.md) for operational procedures

---

**Last Updated:** January 4, 2026 | **Status:** Complete
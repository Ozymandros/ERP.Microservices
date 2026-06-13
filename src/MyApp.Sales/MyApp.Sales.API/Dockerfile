# Build arg for registry prefix (must be defined before any FROM that uses it)
# e.g., "myappdevcontainerregistry.azurecr.io/" for ACR builds, empty for local builds
ARG BASE_IMAGE_REGISTRY=

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy nuget.config to ensure clean package resolution (with src/ prefix since build context is root)
COPY src/nuget.config ./

# Copy shared projects (with src/ prefix since build context is root)
COPY ["src/MyApp.Shared/MyApp.Shared.Domain/MyApp.Shared.Domain.csproj", "MyApp.Shared/MyApp.Shared.Domain/"]
COPY ["src/MyApp.Shared/MyApp.Shared.Application/MyApp.Shared.Application.csproj", "MyApp.Shared/MyApp.Shared.Application/"]
COPY ["src/MyApp.Shared/MyApp.Shared.Infrastructure/MyApp.Shared.Infrastructure.csproj", "MyApp.Shared/MyApp.Shared.Infrastructure/"]
COPY ["src/MyApp.Shared/MyApp.Shared.CQRS/MyApp.Shared.CQRS.csproj", "MyApp.Shared/MyApp.Shared.CQRS/"]
COPY ["src/MyApp.Shared/MyApp.Shared.SignalR/MyApp.Shared.SignalR.csproj", "MyApp.Shared/MyApp.Shared.SignalR/"]

# Copy Sales service projects
COPY ["src/MyApp.Sales/MyApp.Sales.Domain/MyApp.Sales.Domain.csproj", "MyApp.Sales/MyApp.Sales.Domain/"]
COPY ["src/MyApp.Sales/MyApp.Sales.Application.Contracts/MyApp.Sales.Application.Contracts.csproj", "MyApp.Sales/MyApp.Sales.Application.Contracts/"]
COPY ["src/MyApp.Sales/MyApp.Sales.Application/MyApp.Sales.Application.csproj", "MyApp.Sales/MyApp.Sales.Application/"]
COPY ["src/MyApp.Sales/MyApp.Sales.Infrastructure/MyApp.Sales.Infrastructure.csproj", "MyApp.Sales/MyApp.Sales.Infrastructure/"]
COPY ["src/MyApp.Sales/MyApp.Sales.API/MyApp.Sales.API.csproj", "MyApp.Sales/MyApp.Sales.API/"]

# Cross-service Application.Contracts (compile-time DTO references only)
COPY ["src/MyApp.Inventory/MyApp.Inventory.Domain/MyApp.Inventory.Domain.csproj", "MyApp.Inventory/MyApp.Inventory.Domain/"]
COPY ["src/MyApp.Inventory/MyApp.Inventory.Application.Contracts/MyApp.Inventory.Application.Contracts.csproj", "MyApp.Inventory/MyApp.Inventory.Application.Contracts/"]
COPY ["src/MyApp.Orders/MyApp.Orders.Domain/MyApp.Orders.Domain.csproj", "MyApp.Orders/MyApp.Orders.Domain/"]
COPY ["src/MyApp.Orders/MyApp.Orders.Application.Contracts/MyApp.Orders.Application.Contracts.csproj", "MyApp.Orders/MyApp.Orders.Application.Contracts/"]

# Restore dependencies
RUN dotnet restore "MyApp.Sales/MyApp.Sales.API/MyApp.Sales.API.csproj"

# Copy only needed source files (not everything from context)
COPY src/MyApp.Shared/ MyApp.Shared/
COPY src/MyApp.Inventory/MyApp.Inventory.Domain/ MyApp.Inventory/MyApp.Inventory.Domain/
COPY src/MyApp.Inventory/MyApp.Inventory.Application.Contracts/ MyApp.Inventory/MyApp.Inventory.Application.Contracts/
COPY src/MyApp.Orders/MyApp.Orders.Domain/ MyApp.Orders/MyApp.Orders.Domain/
COPY src/MyApp.Orders/MyApp.Orders.Application.Contracts/ MyApp.Orders/MyApp.Orders.Application.Contracts/
COPY src/MyApp.Sales/ MyApp.Sales/

# Build the project
WORKDIR "/src/MyApp.Sales/MyApp.Sales.API"
RUN dotnet build "MyApp.Sales.API.csproj" -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish "MyApp.Sales.API.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Runtime stage - Use shared base image
# BASE_IMAGE_REGISTRY is defined at the top of the file
FROM ${BASE_IMAGE_REGISTRY}myapp-microservices-base:10.0 AS final
USER appuser
WORKDIR /app
COPY --chown=appuser:appuser --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.Sales.API.dll"]

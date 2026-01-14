# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy nuget.config to ensure clean package resolution (with src/ prefix since build context is root)
COPY src/nuget.config ./

# Copy shared projects
COPY ["src/MyApp.Shared/MyApp.Shared.Domain/MyApp.Shared.Domain.csproj", "MyApp.Shared/MyApp.Shared.Domain/"]
COPY ["src/MyApp.Shared/MyApp.Shared.Infrastructure/MyApp.Shared.Infrastructure.csproj", "MyApp.Shared/MyApp.Shared.Infrastructure/"]
COPY ["src/MyApp.Shared/MyApp.Shared.CQRS/MyApp.Shared.CQRS.csproj", "MyApp.Shared/MyApp.Shared.CQRS/"]
COPY ["src/MyApp.Shared/MyApp.Shared.SignalR/MyApp.Shared.SignalR.csproj", "MyApp.Shared/MyApp.Shared.SignalR/"]

# Copy Inventory service projects
COPY ["src/MyApp.Inventory/MyApp.Inventory.Domain/MyApp.Inventory.Domain.csproj", "MyApp.Inventory/MyApp.Inventory.Domain/"]
COPY ["src/MyApp.Inventory/MyApp.Inventory.Application.Contracts/MyApp.Inventory.Application.Contracts.csproj", "MyApp.Inventory/MyApp.Inventory.Application.Contracts/"]
COPY ["src/MyApp.Inventory/MyApp.Inventory.Application/MyApp.Inventory.Application.csproj", "MyApp.Inventory/MyApp.Inventory.Application/"]
COPY ["src/MyApp.Inventory/MyApp.Inventory.Infrastructure/MyApp.Inventory.Infrastructure.csproj", "MyApp.Inventory/MyApp.Inventory.Infrastructure/"]
COPY ["src/MyApp.Inventory/MyApp.Inventory.API/MyApp.Inventory.API.csproj", "MyApp.Inventory/MyApp.Inventory.API/"]

# Restore dependencies with BuildKit cache mount (MUCH FASTER)
# Using shared cache across all services for better performance
RUN --mount=type=cache,id=nuget-shared,target=/root/.nuget/packages \
    dotnet restore "MyApp.Inventory/MyApp.Inventory.API/MyApp.Inventory.API.csproj"

# Copy only needed source files (not everything from context)
COPY src/MyApp.Shared/ MyApp.Shared/
COPY src/MyApp.Inventory/ MyApp.Inventory/

# Build the project
WORKDIR "/src/MyApp.Inventory/MyApp.Inventory.API"
RUN --mount=type=cache,id=nuget-shared,target=/root/.nuget/packages \
    dotnet build "MyApp.Inventory.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN --mount=type=cache,id=nuget-shared,target=/root/.nuget/packages \
    dotnet publish "MyApp.Inventory.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage - Use shared base image
# Build arg for registry prefix (e.g., "myappdevcontainerregistry.azurecr.io/" or empty for local)
ARG BASE_IMAGE_REGISTRY=""
FROM ${BASE_IMAGE_REGISTRY}myapp-microservices-base:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.Inventory.API.dll"]

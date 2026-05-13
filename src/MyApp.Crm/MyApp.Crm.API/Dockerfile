# Build arg for registry prefix (must be defined before any FROM that uses it)
# e.g., "myappdevcontainerregistry.azurecr.io/" for ACR builds, empty for local builds
ARG BASE_IMAGE_REGISTRY=

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy nuget.config to ensure clean package resolution (with src/ prefix since build context is root)
COPY src/nuget.config ./

# Copy shared projects (with src/ prefix since build context is root); CRM uses Domain, Infrastructure, CQRS (no SignalR)
COPY ["src/MyApp.Shared/MyApp.Shared.Domain/MyApp.Shared.Domain.csproj", "MyApp.Shared/MyApp.Shared.Domain/"]
COPY ["src/MyApp.Shared/MyApp.Shared.Infrastructure/MyApp.Shared.Infrastructure.csproj", "MyApp.Shared/MyApp.Shared.Infrastructure/"]
COPY ["src/MyApp.Shared/MyApp.Shared.CQRS/MyApp.Shared.CQRS.csproj", "MyApp.Shared/MyApp.Shared.CQRS/"]

# Copy CRM service projects
COPY ["src/MyApp.Crm/MyApp.Crm.Domain/MyApp.Crm.Domain.csproj", "MyApp.Crm/MyApp.Crm.Domain/"]
COPY ["src/MyApp.Crm/MyApp.Crm.Application.Contracts/MyApp.Crm.Application.Contracts.csproj", "MyApp.Crm/MyApp.Crm.Application.Contracts/"]
COPY ["src/MyApp.Crm/MyApp.Crm.Application/MyApp.Crm.Application.csproj", "MyApp.Crm/MyApp.Crm.Application/"]
COPY ["src/MyApp.Crm/MyApp.Crm.Infrastructure/MyApp.Crm.Infrastructure.csproj", "MyApp.Crm/MyApp.Crm.Infrastructure/"]
COPY ["src/MyApp.Crm/MyApp.Crm.API/MyApp.Crm.API.csproj", "MyApp.Crm/MyApp.Crm.API/"]

# Restore dependencies
RUN dotnet restore "MyApp.Crm/MyApp.Crm.API/MyApp.Crm.API.csproj"

# Copy only needed source files (not everything from context)
COPY src/MyApp.Shared/ MyApp.Shared/
COPY src/MyApp.Crm/ MyApp.Crm/

# Build the project
WORKDIR "/src/MyApp.Crm/MyApp.Crm.API"
RUN dotnet build "MyApp.Crm.API.csproj" -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish "MyApp.Crm.API.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Runtime stage - Use shared base image
# BASE_IMAGE_REGISTRY is defined at the top of the file
FROM ${BASE_IMAGE_REGISTRY}myapp-microservices-base:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.Crm.API.dll"]

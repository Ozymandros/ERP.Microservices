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

# Copy Audit service projects
COPY ["src/MyApp.Audit/MyApp.Audit.Domain/MyApp.Audit.Domain.csproj", "MyApp.Audit/MyApp.Audit.Domain/"]
COPY ["src/MyApp.Audit/MyApp.Audit.Application.Contracts/MyApp.Audit.Application.Contracts.csproj", "MyApp.Audit/MyApp.Audit.Application.Contracts/"]
COPY ["src/MyApp.Audit/MyApp.Audit.Application/MyApp.Audit.Application.csproj", "MyApp.Audit/MyApp.Audit.Application/"]
COPY ["src/MyApp.Audit/MyApp.Audit.Infrastructure/MyApp.Audit.Infrastructure.csproj", "MyApp.Audit/MyApp.Audit.Infrastructure/"]
COPY ["src/MyApp.Audit/MyApp.Audit.API/MyApp.Audit.API.csproj", "MyApp.Audit/MyApp.Audit.API/"]

# Restore dependencies
RUN dotnet restore "MyApp.Audit/MyApp.Audit.API/MyApp.Audit.API.csproj"

# Copy only needed source files (not everything from context)
COPY src/MyApp.Shared/ MyApp.Shared/
COPY src/MyApp.Audit/ MyApp.Audit/

# Build the project
WORKDIR "/src/MyApp.Audit/MyApp.Audit.API"
RUN dotnet build "MyApp.Audit.API.csproj" -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish "MyApp.Audit.API.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Runtime stage - Use shared base image
# BASE_IMAGE_REGISTRY is defined at the top of the file
FROM ${BASE_IMAGE_REGISTRY}myapp-microservices-base:10.0 AS final
USER appuser
WORKDIR /app
COPY --chown=appuser:appuser --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.Audit.API.dll"]

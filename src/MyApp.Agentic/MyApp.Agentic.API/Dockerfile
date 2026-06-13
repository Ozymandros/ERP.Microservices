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

# Copy Agentic service projects
COPY ["src/MyApp.Agentic/MyApp.Agentic.Domain/MyApp.Agentic.Domain.csproj", "MyApp.Agentic/MyApp.Agentic.Domain/"]
COPY ["src/MyApp.Agentic/MyApp.Agentic.Application.Contracts/MyApp.Agentic.Application.Contracts.csproj", "MyApp.Agentic/MyApp.Agentic.Application.Contracts/"]
COPY ["src/MyApp.Agentic/MyApp.Agentic.Application/MyApp.Agentic.Application.csproj", "MyApp.Agentic/MyApp.Agentic.Application/"]
COPY ["src/MyApp.Agentic/MyApp.Agentic.Infrastructure/MyApp.Agentic.Infrastructure.csproj", "MyApp.Agentic/MyApp.Agentic.Infrastructure/"]
COPY ["src/MyApp.Agentic/MyApp.Agentic.API/MyApp.Agentic.API.csproj", "MyApp.Agentic/MyApp.Agentic.API/"]

# Restore dependencies
RUN dotnet restore "MyApp.Agentic/MyApp.Agentic.API/MyApp.Agentic.API.csproj"

# Copy only needed source files (not everything from context)
COPY src/MyApp.Shared/ MyApp.Shared/
COPY src/MyApp.Agentic/ MyApp.Agentic/

# Build the project
WORKDIR "/src/MyApp.Agentic/MyApp.Agentic.API"
RUN dotnet build "MyApp.Agentic.API.csproj" -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish "MyApp.Agentic.API.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Runtime stage - Use shared base image
# BASE_IMAGE_REGISTRY is defined at the top of the file
FROM ${BASE_IMAGE_REGISTRY}myapp-microservices-base:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.Agentic.API.dll"]

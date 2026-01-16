# Build arg for registry prefix (must be defined before any FROM that uses it)
# e.g., "myappdevcontainerregistry.azurecr.io/" for ACR builds, empty for local builds
ARG BASE_IMAGE_REGISTRY=

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy shared projects (with src/ prefix since build context is root)
COPY ["src/MyApp.Shared/MyApp.Shared.Domain/MyApp.Shared.Domain.csproj", "MyApp.Shared/MyApp.Shared.Domain/"]
COPY ["src/MyApp.Shared/MyApp.Shared.Infrastructure/MyApp.Shared.Infrastructure.csproj", "MyApp.Shared/MyApp.Shared.Infrastructure/"]
COPY ["src/MyApp.Shared/MyApp.Shared.CQRS/MyApp.Shared.CQRS.csproj", "MyApp.Shared/MyApp.Shared.CQRS/"]
COPY ["src/MyApp.Shared/MyApp.Shared.SignalR/MyApp.Shared.SignalR.csproj", "MyApp.Shared/MyApp.Shared.SignalR/"]

# Copy Billing service projects
COPY ["src/MyApp.Billing/MyApp.Billing.Domain/MyApp.Billing.Domain.csproj", "MyApp.Billing/MyApp.Billing.Domain/"]
COPY ["src/MyApp.Billing/MyApp.Billing.Application.Contracts/MyApp.Billing.Application.Contracts.csproj", "MyApp.Billing/MyApp.Billing.Application.Contracts/"]
COPY ["src/MyApp.Billing/MyApp.Billing.Application/MyApp.Billing.Application.csproj", "MyApp.Billing/MyApp.Billing.Application/"]
COPY ["src/MyApp.Billing/MyApp.Billing.Infrastructure/MyApp.Billing.Infrastructure.csproj", "MyApp.Billing/MyApp.Billing.Infrastructure/"]
COPY ["src/MyApp.Billing/MyApp.Billing.API/MyApp.Billing.API.csproj", "MyApp.Billing/MyApp.Billing.API/"]

# Restore dependencies
RUN dotnet restore "MyApp.Billing/MyApp.Billing.API/MyApp.Billing.API.csproj"

# Copy only needed source files (not everything from context)
COPY ["src/MyApp.Shared/", "MyApp.Shared/"]
COPY ["src/MyApp.Billing/", "MyApp.Billing/"]

# Build the project
WORKDIR "/src/MyApp.Billing/MyApp.Billing.API"
RUN dotnet build "MyApp.Billing.API.csproj" -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish "MyApp.Billing.API.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Runtime stage - Use shared base image
# BASE_IMAGE_REGISTRY is defined at the top of the file
FROM ${BASE_IMAGE_REGISTRY}myapp-microservices-base:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.Billing.API.dll"]
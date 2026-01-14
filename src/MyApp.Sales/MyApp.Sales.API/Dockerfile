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

# Copy Sales service projects
COPY ["src/MyApp.Sales/MyApp.Sales.Domain/MyApp.Sales.Domain.csproj", "MyApp.Sales/MyApp.Sales.Domain/"]
COPY ["src/MyApp.Sales/MyApp.Sales.Application.Contracts/MyApp.Sales.Application.Contracts.csproj", "MyApp.Sales/MyApp.Sales.Application.Contracts/"]
COPY ["src/MyApp.Sales/MyApp.Sales.Application/MyApp.Sales.Application.csproj", "MyApp.Sales/MyApp.Sales.Application/"]
COPY ["src/MyApp.Sales/MyApp.Sales.Infrastructure/MyApp.Sales.Infrastructure.csproj", "MyApp.Sales/MyApp.Sales.Infrastructure/"]
COPY ["src/MyApp.Sales/MyApp.Sales.API/MyApp.Sales.API.csproj", "MyApp.Sales/MyApp.Sales.API/"]

# Restore dependencies with BuildKit cache mount (MUCH FASTER)
RUN --mount=type=cache,id=nuget-shared,target=/root/.nuget/packages \
    dotnet restore "MyApp.Sales/MyApp.Sales.API/MyApp.Sales.API.csproj"

# Copy only needed source files (not everything from context)
COPY src/MyApp.Shared/ MyApp.Shared/
COPY src/MyApp.Sales/ MyApp.Sales/

# Build the project
WORKDIR "/src/MyApp.Sales/MyApp.Sales.API"
RUN --mount=type=cache,id=nuget-shared,target=/root/.nuget/packages \
    dotnet build "MyApp.Sales.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN --mount=type=cache,id=nuget-shared,target=/root/.nuget/packages \
    dotnet publish "MyApp.Sales.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage - Use shared base image
# Build arg for registry prefix (e.g., "myappdevcontainerregistry.azurecr.io/" or empty for local)
ARG BASE_IMAGE_REGISTRY=""
FROM ${BASE_IMAGE_REGISTRY}myapp-microservices-base:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.Sales.API.dll"]

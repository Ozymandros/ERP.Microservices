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

# Copy Orders service projects
COPY ["src/MyApp.Orders/MyApp.Orders.Domain/MyApp.Orders.Domain.csproj", "MyApp.Orders/MyApp.Orders.Domain/"]
COPY ["src/MyApp.Orders/MyApp.Orders.Application.Contracts/MyApp.Orders.Application.Contracts.csproj", "MyApp.Orders/MyApp.Orders.Application.Contracts/"]
COPY ["src/MyApp.Orders/MyApp.Orders.Application/MyApp.Orders.Application.csproj", "MyApp.Orders/MyApp.Orders.Application/"]
COPY ["src/MyApp.Orders/MyApp.Orders.Infrastructure/MyApp.Orders.Infrastructure.csproj", "MyApp.Orders/MyApp.Orders.Infrastructure/"]
COPY ["src/MyApp.Orders/MyApp.Orders.API/MyApp.Orders.API.csproj", "MyApp.Orders/MyApp.Orders.API/"]

# Restore dependencies with BuildKit cache mount (MUCH FASTER)
RUN --mount=type=cache,id=nuget-shared,target=/root/.nuget/packages \
    dotnet restore "MyApp.Orders/MyApp.Orders.API/MyApp.Orders.API.csproj"

# Copy only needed source files (not everything from context)
COPY src/MyApp.Shared/ MyApp.Shared/
COPY src/MyApp.Orders/ MyApp.Orders/

# Build the project
WORKDIR "/src/MyApp.Orders/MyApp.Orders.API"
RUN --mount=type=cache,id=nuget-shared,target=/root/.nuget/packages \
    dotnet build "MyApp.Orders.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN --mount=type=cache,id=nuget-shared,target=/root/.nuget/packages \
    dotnet publish "MyApp.Orders.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage - Use shared base image
# Build arg for registry prefix (e.g., "myappdevcontainerregistry.azurecr.io/" or empty for local)
ARG BASE_IMAGE_REGISTRY=""
FROM ${BASE_IMAGE_REGISTRY}myapp-microservices-base:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.Orders.API.dll"]

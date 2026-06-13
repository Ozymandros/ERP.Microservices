# Optional registry prefix for myapp-microservices-base (empty = local; CI: ghcr.io/<owner>/<repo>/)
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

# Copy Auth service projects
COPY ["src/MyApp.Auth/MyApp.Auth.Domain/MyApp.Auth.Domain.csproj", "MyApp.Auth/MyApp.Auth.Domain/"]
COPY ["src/MyApp.Auth/MyApp.Auth.Application.Contracts/MyApp.Auth.Application.Contracts.csproj", "MyApp.Auth/MyApp.Auth.Application.Contracts/"]
COPY ["src/MyApp.Auth/MyApp.Auth.Application/MyApp.Auth.Application.csproj", "MyApp.Auth/MyApp.Auth.Application/"]
COPY ["src/MyApp.Auth/MyApp.Auth.Infrastructure/MyApp.Auth.Infrastructure.csproj", "MyApp.Auth/MyApp.Auth.Infrastructure/"]
COPY ["src/MyApp.Auth/MyApp.Auth.API/MyApp.Auth.API.csproj", "MyApp.Auth/MyApp.Auth.API/"]

# Restore dependencies
RUN dotnet restore "MyApp.Auth/MyApp.Auth.API/MyApp.Auth.API.csproj"

# Copy only needed source files (not everything from context)
COPY src/MyApp.Shared/ MyApp.Shared/
COPY src/MyApp.Auth/ MyApp.Auth/

# Build the project
WORKDIR "/src/MyApp.Auth/MyApp.Auth.API"
RUN dotnet build "MyApp.Auth.API.csproj" -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish "MyApp.Auth.API.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Runtime stage - shared microservices base
FROM ${BASE_IMAGE_REGISTRY}myapp-microservices-base:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.Auth.API.dll"]

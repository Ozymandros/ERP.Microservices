# CI / production build (build context: repository root)
#
# Build args: Gateway uses mcr.microsoft.com/dotnet/aspnet:10.0 for the final image (not myapp-microservices-base).
# No BASE_IMAGE_REGISTRY here unless you introduce one and substitute the Runtime FROM (microservices use ${BASE_IMAGE_REGISTRY}myapp-microservices-base).

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy nuget.config to ensure clean package resolution (with src/ prefix since build context is repository root)
COPY src/nuget.config ./

# Copy ErpApiGateway project (with src/ prefix since build context is repository root)
COPY ["src/ErpApiGateway/ErpApiGateway.csproj", "ErpApiGateway/"]

# Copy shared projects (with src/ prefix since build context is repository root)
COPY ["src/MyApp.Shared/MyApp.Shared.Domain/MyApp.Shared.Domain.csproj", "MyApp.Shared/MyApp.Shared.Domain/"]
COPY ["src/MyApp.Shared/MyApp.Shared.Infrastructure/MyApp.Shared.Infrastructure.csproj", "MyApp.Shared/MyApp.Shared.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "ErpApiGateway/ErpApiGateway.csproj"

# Copy only needed source files (Gateway sources then Shared — preserve this order for deterministic layers)
COPY src/ErpApiGateway/ ErpApiGateway/
COPY src/MyApp.Shared/ MyApp.Shared/

# Build the project
WORKDIR "/src/ErpApiGateway"
RUN dotnet build "ErpApiGateway.csproj" -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish "ErpApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Runtime stage — aspnet:10.0 final (not myapp-microservices-base). BASE_IMAGE_REGISTRY N/A for this image.
# curl/ca-certificates for HTTP health checks; runs as non-root appuser after chown.
# Critical copy order: COPY published output before RUN chown so /app is owned by appuser.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

ENV APP_UID=1654 ASPNETCORE_HTTP_PORTS=8080

USER root
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl ca-certificates && \
    rm -rf /var/lib/apt/lists/* && \
    groupadd -r appuser && \
    useradd -r -g appuser appuser

COPY --chown=appuser:appuser --from=publish /app/publish .

USER appuser

ENTRYPOINT ["dotnet", "ErpApiGateway.dll"]

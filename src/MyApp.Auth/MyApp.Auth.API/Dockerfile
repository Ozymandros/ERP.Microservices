# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src


# Copy shared projects (with src/ prefix since build context is root)
COPY ["src/MyApp.Shared/MyApp.Shared.Domain/MyApp.Shared.Domain.csproj", "MyApp.Shared/MyApp.Shared.Domain/"]
COPY ["src/MyApp.Shared/MyApp.Shared.Infrastructure/MyApp.Shared.Infrastructure.csproj", "MyApp.Shared/MyApp.Shared.Infrastructure/"]

# Copy Auth service projects
COPY ["src/MyApp.Auth/MyApp.Auth.Domain/MyApp.Auth.Domain.csproj", "MyApp.Auth/MyApp.Auth.Domain/"]
COPY ["src/MyApp.Auth/MyApp.Auth.Application.Contracts/MyApp.Auth.Application.Contracts.csproj", "MyApp.Auth/MyApp.Auth.Application.Contracts/"]
COPY ["src/MyApp.Auth/MyApp.Auth.Application/MyApp.Auth.Application.csproj", "MyApp.Auth/MyApp.Auth.Application/"]
COPY ["src/MyApp.Auth/MyApp.Auth.Infrastructure/MyApp.Auth.Infrastructure.csproj", "MyApp.Auth/MyApp.Auth.Infrastructure/"]
COPY ["src/MyApp.Auth/MyApp.Auth.API/MyApp.Auth.API.csproj", "MyApp.Auth/MyApp.Auth.API/"]

# Restore dependencies
RUN dotnet restore "MyApp.Auth/MyApp.Auth.API/MyApp.Auth.API.csproj"

# Copy all source files (with src/ prefix since build context is root)
COPY src/ .

# Build the project
WORKDIR "/src/MyApp.Auth/MyApp.Auth.API"
RUN dotnet build "MyApp.Auth.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MyApp.Auth.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

# Pre-install dependencies in a separate layer for better caching
# This layer will be cached and reused unless the base image changes
USER root
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl ca-certificates && \
    rm -rf /var/lib/apt/lists/* && \
    groupadd -r appuser && \
    useradd -r -g appuser appuser

# Copy files and set ownership (this changes more frequently)
# IMPORTANT: Copy files while still root, then change ownership
# If we switch to 'appuser' before copying, the files will be owned by root
# and the non-root user won't have permissions to run them
COPY --from=publish /app/publish .
RUN chown -R appuser:appuser /app

# Switch to non-root user for security (principle of least privilege)
USER appuser

ENTRYPOINT ["dotnet", "MyApp.Auth.API.dll"]

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src


# Copy shared projects
COPY ["MyApp.Shared/MyApp.Shared.Domain/MyApp.Shared.Domain.csproj", "MyApp.Shared/MyApp.Shared.Domain/"]
COPY ["MyApp.Shared/MyApp.Shared.Infrastructure/MyApp.Shared.Infrastructure.csproj", "MyApp.Shared/MyApp.Shared.Infrastructure/"]

# Copy Auth service projects
COPY ["MyApp.Auth/MyApp.Auth.Domain/MyApp.Auth.Domain.csproj", "MyApp.Auth/MyApp.Auth.Domain/"]
COPY ["MyApp.Auth/MyApp.Auth.Application.Contracts/MyApp.Auth.Application.Contracts.csproj", "MyApp.Auth/MyApp.Auth.Application.Contracts/"]
COPY ["MyApp.Auth/MyApp.Auth.Application/MyApp.Auth.Application.csproj", "MyApp.Auth/MyApp.Auth.Application/"]
COPY ["MyApp.Auth/MyApp.Auth.Infrastructure/MyApp.Auth.Infrastructure.csproj", "MyApp.Auth/MyApp.Auth.Infrastructure/"]
COPY ["MyApp.Auth/MyApp.Auth.API/MyApp.Auth.API.csproj", "MyApp.Auth/MyApp.Auth.API/"]

# Restore dependencies
RUN dotnet restore "MyApp.Auth/MyApp.Auth.API/MyApp.Auth.API.csproj"

# Copy all source files
COPY . .

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

# Install curl for health checks
USER root
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy files and set ownership
COPY --from=publish /app/publish .
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

ENTRYPOINT ["dotnet", "MyApp.Auth.API.dll"]

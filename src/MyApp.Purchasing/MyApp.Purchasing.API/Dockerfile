# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy shared projects
COPY ["MyApp.Shared/MyApp.Shared.Domain/MyApp.Shared.Domain.csproj", "MyApp.Shared/MyApp.Shared.Domain/"]
COPY ["MyApp.Shared/MyApp.Shared.Infrastructure/MyApp.Shared.Infrastructure.csproj", "MyApp.Shared/MyApp.Shared.Infrastructure/"]

# Copy Purchasing service projects
COPY ["MyApp.Purchasing/MyApp.Purchasing.Domain/MyApp.Purchasing.Domain.csproj", "MyApp.Purchasing/MyApp.Purchasing.Domain/"]
COPY ["MyApp.Purchasing/MyApp.Purchasing.Application.Contracts/MyApp.Purchasing.Application.Contracts.csproj", "MyApp.Purchasing/MyApp.Purchasing.Application.Contracts/"]
COPY ["MyApp.Purchasing/MyApp.Purchasing.Application/MyApp.Purchasing.Application.csproj", "MyApp.Purchasing/MyApp.Purchasing.Application/"]
COPY ["MyApp.Purchasing/MyApp.Purchasing.Infrastructure/MyApp.Purchasing.Infrastructure.csproj", "MyApp.Purchasing/MyApp.Purchasing.Infrastructure/"]
COPY ["MyApp.Purchasing/MyApp.Purchasing.API/MyApp.Purchasing.API.csproj", "MyApp.Purchasing/MyApp.Purchasing.API/"]

# Restore dependencies
RUN dotnet restore "MyApp.Purchasing/MyApp.Purchasing.API/MyApp.Purchasing.API.csproj"

# Copy all source files
COPY . .

# Build the project
WORKDIR "/src/MyApp.Purchasing/MyApp.Purchasing.API"
RUN dotnet build "MyApp.Purchasing.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MyApp.Purchasing.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
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

ENTRYPOINT ["dotnet", "MyApp.Purchasing.API.dll"]

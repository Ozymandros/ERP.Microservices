# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src


# Copy shared projects
COPY ["MyApp.Shared/MyApp.Shared.Domain/MyApp.Shared.Domain.csproj", "MyApp.Shared/MyApp.Shared.Domain/"]
COPY ["MyApp.Shared/MyApp.Shared.Infrastructure/MyApp.Shared.Infrastructure.csproj", "MyApp.Shared/MyApp.Shared.Infrastructure/"]

# Copy Sales service projects
COPY ["MyApp.Sales/MyApp.Sales.Domain/MyApp.Sales.Domain.csproj", "MyApp.Sales/MyApp.Sales.Domain/"]
COPY ["MyApp.Sales/MyApp.Sales.Application.Contracts/MyApp.Sales.Application.Contracts.csproj", "MyApp.Sales/MyApp.Sales.Application.Contracts/"]
COPY ["MyApp.Sales/MyApp.Sales.Application/MyApp.Sales.Application.csproj", "MyApp.Sales/MyApp.Sales.Application/"]
COPY ["MyApp.Sales/MyApp.Sales.Infrastructure/MyApp.Sales.Infrastructure.csproj", "MyApp.Sales/MyApp.Sales.Infrastructure/"]
COPY ["MyApp.Sales/MyApp.Sales.API/MyApp.Sales.API.csproj", "MyApp.Sales/MyApp.Sales.API/"]

# Restore dependencies
RUN dotnet restore "MyApp.Sales/MyApp.Sales.API/MyApp.Sales.API.csproj"

# Copy all source files
COPY . .

# Build the project
WORKDIR "/src/MyApp.Sales/MyApp.Sales.API"
RUN dotnet build "MyApp.Sales.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MyApp.Sales.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS final
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

ENTRYPOINT ["dotnet", "MyApp.Sales.API.dll"]

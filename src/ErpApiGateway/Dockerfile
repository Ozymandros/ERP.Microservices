# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the Gateway project file for restore
COPY ["ErpApiGateway/ErpApiGateway.csproj", "ErpApiGateway/"]

# Copy shared projects
COPY ["MyApp.Shared/MyApp.Shared.Domain/MyApp.Shared.Domain.csproj", "MyApp.Shared/MyApp.Shared.Domain/"]
COPY ["MyApp.Shared/MyApp.Shared.Infrastructure/MyApp.Shared.Infrastructure.csproj", "MyApp.Shared/MyApp.Shared.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "ErpApiGateway/ErpApiGateway.csproj"

# Copy source files for Gateway
COPY ["ErpApiGateway/", "ErpApiGateway/"]
COPY ["MyApp.Shared/", "MyApp.Shared/"]

# Build the project
WORKDIR "/src/ErpApiGateway"
RUN dotnet build "ErpApiGateway.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ErpApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS final
WORKDIR /app
EXPOSE 8080

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ErpApiGateway.dll"]
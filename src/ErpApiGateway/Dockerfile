# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
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
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

# --- THE LINES FROM YOUR IMAGE (Preserving everything) ---
ENV APP_UID=1654 ASPNETCORE_HTTP_PORTS=8080
RUN apt-get update && apt-get install -y --no-install-recommends curl

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# --- CRITICAL ORDER CHANGE ---

# 1. Copy the files FIRST while still root
# If we change to 'appuser' before this, the files will be owned by root 
# and the non-root user won't have permissions to run them.
COPY --from=publish /app/publish .

# 2. Change ownership AFTER the files are already in the folder
# This ensures that 'appuser' owns the actual DLLs.
RUN chown -R appuser:appuser /app

# 3. NOW switch to the non-root user
USER appuser

ENTRYPOINT ["dotnet", "ErpApiGateway.dll"]
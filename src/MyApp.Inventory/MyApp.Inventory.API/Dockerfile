# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src


# Copy shared projects
COPY ["MyApp.Shared/MyApp.Shared.Domain/MyApp.Shared.Domain.csproj", "MyApp.Shared/MyApp.Shared.Domain/"]
COPY ["MyApp.Shared/MyApp.Shared.Infrastructure/MyApp.Shared.Infrastructure.csproj", "MyApp.Shared/MyApp.Shared.Infrastructure/"]

# Copy Inventory service projects
COPY ["MyApp.Inventory/MyApp.Inventory.Domain/MyApp.Inventory.Domain.csproj", "MyApp.Inventory/MyApp.Inventory.Domain/"]
COPY ["MyApp.Inventory/MyApp.Inventory.Application.Contracts/MyApp.Inventory.Application.Contracts.csproj", "MyApp.Inventory/MyApp.Inventory.Application.Contracts/"]
COPY ["MyApp.Inventory/MyApp.Inventory.Application/MyApp.Inventory.Application.csproj", "MyApp.Inventory/MyApp.Inventory.Application/"]
COPY ["MyApp.Inventory/MyApp.Inventory.Infrastructure/MyApp.Inventory.Infrastructure.csproj", "MyApp.Inventory/MyApp.Inventory.Infrastructure/"]
COPY ["MyApp.Inventory/MyApp.Inventory.API/MyApp.Inventory.API.csproj", "MyApp.Inventory/MyApp.Inventory.API/"]

# Restore dependencies
RUN dotnet restore "MyApp.Inventory/MyApp.Inventory.API/MyApp.Inventory.API.csproj"

# Copy all source files
COPY . .

# Build the project
WORKDIR "/src/MyApp.Inventory/MyApp.Inventory.API"
RUN dotnet build "MyApp.Inventory.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MyApp.Inventory.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage - Use shared base image
FROM myapp-microservices-base:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.Inventory.API.dll"]

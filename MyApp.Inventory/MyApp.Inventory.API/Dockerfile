# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MyApp.Inventory/MyApp.Inventory.API/MyApp.Inventory.API.csproj", "MyApp.Inventory/MyApp.Inventory.API/"]
COPY ["MyApp.Inventory/MyApp.Inventory.Domain/MyApp.Inventory.Domain.csproj", "MyApp.Inventory/MyApp.Inventory.Domain/"]
COPY ["MyApp.Inventory/MyApp.Inventory.Application/MyApp.Inventory.Application.csproj", "MyApp.Inventory/MyApp.Inventory.Application/"]
COPY ["MyApp.Inventory/MyApp.Inventory.Application.Contracts/MyApp.Inventory.Application.Contracts.csproj", "MyApp.Inventory/MyApp.Inventory.Application.Contracts/"]
COPY ["MyApp.Shared/MyApp.Shared.Domain/MyApp.Shared.Domain.csproj", "MyApp.Shared/MyApp.Shared.Domain/"]
COPY ["MyApp.Shared/MyApp.Shared.CQRS/MyApp.Shared.CQRS.csproj", "MyApp.Shared/MyApp.Shared.CQRS/"]
COPY ["MyApp.Inventory/MyApp.Inventory.Infrastructure/MyApp.Inventory.Infrastructure.csproj", "MyApp.Inventory/MyApp.Inventory.Infrastructure/"]
RUN dotnet restore "./MyApp.Inventory/MyApp.Inventory.API/MyApp.Inventory.API.csproj"
COPY . .
WORKDIR "/src/MyApp.Inventory/MyApp.Inventory.API"
RUN dotnet build "./MyApp.Inventory.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MyApp.Inventory.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.Inventory.API.dll"]
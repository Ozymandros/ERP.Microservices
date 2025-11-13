# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
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
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.Sales.API.dll"]

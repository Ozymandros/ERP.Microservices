# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src


# Copy shared projects
COPY ["MyApp.Shared/MyApp.Shared.Domain/MyApp.Shared.Domain.csproj", "MyApp.Shared/MyApp.Shared.Domain/"]
COPY ["MyApp.Shared/MyApp.Shared.Infrastructure/MyApp.Shared.Infrastructure.csproj", "MyApp.Shared/MyApp.Shared.Infrastructure/"]

# Copy Orders service projects
COPY ["MyApp.Orders/MyApp.Orders.Domain/MyApp.Orders.Domain.csproj", "MyApp.Orders/MyApp.Orders.Domain/"]
COPY ["MyApp.Orders/MyApp.Orders.Application.Contracts/MyApp.Orders.Application.Contracts.csproj", "MyApp.Orders/MyApp.Orders.Application.Contracts/"]
COPY ["MyApp.Orders/MyApp.Orders.Application/MyApp.Orders.Application.csproj", "MyApp.Orders/MyApp.Orders.Application/"]
COPY ["MyApp.Orders/MyApp.Orders.Infrastructure/MyApp.Orders.Infrastructure.csproj", "MyApp.Orders/MyApp.Orders.Infrastructure/"]
COPY ["MyApp.Orders/MyApp.Orders.API/MyApp.Orders.API.csproj", "MyApp.Orders/MyApp.Orders.API/"]

# Restore dependencies
RUN dotnet restore "MyApp.Orders/MyApp.Orders.API/MyApp.Orders.API.csproj"

# Copy all source files
COPY . .

# Build the project
WORKDIR "/src/MyApp.Orders/MyApp.Orders.API"
RUN dotnet build "MyApp.Orders.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MyApp.Orders.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS final
WORKDIR /app
EXPOSE 8080

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.Orders.API.dll"]

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy shared projects
COPY ["MyApp.Shared/MyApp.Shared.Domain/MyApp.Shared.Domain.csproj", "MyApp.Shared/MyApp.Shared.Domain/"]
COPY ["MyApp.Shared/MyApp.Shared.Infrastructure/MyApp.Shared.Infrastructure.csproj", "MyApp.Shared/MyApp.Shared.Infrastructure/"]

# Copy Billing service projects
COPY ["MyApp.Billing/MyApp.Billing.Domain/MyApp.Billing.Domain.csproj", "MyApp.Billing/MyApp.Billing.Domain/"]
COPY ["MyApp.Billing/MyApp.Billing.Application.Contracts/MyApp.Billing.Application.Contracts.csproj", "MyApp.Billing/MyApp.Billing.Application.Contracts/"]
COPY ["MyApp.Billing/MyApp.Billing.Application/MyApp.Billing.Application.csproj", "MyApp.Billing/MyApp.Billing.Application/"]
COPY ["MyApp.Billing/MyApp.Billing.Infrastructure/MyApp.Billing.Infrastructure.csproj", "MyApp.Billing/MyApp.Billing.Infrastructure/"]
COPY ["MyApp.Billing/MyApp.Billing.API/MyApp.Billing.API.csproj", "MyApp.Billing/MyApp.Billing.API/"]

# Restore dependencies
RUN dotnet restore "MyApp.Billing/MyApp.Billing.API/MyApp.Billing.API.csproj"

# Copy all source files
COPY . .

# Build the project
WORKDIR "/src/MyApp.Billing/MyApp.Billing.API"
RUN dotnet build "MyApp.Billing.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MyApp.Billing.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage - Use shared base image
FROM myapp-microservices-base:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.Billing.API.dll"]
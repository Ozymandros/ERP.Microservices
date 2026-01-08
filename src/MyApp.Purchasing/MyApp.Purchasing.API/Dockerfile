# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy nuget.config to ensure clean package resolution
COPY nuget.config ./

# Copy shared projects
COPY ["MyApp.Shared/MyApp.Shared.Domain/MyApp.Shared.Domain.csproj", "MyApp.Shared/MyApp.Shared.Domain/"]
COPY ["MyApp.Shared/MyApp.Shared.Infrastructure/MyApp.Shared.Infrastructure.csproj", "MyApp.Shared/MyApp.Shared.Infrastructure/"]
COPY ["MyApp.Shared/MyApp.Shared.CQRS/MyApp.Shared.CQRS.csproj", "MyApp.Shared/MyApp.Shared.CQRS/"]
COPY ["MyApp.Shared/MyApp.Shared.SignalR/MyApp.Shared.SignalR.csproj", "MyApp.Shared/MyApp.Shared.SignalR/"]

# Copy Purchasing service projects
COPY ["MyApp.Purchasing/MyApp.Purchasing.Domain/MyApp.Purchasing.Domain.csproj", "MyApp.Purchasing/MyApp.Purchasing.Domain/"]
COPY ["MyApp.Purchasing/MyApp.Purchasing.Application.Contracts/MyApp.Purchasing.Application.Contracts.csproj", "MyApp.Purchasing/MyApp.Purchasing.Application.Contracts/"]
COPY ["MyApp.Purchasing/MyApp.Purchasing.Application/MyApp.Purchasing.Application.csproj", "MyApp.Purchasing/MyApp.Purchasing.Application/"]
COPY ["MyApp.Purchasing/MyApp.Purchasing.Infrastructure/MyApp.Purchasing.Infrastructure.csproj", "MyApp.Purchasing/MyApp.Purchasing.Infrastructure/"]
COPY ["MyApp.Purchasing/MyApp.Purchasing.API/MyApp.Purchasing.API.csproj", "MyApp.Purchasing/MyApp.Purchasing.API/"]

# Restore dependencies with BuildKit cache mount (MUCH FASTER)
RUN --mount=type=cache,id=nuget-shared,target=/root/.nuget/packages \
    dotnet restore "MyApp.Purchasing/MyApp.Purchasing.API/MyApp.Purchasing.API.csproj"

# Copy only needed source files (not everything from context)
COPY MyApp.Shared/ MyApp.Shared/
COPY MyApp.Purchasing/ MyApp.Purchasing/

# Build the project with cache mount
WORKDIR "/src/MyApp.Purchasing/MyApp.Purchasing.API"
RUN --mount=type=cache,id=nuget-shared,target=/root/.nuget/packages \
    dotnet build "MyApp.Purchasing.API.csproj" -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN --mount=type=cache,id=nuget-shared,target=/root/.nuget/packages \
    dotnet publish "MyApp.Purchasing.API.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Runtime stage - Use shared base image
FROM myapp-microservices-base:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.Purchasing.API.dll"]

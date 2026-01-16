# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy entire contents (azd copies only the service folder here)
COPY . .

# Build the project
RUN dotnet build "MyApp.Auth.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MyApp.Auth.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM myapp-microservices-base:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.Auth.API.dll"]

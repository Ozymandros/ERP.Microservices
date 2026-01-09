# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy entire contents
COPY . .

# Build the project
RUN dotnet build "MyApp.Billing.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MyApp.Billing.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM myapp-microservices-base:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.Billing.API.dll"]
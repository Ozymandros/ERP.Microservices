# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file
COPY ["ERP.Microservices.sln", "./"]

# Copy Gateway project
COPY ["ErpApiGateway/ErpApiGateway.csproj", "ErpApiGateway/"]

# Restore dependencies
RUN dotnet restore "ErpApiGateway/ErpApiGateway.csproj"

# Copy all source files
COPY . .

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

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ErpApiGateway.dll"]
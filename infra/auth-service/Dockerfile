# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy entire contents (azd copies only the service folder here)
COPY . .

# Build the project
RUN dotnet build "MyApp.Auth.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MyApp.Auth.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage (azd copies only this folder; CI passes BASE_IMAGE_REGISTRY)
ARG BASE_IMAGE_REGISTRY=
FROM ${BASE_IMAGE_REGISTRY}myapp-microservices-base:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.Auth.API.dll"]

# Etapa base per executar
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["MyApp.Purchasing.API/MyApp.Purchasing.API.csproj", "MyApp.Purchasing.API/"]
RUN dotnet restore "MyApp.Purchasing.API/MyApp.Purchasing.API.csproj"
COPY . .
WORKDIR "/src/MyApp.Purchasing.API"
RUN dotnet build "MyApp.Purchasing.API.csproj" -c Release -o /app/build

# Etapa de publicació
FROM build AS publish
RUN dotnet publish "MyApp.Purchasing.API.csproj" -c Release -o /app/publish

# Etapa final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.Purchasing.API.dll"]

ARG BASE_IMAGE_REGISTRY=

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy nuget.config (context is repo root when using this Dockerfile)
COPY src/nuget.config ./

# Copy project references
COPY ["src/MyApp.Shared/MyApp.Shared.Domain/MyApp.Shared.Domain.csproj", "MyApp.Shared.MyApp.Shared.Domain/"]
COPY ["src/MyApp.Shared/MyApp.Shared.Infrastructure/MyApp.Shared.Infrastructure.csproj", "MyApp.Shared.MyApp.Shared.Infrastructure/"]

COPY ["src/MyApp.Auth/MyApp.Auth.Application.Contracts/MyApp.Auth.Application.Contracts.csproj", "MyApp.Auth.MyApp.Auth.Application.Contracts/"]
COPY ["src/MyApp.Billing/MyApp.Billing.Application.Contracts/MyApp.Billing.Application.Contracts.csproj", "MyApp.Billing.MyApp.Billing.Application.Contracts/"]
COPY ["src/MyApp.Crm/MyApp.Crm.Application.Contracts/MyApp.Crm.Application.Contracts.csproj", "MyApp.Crm.MyApp.Crm.Application.Contracts/"]
COPY ["src/MyApp.Inventory/MyApp.Inventory.Application.Contracts/MyApp.Inventory.Application.Contracts.csproj", "MyApp.Inventory.MyApp.Inventory.Application.Contracts/"]
COPY ["src/MyApp.Orders/MyApp.Orders.Application.Contracts/MyApp.Orders.Application.Contracts.csproj", "MyApp.Orders.MyApp.Orders.Application.Contracts/"]
COPY ["src/MyApp.Purchasing/MyApp.Purchasing.Application.Contracts/MyApp.Purchasing.Application.Contracts.csproj", "MyApp.Purchasing.MyApp.Purchasing.Application.Contracts/"]
COPY ["src/MyApp.Sales/MyApp.Sales.Application.Contracts/MyApp.Sales.Application.Contracts.csproj", "MyApp.Sales.MyApp.Sales.Application.Contracts/"]

COPY ["src/MyApp.SemanticKernel/MyApp.SemanticKernel.csproj", "MyApp.SemanticKernel/"]

RUN dotnet restore "MyApp.SemanticKernel/MyApp.SemanticKernel.csproj"

COPY src/ MyAppSrc/
COPY MyApp.SemanticKernel/ MyApp.SemanticKernel/

WORKDIR "/src/MyApp.SemanticKernel"
RUN dotnet build "MyApp.SemanticKernel.csproj" -c Release -o /app/build --no-restore

FROM build AS publish
RUN dotnet publish "MyApp.SemanticKernel.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM ${BASE_IMAGE_REGISTRY}myapp-microservices-base:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.SemanticKernel.dll"]

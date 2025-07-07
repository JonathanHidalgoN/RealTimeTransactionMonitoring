FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY FinancialMonitoringSolution.sln .

COPY src/FinancialMonitoring.Models/*.csproj src/FinancialMonitoring.Models/
COPY tests/FinancialMonitoring.LoadTests/*.csproj tests/FinancialMonitoring.LoadTests/

RUN dotnet restore tests/FinancialMonitoring.LoadTests/FinancialMonitoring.LoadTests.csproj

COPY src/FinancialMonitoring.Models/ src/FinancialMonitoring.Models/
COPY tests/FinancialMonitoring.LoadTests/ tests/FinancialMonitoring.LoadTests/

RUN dotnet build tests/FinancialMonitoring.LoadTests/FinancialMonitoring.LoadTests.csproj -c Release --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /source/tests/FinancialMonitoring.LoadTests/bin/Release/net8.0/ .

ENTRYPOINT ["dotnet", "FinancialMonitoring.LoadTests.dll"]

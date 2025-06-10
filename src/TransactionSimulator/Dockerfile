FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY FinancialMonitoringSolution.sln .
COPY src/TransactionSimulator/TransactionSimulator.csproj ./src/TransactionSimulator/
COPY src/FinancialMonitoring.Models/FinancialMonitoring.Models.csproj ./src/FinancialMonitoring.Models/
COPY src/FinancialMonitoring.Abstractions/FinancialMonitoring.Abstractions.csproj ./src/FinancialMonitoring.Abstractions/

RUN dotnet restore ./src/TransactionSimulator/TransactionSimulator.csproj

COPY ./src/ ./src/

RUN dotnet publish ./src/TransactionSimulator/TransactionSimulator.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TransactionSimulator.dll"]

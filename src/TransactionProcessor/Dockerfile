FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY FinancialMonitoringSolution.sln .
COPY src/TransactionProcessor/TransactionProcessor.csproj ./src/TransactionProcessor/
COPY src/FinancialMonitoring.Models/FinancialMonitoring.Models.csproj ./src/FinancialMonitoring.Models/

# RUN dotnet restore FinancialMonitoringSolution.sln
RUN dotnet restore ./src/TransactionProcessor/TransactionProcessor.csproj

COPY ./src/ ./src/

RUN dotnet publish ./src/TransactionProcessor/TransactionProcessor.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TransactionProcessor.dll"]

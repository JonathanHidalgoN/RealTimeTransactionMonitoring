FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY FinancialMonitoringSolution.sln .
COPY src/FinancialMonitoring.Api/FinancialMonitoring.Api.csproj ./src/FinancialMonitoring.Api/
COPY src/FinancialMonitoring.Models/FinancialMonitoring.Models.csproj ./src/FinancialMonitoring.Models/
COPY src/FinancialMonitoring.Abstractions/FinancialMonitoring.Abstractions.csproj ./src/FinancialMonitoring.Abstractions/
COPY src/TransactionProcessor/TransactionProcessor.csproj ./src/TransactionProcessor/
COPY src/TransactionSimulator/TransactionSimulator.csproj ./src/TransactionSimulator/
COPY tests/FinancialMonitoring.Api.Tests/FinancialMonitoring.Api.Tests.csproj ./tests/FinancialMonitoring.Api.Tests/
COPY tests/FinancialMonitoring.Models.Tests/FinancialMonitoring.Models.Tests.csproj ./tests/FinancialMonitoring.Models.Tests/
COPY tests/TransactionSimulator.Tests/TransactionSimulator.Tests.csproj ./tests/TransactionSimulator.Tests/
COPY tests/TransactionProcessor.Tests/TransactionProcessor.Tests.csproj ./tests/TransactionProcessor.Tests/
COPY src/FinancialMonitoring.WebApp/FinancialMonitoring.WebApp.csproj ./src/FinancialMonitoring.WebApp/ # <-- ADDED

RUN dotnet restore "FinancialMonitoringSolution.sln"

COPY . .

RUN dotnet publish "src/TransactionProcessor/TransactionProcessor.csproj" -c Release -o /app/publish --no-restore


FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TransactionProcessor.dll"]

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY FinancialMonitoringSolution.sln .
COPY src/FinancialMonitoring.Api/FinancialMonitoring.Api.csproj ./src/FinancialMonitoring.Api/
COPY src/FinancialMonitoring.Models/FinancialMonitoring.Models.csproj ./src/FinancialMonitoring.Models/
COPY src/FinancialMonitoring.Abstractions/FinancialMonitoring.Abstractions.csproj ./src/FinancialMonitoring.Abstractions/
COPY src/TransactionProcessor/TransactionProcessor.csproj ./src/TransactionProcessor/
COPY src/TransactionSimulator/TransactionSimulator.csproj ./src/TransactionSimulator/
COPY tests/unit/FinancialMonitoring.Api.Tests/FinancialMonitoring.Api.Tests.csproj ./tests/unit/FinancialMonitoring.Api.Tests/
COPY tests/unit/FinancialMonitoring.Models.Tests/FinancialMonitoring.Models.Tests.csproj ./tests/unit/FinancialMonitoring.Models.Tests/
COPY tests/unit/TransactionSimulator.Tests/TransactionSimulator.Tests.csproj ./tests/unit/TransactionSimulator.Tests/
COPY tests/unit/TransactionProcessor.Tests/TransactionProcessor.Tests.csproj ./tests/unit/TransactionProcessor.Tests/
COPY src/FinancialMonitoring.WebApp/FinancialMonitoring.WebApp.csproj ./src/FinancialMonitoring.WebApp/

RUN dotnet restore "FinancialMonitoringSolution.sln"

COPY . .

RUN dotnet publish "src/FinancialMonitoring.WebApp/FinancialMonitoring.WebApp.csproj" -c Release -o /app/publish


FROM nginx:alpine
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

EXPOSE 80

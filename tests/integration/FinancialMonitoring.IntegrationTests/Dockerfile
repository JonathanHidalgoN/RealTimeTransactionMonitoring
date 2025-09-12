FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy solution file
COPY FinancialMonitoringSolution.sln .

# Copy project files
COPY src/FinancialMonitoring.Models/*.csproj src/FinancialMonitoring.Models/
COPY src/FinancialMonitoring.Abstractions/*.csproj src/FinancialMonitoring.Abstractions/
COPY src/FinancialMonitoring.Api/*.csproj src/FinancialMonitoring.Api/
COPY tests/integration/FinancialMonitoring.IntegrationTests/*.csproj tests/integration/FinancialMonitoring.IntegrationTests/

# Restore packages
RUN dotnet restore tests/integration/FinancialMonitoring.IntegrationTests/FinancialMonitoring.IntegrationTests.csproj

# Copy source code
COPY src/FinancialMonitoring.Models/ src/FinancialMonitoring.Models/
COPY src/FinancialMonitoring.Abstractions/ src/FinancialMonitoring.Abstractions/
COPY src/FinancialMonitoring.Api/ src/FinancialMonitoring.Api/
COPY tests/integration/FinancialMonitoring.IntegrationTests/ tests/integration/FinancialMonitoring.IntegrationTests/

# Build
RUN dotnet build tests/integration/FinancialMonitoring.IntegrationTests/FinancialMonitoring.IntegrationTests.csproj -c Release --no-restore

# Test
FROM build AS test
WORKDIR /source

# Add container test runner script to support category filtering
COPY scripts/generalUtils/container-test-runner.sh /app/container-test-runner.sh
RUN chmod +x /app/container-test-runner.sh

ENTRYPOINT ["/app/container-test-runner.sh"]
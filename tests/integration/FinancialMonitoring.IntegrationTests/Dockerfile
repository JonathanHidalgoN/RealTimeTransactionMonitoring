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

CMD ["sh", "-c", "dotnet test tests/integration/FinancialMonitoring.IntegrationTests/FinancialMonitoring.IntegrationTests.csproj --no-build -c Release --logger \"console;verbosity=${TEST_VERBOSITY:-normal}\" --results-directory /app/test-results ${TEST_CATEGORY:+--filter Category=$TEST_CATEGORY}"]
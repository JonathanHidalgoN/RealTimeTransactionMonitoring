FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy solution file
COPY FinancialMonitoringSolution.sln .

# Copy project files
COPY src/FinancialMonitoring.Models/*.csproj src/FinancialMonitoring.Models/
COPY src/FinancialMonitoring.Abstractions/*.csproj src/FinancialMonitoring.Abstractions/
COPY src/FinancialMonitoring.Api/*.csproj src/FinancialMonitoring.Api/
COPY tests/FinancialMonitoring.IntegrationTests/*.csproj tests/FinancialMonitoring.IntegrationTests/

# Restore packages
RUN dotnet restore tests/FinancialMonitoring.IntegrationTests/FinancialMonitoring.IntegrationTests.csproj

# Copy source code
COPY src/FinancialMonitoring.Models/ src/FinancialMonitoring.Models/
COPY src/FinancialMonitoring.Abstractions/ src/FinancialMonitoring.Abstractions/
COPY src/FinancialMonitoring.Api/ src/FinancialMonitoring.Api/
COPY tests/FinancialMonitoring.IntegrationTests/ tests/FinancialMonitoring.IntegrationTests/

# Build
RUN dotnet build tests/FinancialMonitoring.IntegrationTests/FinancialMonitoring.IntegrationTests.csproj -c Release --no-restore

# Test
FROM build AS test
WORKDIR /source
ENTRYPOINT ["dotnet", "test", "tests/FinancialMonitoring.IntegrationTests/FinancialMonitoring.IntegrationTests.csproj", "--no-build", "-c", "Release", "--logger", "trx", "--results-directory", "/app/test-results", "--filter", "FullyQualifiedName~BasicConnectivityTests"]
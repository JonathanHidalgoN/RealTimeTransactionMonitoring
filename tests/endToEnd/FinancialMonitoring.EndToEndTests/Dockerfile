FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy solution file
COPY FinancialMonitoringSolution.sln .

# Copy project files
COPY src/FinancialMonitoring.Models/*.csproj src/FinancialMonitoring.Models/
COPY src/FinancialMonitoring.Abstractions/*.csproj src/FinancialMonitoring.Abstractions/
COPY src/FinancialMonitoring.Api/*.csproj src/FinancialMonitoring.Api/
COPY tests/endToEnd/FinancialMonitoring.EndToEndTests/*.csproj tests/endToEnd/FinancialMonitoring.EndToEndTests/

# Restore packages
RUN dotnet restore tests/endToEnd/FinancialMonitoring.EndToEndTests/FinancialMonitoring.EndToEndTests.csproj

# Copy source code
COPY src/FinancialMonitoring.Models/ src/FinancialMonitoring.Models/
COPY src/FinancialMonitoring.Abstractions/ src/FinancialMonitoring.Abstractions/
COPY src/FinancialMonitoring.Api/ src/FinancialMonitoring.Api/
COPY tests/endToEnd/FinancialMonitoring.EndToEndTests/ tests/endToEnd/FinancialMonitoring.EndToEndTests/

# Build
RUN dotnet build tests/endToEnd/FinancialMonitoring.EndToEndTests/FinancialMonitoring.EndToEndTests.csproj -c Release --no-restore

# Test
FROM build AS test
WORKDIR /source

CMD ["sh", "-c", "dotnet test tests/endToEnd/FinancialMonitoring.EndToEndTests/FinancialMonitoring.EndToEndTests.csproj --no-build -c Release --logger \"console;verbosity=${TEST_VERBOSITY:-normal}\" --results-directory /app/test-results ${TEST_CATEGORY:+--filter Category=$TEST_CATEGORY}"]

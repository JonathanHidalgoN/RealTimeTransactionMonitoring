#!/bin/bash

set -e

echo "Starting Test Suite..."

echo "Building test environment..."
docker compose -f docker-compose.test.yml build

echo "Running Unit Tests..."
dotnet test tests/unit/FinancialMonitoring.Models.Tests/FinancialMonitoring.Models.Tests.csproj --configuration Release --logger "console;verbosity=minimal"
dotnet test tests/unit/FinancialMonitoring.Api.Tests/FinancialMonitoring.Api.Tests.csproj --configuration Release --logger "console;verbosity=minimal"
dotnet test tests/unit/TransactionProcessor.Tests/TransactionProcessor.Tests.csproj --configuration Release --logger "console;verbosity=minimal"
dotnet test tests/unit/TransactionSimulator.Tests/TransactionSimulator.Tests.csproj --configuration Release --logger "console;verbosity=minimal"

echo "Running Integration Tests (WebApplicationFactory - fast)..."
dotnet test tests/integration/FinancialMonitoring.IntegrationTests/FinancialMonitoring.IntegrationTests.csproj --configuration Release --logger "console;verbosity=minimal"

echo "Starting end-to-end test environment..."
docker compose -f docker-compose.test.yml up -d

echo "Waiting for services to be ready..."
sleep 120

echo "Running End-to-End Tests (full infrastructure - slow)..."
docker compose -f docker-compose.test.yml run --rm integration-tests

echo "Cleaning up..."
docker compose -f docker-compose.test.yml down -v

echo "All tests completed successfully!"

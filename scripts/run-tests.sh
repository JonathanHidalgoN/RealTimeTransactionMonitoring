#!/bin/bash

set -e

echo "Starting Integration Tests..."

echo "Building test environment..."
docker compose -f docker-compose.test.yml build

echo "Running Unit Tests..."
dotnet test tests/unit/FinancialMonitoring.Models.Tests/FinancialMonitoring.Models.Tests.csproj --configuration Release --logger "console;verbosity=minimal"
dotnet test tests/unit/FinancialMonitoring.Api.Tests/FinancialMonitoring.Api.Tests.csproj --configuration Release --logger "console;verbosity=minimal"
dotnet test tests/unit/TransactionProcessor.Tests/TransactionProcessor.Tests.csproj --configuration Release --logger "console;verbosity=minimal"
dotnet test tests/unit/TransactionSimulator.Tests/TransactionSimulator.Tests.csproj --configuration Release --logger "console;verbosity=minimal"

echo "Starting integration test environment..."
docker compose -f docker-compose.test.yml up -d

echo "Waiting for services to be ready..."
sleep 120

echo "Running Integration Tests..."
docker compose -f docker-compose.test.yml run --rm integration-tests

echo "Cleaning up..."
docker compose -f docker-compose.test.yml down -v

echo "All tests completed successfully!"

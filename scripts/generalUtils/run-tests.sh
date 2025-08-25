#!/bin/bash

set -e

echo "Starting Integration and Load Tests..."

echo "Building test environment..."
docker compose -f docker-compose.test.yml build

echo "Running Unit Tests..."
dotnet test --configuration Release --logger "console;verbosity=minimal"

echo "Starting integration test environment..."
docker compose -f docker-compose.test.yml up -d

echo "Waiting for services to be ready..."
sleep 60

echo "Running Integration Tests..."
docker compose -f docker-compose.test.yml run --rm integration-tests

echo "Running Load Tests..."
docker compose -f docker-compose.test.yml run --rm load-tests

echo "Cleaning up..."
docker compose -f docker-compose.test.yml down -v

echo "All tests completed successfully!"

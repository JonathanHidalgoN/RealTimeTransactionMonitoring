#!/bin/bash
# Build and Push Docker Images
# This script builds all Docker images and pushes them to Azure Container Registry

set -e

if [ -z "$1" ]; then
    echo "ERROR: ACR server address required"
    echo "Usage: $0 <acr_login_server>"
    echo "Example: $0 finmonacr123.azurecr.io"
    exit 1
fi

ACR_SERVER="$1"

echo "Building and pushing Docker images to: $ACR_SERVER"
echo ""

echo "Logging into ACR..."
az acr login --name "${ACR_SERVER%%.*}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$PROJECT_ROOT"

echo "Project root: $PROJECT_ROOT"
echo ""

echo "[1/3] Building API image..."
docker build \
    -t "$ACR_SERVER/financialmonitoring-api:latest" \
    -f src/FinancialMonitoring.Api/Dockerfile \
    .

echo "[1/3] Pushing API image..."
docker push "$ACR_SERVER/financialmonitoring-api:latest"
echo ""

echo "[2/3] Building Processor image..."
docker build \
    -t "$ACR_SERVER/transactionprocessor:latest" \
    -f src/TransactionProcessor/Dockerfile \
    .

echo "[2/3] Pushing Processor image..."
docker push "$ACR_SERVER/transactionprocessor:latest"
echo ""

echo "[3/3] Building Simulator image..."
docker build \
    -t "$ACR_SERVER/transactionsimulator:latest" \
    -f src/TransactionSimulator/Dockerfile \
    .

echo "[3/3] Pushing Simulator image..."
docker push "$ACR_SERVER/transactionsimulator:latest"
echo ""

echo "SUCCESS: All images built and pushed to ACR"
echo ""
echo "Images available:"
echo "  - $ACR_SERVER/financialmonitoring-api:latest"
echo "  - $ACR_SERVER/transactionprocessor:latest"
echo "  - $ACR_SERVER/transactionsimulator:latest"

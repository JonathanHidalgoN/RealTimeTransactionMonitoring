#!/bin/bash
# Build and Push Docker Images
# This script builds all Docker images and pushes them to Azure Container Registry

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

if [ -z "$1" ]; then
    echo -e "${RED}ERROR: ACR server address required${NC}"
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

echo -e "${YELLOW}[1/3] Building API image...${NC}"
docker build \
    -t "$ACR_SERVER/financialmonitoring-api:latest" \
    -f src/FinancialMonitoring.Api/Dockerfile \
    .

echo -e "${YELLOW}[1/3] Pushing API image...${NC}"
docker push "$ACR_SERVER/financialmonitoring-api:latest"
echo ""

echo -e "${YELLOW}[2/3] Building Processor image...${NC}"
docker build \
    -t "$ACR_SERVER/transactionprocessor:latest" \
    -f src/TransactionProcessor/Dockerfile \
    .

echo -e "${YELLOW}[2/3] Pushing Processor image...${NC}"
docker push "$ACR_SERVER/transactionprocessor:latest"
echo ""

echo -e "${YELLOW}[3/3] Building Simulator image...${NC}"
docker build \
    -t "$ACR_SERVER/transactionsimulator:latest" \
    -f src/TransactionSimulator/Dockerfile \
    .

echo -e "${YELLOW}[3/3] Pushing Simulator image...${NC}"
docker push "$ACR_SERVER/transactionsimulator:latest"
echo ""

echo -e "${GREEN}SUCCESS: All images built and pushed to ACR${NC}"
echo ""
echo "Images available:"
echo "  - $ACR_SERVER/financialmonitoring-api:latest"
echo "  - $ACR_SERVER/transactionprocessor:latest"
echo "  - $ACR_SERVER/transactionsimulator:latest"

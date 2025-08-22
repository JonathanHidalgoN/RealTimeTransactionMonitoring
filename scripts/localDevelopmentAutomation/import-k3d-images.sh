#!/bin/bash
# Purpose: Imports locally built Docker images into the k3d cluster.

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

echo -e "${CYAN}======================================================${NC}"
echo -e "${CYAN}  Importing Local Docker Images to k3d Cluster        ${NC}"
echo -e "${CYAN}======================================================${NC}"
echo ""

TAG="latest"
echo -e "${GREEN}✓ Using tag for local images: ${TAG}${NC}"

declare -A localImages
localImages["financialmonitoring-api"]="src/FinancialMonitoring.Api/Dockerfile.dev"
localImages["transactionprocessor"]="src/TransactionProcessor/Dockerfile.dev"
localImages["transactionsimulator"]="src/TransactionSimulator/Dockerfile.dev"
localImages["webapp"]="src/FinancialMonitoring.WebApp/Dockerfile"

echo -e "${YELLOW}--- Importing local images into k3d cluster ---${NC}"
for localImage in "${!localImages[@]}"; do
    IMAGE_NAME="${REGISTRY_PREFIX}${localImage}:${TAG}"
    echo -e "${CYAN}Importing ${IMAGE_NAME}...${NC}"
    k3d image import "${IMAGE_NAME}" --cluster finmon-local
    echo -e "${GREEN}✓ Successfully imported ${IMAGE_NAME}.${NC}"
done

echo -e "${GREEN}  All specified images are now available in k3d!      ${NC}"
echo ""

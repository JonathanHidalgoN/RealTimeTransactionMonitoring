#!/bin/bash
# Purpose: Builds Docker images for local development, tagging them appropriately.

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

echo -e "${CYAN}======================================================${NC}"
echo -e "${CYAN}  Building Local Docker Images for Development        ${NC}"
echo -e "${CYAN}======================================================${NC}"
echo ""

TAG="latest"
echo -e "${GREEN}✓ Using tag for local images build: ${TAG}${NC}"

declare -A localImages
localImages["financialmonitoring-api"]="src/FinancialMonitoring.Api/Dockerfile.dev"
localImages["transactionprocessor"]="src/TransactionProcessor/Dockerfile.dev"
localImages["transactionsimulator"]="src/TransactionSimulator/Dockerfile.dev"
localImages["webapp"]="src/FinancialMonitoring.WebApp/Dockerfile"

for localImage in "${!localImages[@]}"; do
    IMAGE_NAME="${localImage}:${TAG}"
    DOCKERFILE_PATH="${localImages[$localImage]}"

    echo -e "\n${YELLOW}--- Building ${localImage}:${TAG} ---${NC}"
    echo -e "${CYAN}Image: ${IMAGE_NAME}${NC}"
    echo -e "${CYAN}Dockerfile: ${DOCKERFILE_PATH}${NC}"

    docker build -t "${IMAGE_NAME}" -f "${DOCKERFILE_PATH}" .
    echo -e "${GREEN}✓ Successfully built ${localImage}:${TAG}.${NC}"
done

echo -e "${GREEN}  All local Docker images built successfully!         ${NC}"
echo ""

#!/bin/bash
set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${YELLOW}--- Building and Pushing Production Docker Images to ACR ---${NC}"

echo "Fetching ACR details from Terraform state..."
ACR_LOGIN_SERVER=$(cd ./infra && terraform output -raw acr_login_server)
ACR_NAME=$(cd ./infra && terraform output -raw acr_name)

if [ -z "$ACR_LOGIN_SERVER" ]; then
    echo "Error: Could not retrieve ACR details from Terraform output. Please run 'terraform apply' first." >&2
    exit 1
fi
echo "Found ACR: ${ACR_LOGIN_SERVER}"

echo "Logging into ACR..."
az acr login --name "${ACR_NAME}"
echo -e "${GREEN}✓ Docker is now authenticated with ACR.${NC}"

TAG="latest"
echo "Using tag: ${TAG}"

declare -A services
services["financialmonitoring-api"]="src/FinancialMonitoring.Api/Dockerfile"
services["transactionprocessor"]="src/TransactionProcessor/Dockerfile"
services["transactionsimulator"]="src/TransactionSimulator/Dockerfile"

for service in "${!services[@]}"; do
    IMAGE_NAME="${ACR_LOGIN_SERVER}/${service}:${TAG}"
    DOCKERFILE_PATH="${services[$service]}"

    echo -e "\n${YELLOW}--- Building ${service} ---${NC}"
    echo "Image: ${IMAGE_NAME}"
    echo "Dockerfile: ${DOCKERFILE_PATH}"

    docker build -t "${IMAGE_NAME}" -f "${DOCKERFILE_PATH}" .

    echo -e "\n${YELLOW}--- Pushing ${service} ---${NC}"
    docker push "${IMAGE_NAME}"

    echo -e "${GREEN}✓ Successfully pushed ${service}:${TAG}${NC}"
done

echo -e "\n${GREEN}All images have been successfully pushed to ACR!${NC}"

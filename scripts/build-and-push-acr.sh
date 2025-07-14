#!/bin/bash
# Purpose: Build and push Docker images to Azure Container Registry
# Builds: API, TransactionProcessor, TransactionSimulator containers

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

echo -e "${YELLOW}Building & Pushing Docker Images to ACR${NC}"

echo -e "\n${CYAN}--- Step 1: Fetching resource details from Terraform state ---${NC}"
ACR_LOGIN_SERVER=$(cd ./infra && terraform output -raw acr_login_server)
ACR_NAME=$(cd ./infra && terraform output -raw acr_name)
AKS_CLUSTER_NAME=$(cd ./infra && terraform output -raw aks_cluster_name 2>/dev/null || echo "")

if [ -z "$ACR_LOGIN_SERVER" ]; then
    echo "ERROR: Could not retrieve ACR details from Terraform. Please run 'terraform apply' first in the './infra' directory." >&2
    exit 1
fi
echo "Found ACR: ${ACR_LOGIN_SERVER}"
if [ -n "$AKS_CLUSTER_NAME" ]; then
    echo "Found AKS Cluster: ${AKS_CLUSTER_NAME}"
fi

echo -e "\n${CYAN}--- Step 2: Authenticating local Docker client with ACR ---${NC}"
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

echo -e "\n${GREEN}✓ Images Pushed Successfully${NC}"
echo "ACR: ${ACR_LOGIN_SERVER}"
echo ""
echo "Next steps: Update k8s manifests with ACR name and deploy to AKS"

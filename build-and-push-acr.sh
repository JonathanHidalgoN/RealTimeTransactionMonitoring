#!/bin/bash
set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

echo -e "${YELLOW}======================================================${NC}"
echo -e "${YELLOW}    Build & Push Production Docker Images to ACR      ${NC}"
echo -e "${YELLOW}======================================================${NC}"
echo ""
echo -e "${YELLOW}Prerequisite:${NC} You must have run 'terraform apply' to ensure the ACR exists."
echo ""

echo -e "\n${CYAN}--- Step 1: Fetching ACR details from Terraform state ---${NC}"
ACR_LOGIN_SERVER=$(cd ./infra && terraform output -raw acr_login_server)
ACR_NAME=$(cd ./infra && terraform output -raw acr_name)

if [ -z "$ACR_LOGIN_SERVER" ]; then
    echo "ERROR: Could not retrieve ACR details from Terraform. Please run 'terraform apply' first in the './infra' directory." >&2
    exit 1
fi
echo "Found ACR: ${ACR_LOGIN_SERVER}"

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

echo -e "\n${YELLOW}============================================${NC}"
echo -e "${YELLOW}         Images Pushed - Ready to Deploy      ${NC}"
echo -e "${YELLOW}============================================${NC}"
echo ""
echo "Your production Docker images have been successfully pushed to your Azure Container Registry."
echo "You are now ready to deploy your application to your live AKS cluster."
echo ""
echo -e "${CYAN}--- Next Steps: Deploy Your Application to Azure Kubernetes Service (AKS) ---${NC}"
echo ""
echo -e "${CYAN}1. Start your AKS Cluster (if it's stopped):${NC}"
echo "   AKS clusters can be stopped to save costs. Ensure it's running before deploying."
echo -e "   Run: ${GREEN}az aks start --name \"finmon-aks\" --resource-group \"finmon-rg\"${NC}"
echo ""
echo -e "${CYAN}2. Connect kubectl to Your AKS Cluster:${NC}"
echo "   This command updates your local kubeconfig file to point to your Azure cluster."
echo -e "   Run: ${GREEN}az aks get-credentials --resource-group \"finmon-rg\" --name \"finmon-aks\" --overwrite-existing${NC}"
echo ""
echo -e "${CYAN}3. Deploy Your Application Manifests:${NC}"
echo "   Navigate to your Kubernetes manifest directory and apply the configuration using Kustomize."
echo -e "   Run: ${GREEN}cd k8s-manifests${NC}"
echo "   Run: ${GREEN}kubectl apply -k .${NC}"
echo ""
echo -e "${CYAN}4. Verify the Deployment:${NC}"
echo "   Watch your application pods start up. Wait for them to reach the 'Running' state."
echo -e "   Run: ${GREEN}kubectl get pods -n finmon-app -w${NC}"
echo ""
echo "   Get the public IP address for your API (it may take a few minutes for the 'EXTERNAL-IP' to appear):"
echo -e "   Run: ${GREEN}kubectl get service api-service -n finmon-app${NC}"
echo ""
echo -e "${GREEN}Once you have the external IP, your application is live on Azure!${NC}"
echo ""

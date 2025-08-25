#!/bin/bash
# Purpose: Update Kubernetes manifests with actual ACR name from Terraform

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${YELLOW}Updating Kubernetes manifests with ACR name and client ID${NC}"

ACR_NAME=$(cd infra && terraform output -raw acr_name)
CLIENT_ID=$(cd infra && terraform output -raw app_managed_identity_client_id)
if [ -z "$ACR_NAME" ] || [ -z "$CLIENT_ID" ]; then
    echo "Error: Could not get required values from Terraform"
    exit 1
fi

echo "ACR Name: $ACR_NAME"
echo "Client ID: $CLIENT_ID"

for file in k8s-manifest/02-processor-deployment.yml k8s-manifest/03-api-deployment-service.yml k8s-manifest/04-simulator-deployment.yml; do
    if [ -f "$file" ]; then
        sed -i "s|image: [^/]*.azurecr.io/|image: ${ACR_NAME}.azurecr.io/|g" "$file"
        echo "Updated $file with ACR name"
    fi
done

SERVICE_ACCOUNT_FILE="k8s-manifest/00-serviceaccount.yml"
if [ -f "$SERVICE_ACCOUNT_FILE" ]; then
    sed -i "s|azure.workload.identity/client-id: \"[^\"]*\"|azure.workload.identity/client-id: \"${CLIENT_ID}\"|g" "$SERVICE_ACCOUNT_FILE"
    echo "Updated $SERVICE_ACCOUNT_FILE with client ID"
fi

echo -e "${GREEN}Kubernetes manifests updated with ACR name and client ID${NC}"

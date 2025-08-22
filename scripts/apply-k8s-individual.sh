#!/bin/bash
# Purpose: Applies Kubernetes manifests individually using kubectl apply -f.
#          Ensures resources are applied in a logical order.

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

echo -e "${CYAN}======================================================${NC}"
echo -e "${CYAN}  Applying Kubernetes Manifests Individually          ${NC}"
echo -e "${CYAN}======================================================${NC}"
echo ""

K8S_MANIFEST_DIR="k8s-manifest"

# Define the order of files to apply
declare -a files_to_apply=(
    "${K8S_MANIFEST_DIR}/01-namespace.yml"
    "${K8S_MANIFEST_DIR}/06-env-configmap.yml"
    "${K8S_MANIFEST_DIR}/02-processor-deployment.yml"
    "${K8S_MANIFEST_DIR}/04-simulator-deployment.yml"
    "${K8S_MANIFEST_DIR}/03-api-deployment-service.yml"
)

for file in "${files_to_apply[@]}"; do
    if [ -f "$file" ]; then
        echo -e "${YELLOW}--- Applying ${file} ---${NC}"
        kubectl apply -f "$file"
        echo -e "${GREEN}✓ Successfully applied ${file}.${NC}"
    else
        echo -e "${RED}Error: File not found: ${file}${NC}" >&2
        exit 1
    fi
done

echo -e "${GREEN}  All Kubernetes Manifests Applied Successfully!      ${NC}"
echo ""

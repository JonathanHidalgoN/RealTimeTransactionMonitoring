#!/bin/bash
# Purpose: Applies Kubernetes manifests for local development using Kustomize overlay.
#          Uses the local overlay to apply all resources with proper local configuration.

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${CYAN}======================================================${NC}"
echo -e "${CYAN}  Applying Local Kubernetes Manifests via Kustomize   ${NC}"
echo -e "${CYAN}======================================================${NC}"
echo ""

KUSTOMIZE_OVERLAY_PATH="k8s-manifest/overlays/local"

# Check if the kustomize overlay directory exists
if [ ! -d "$KUSTOMIZE_OVERLAY_PATH" ]; then
    echo -e "${RED}Error: Kustomize overlay directory not found: ${KUSTOMIZE_OVERLAY_PATH}${NC}" >&2
    exit 1
fi

echo -e "${YELLOW}--- Applying Kustomize overlay: ${KUSTOMIZE_OVERLAY_PATH} ---${NC}"
echo ""

kubectl apply -k "$KUSTOMIZE_OVERLAY_PATH"

echo ""
echo -e "${GREEN}✓ Successfully applied all local Kubernetes manifests!${NC}"
echo ""
echo -e "${CYAN}Resources deployed:${NC}"
kubectl get all -n finmon-app

echo ""
echo -e "${GREEN}  Local Kubernetes Environment Ready!                 ${NC}"
echo ""

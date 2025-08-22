#!/bin/bash
set -e

YELLOW='\033[1;33m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
NC='\033[0m'

echo -e "${CYAN}======================================================${NC}"
echo -e "${CYAN}  Creating Local k3d Cluster for Development          ${NC}"
echo -e "${CYAN}======================================================${NC}"
echo ""

if [ -n "$LOCAL_K3D_LOAD_BALANCER_PORT" ]; then
    HOST_PORT=$LOCAL_K3D_LOAD_BALANCER_PORT
    echo -e "${GREEN}✓ Using custom port '${HOST_PORT}' from environment variable
      LOCAL_K3D_LOAD_BALANCER_PORT.${NC}"
else
    HOST_PORT=8081
    echo -e "${YELLOW}Warning: LOCAL_K3D_LOAD_BALANCER_PORT not set.${NC}"
    echo -e "${YELLOW}Defaulting to port '${HOST_PORT}' for the load balancer.${NC}"
fi
echo ""

echo -e "${YELLOW}--- Deleting existing 'finmon-local' cluster (if it exists) ---
      ${NC}"
k3d cluster delete finmon-local || true
echo -e "${GREEN}✓ Cleanup complete.${NC}"
echo ""

echo -e "${YELLOW}--- Creating new 3-node cluster 'finmon-local' ---${NC}"
echo -e "${CYAN}Mapping host port ${HOST_PORT} to cluster ingress port 80.${NC}"
k3d cluster create finmon-local -p "${HOST_PORT}:80@loadbalancer" --agents 2
echo ""

echo -e "${GREEN}✓ Cluster 'finmon-local' created successfully!${NC}"
echo ""
echo -e "You can now verify the nodes with: ${CYAN}kubectl get nodes${NC}"

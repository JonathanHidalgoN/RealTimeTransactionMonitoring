#!/bin/bash
# Purpose: Install NGINX Ingress Controller for AKS
# Provides: External load balancer, API routing, SSL certificate support

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

echo -e "${YELLOW}Installing NGINX Ingress Controller${NC}"

kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.8.2/deploy/static/provider/cloud/deploy.yaml

echo -e "${GREEN}✓ NGINX Ingress Controller deployment started${NC}"
echo ""

echo -e "${CYAN}--- Step 2: Waiting for LoadBalancer IP assignment ---${NC}"
echo "Waiting for external IP to be assigned (this may take 2-3 minutes)..."

# Wait for LoadBalancer to get external IP
for i in {1..60}; do
    EXTERNAL_IP=$(kubectl get service ingress-nginx-controller -n ingress-nginx -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null)
    if [ -n "$EXTERNAL_IP" ]; then
        echo -e "${GREEN}✓ LoadBalancer IP assigned: ${EXTERNAL_IP}${NC}"
        break
    fi
    echo "  Waiting... (${i}/60)"
    sleep 5
done

if [ -z "$EXTERNAL_IP" ]; then
    echo -e "${YELLOW} LoadBalancer IP not yet assigned${NC}"
    echo "Check status with: kubectl get service -n ingress-nginx"
    EXTERNAL_IP="<pending>"
fi

echo ""
echo -e "${GREEN}✓ Ingress Controller Setup Complete${NC}"

if [ "$EXTERNAL_IP" != "<pending>" ]; then
    echo "LoadBalancer IP: ${EXTERNAL_IP}"
    echo "Update DNS A record: api.finmon-ui-azj.com -> ${EXTERNAL_IP}"
else
    echo "LoadBalancer IP still pending. Check: kubectl get service -n ingress-nginx"
fi


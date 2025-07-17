#!/bin/bash
# Purpose: Install cert-manager for automatic SSL certificates
# Provides: Let's Encrypt SSL certificates for HTTPS

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

echo -e "${YELLOW}Installing cert-manager for SSL certificates${NC}"

echo -e "${CYAN}--- Step 1: Installing cert-manager ---${NC}"
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.2/cert-manager.yaml

echo -e "${GREEN}✓ cert-manager installed${NC}"
echo ""

echo -e "${CYAN}--- Step 2: Waiting for cert-manager to be ready ---${NC}"
kubectl wait --for=condition=Available --timeout=300s deployment/cert-manager -n cert-manager
kubectl wait --for=condition=Available --timeout=300s deployment/cert-manager-cainjector -n cert-manager
kubectl wait --for=condition=Available --timeout=300s deployment/cert-manager-webhook -n cert-manager

echo -e "${GREEN}✓ cert-manager is ready${NC}"
echo ""

echo -e "${CYAN}--- Step 3: Creating Let's Encrypt ClusterIssuer ---${NC}"
cat <<EOF | kubectl apply -f -
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-prod
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: admin@finmon-ui-azj.com
    privateKeySecretRef:
      name: letsencrypt-prod
    solvers:
    - http01:
        ingress:
          class: nginx
EOF

echo -e "${GREEN}✓ ClusterIssuer created${NC}"
echo ""

echo -e "${CYAN}--- Step 4: Triggering certificate generation ---${NC}"
kubectl delete secret api-tls-secret -n finmon-app --ignore-not-found
kubectl annotate ingress api-ingress -n finmon-app cert-manager.io/cluster-issuer=letsencrypt-prod --overwrite

echo -e "${GREEN}✓ Certificate generation triggered${NC}"
echo ""

echo -e "${GREEN}✓ cert-manager Setup Complete${NC}"
echo ""
echo "Certificate generation triggered. Check status:"
echo "  kubectl get certificaterequests -n finmon-app"
echo "  kubectl get secrets api-tls-secret -n finmon-app"
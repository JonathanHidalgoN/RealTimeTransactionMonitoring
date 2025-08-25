#!/bin/bash
# Purpose: Restart Azure Workload Identity webhook to resolve authentication issues
# This fixes cases where workload identity authentication fails after cluster operations

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${YELLOW}Restarting Azure Workload Identity webhook${NC}"

echo "Restarting Azure Workload Identity webhook controller..."
kubectl rollout restart deployment azure-wi-webhook-controller-manager -n kube-system

echo "Waiting for webhook pods to be ready..."
kubectl wait --for=condition=ready pod -l app.kubernetes.io/name=azure-wi-webhook-controller-manager -n kube-system --timeout=60s || echo "Webhook pods may still be starting"

echo "Restarting application pods to pick up fresh workload identity tokens..."
kubectl delete pods --all -n finmon-app

echo -e "${GREEN}Azure Workload Identity webhook restarted${NC}"
echo "Application pods will be recreated with fresh workload identity tokens"


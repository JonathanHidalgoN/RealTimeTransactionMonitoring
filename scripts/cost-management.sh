#!/bin/bash

# Load environment variables
if [ -f .env ]; then
    source .env
fi

RESOURCE_GROUP="${RESOURCE_GROUP_NAME:-$(az group list --query "[?contains(name, 'finmon-rg')].name" -o tsv | head -1)}"
AKS_CLUSTER=$(az aks list --resource-group $RESOURCE_GROUP --query "[0].name" -o tsv 2>/dev/null)
NODE_POOL="default"

if [[ -z "$RESOURCE_GROUP" ]]; then
    echo "No finmon resource group found"
    exit 1
fi

if [[ -z "$AKS_CLUSTER" ]]; then
    echo "No AKS cluster found in resource group $RESOURCE_GROUP"
    exit 1
fi

case "$1" in
"start")
    echo "Starting AKS cluster..."
    az aks start --name $AKS_CLUSTER --resource-group $RESOURCE_GROUP
    echo "AKS cluster started"
    ;;

"stop")
    echo "Stopping AKS"
    az aks stop --name $AKS_CLUSTER --resource-group $RESOURCE_GROUP
    echo "AKS cluster stopped"
    ;;

"work")
    echo "Scaling for development work (1 node)..."
    az aks nodepool scale --cluster-name $AKS_CLUSTER --name $NODE_POOL --node-count 1 --resource-group $RESOURCE_GROUP
    echo "Re-enabling autoscaling..."
    az aks nodepool update --cluster-name $AKS_CLUSTER --name $NODE_POOL --resource-group $RESOURCE_GROUP --enable-cluster-autoscaler --min-count 1 --max-count 2
    echo "Scaled to 1 node for development"
    ;;

"pause")
    echo "Scaling to 0 nodes (maximum cost savings)..."
    echo "This will stop all running applications"
    read -p "Continue? (y/N): " confirm
    if [[ "$confirm" =~ ^[Yy]$ ]]; then
        echo "Temporarily disabling autoscaling..."
        az aks nodepool update --cluster-name $AKS_CLUSTER --name $NODE_POOL --resource-group $RESOURCE_GROUP --disable-cluster-autoscaler
        echo "Scaling to 0 nodes..."
        az aks nodepool scale --cluster-name $AKS_CLUSTER --name $NODE_POOL --node-count 0 --resource-group $RESOURCE_GROUP
        echo "Use './cost-management.sh work' to scale back up"
    else
        echo "Cancelled"
    fi
    ;;

"demo")
    echo "Scaling for demo(2 nodes)..."
    az aks nodepool scale --cluster-name $AKS_CLUSTER --name $NODE_POOL --node-count 2 --resource-group $RESOURCE_GROUP
    echo "Scaled to 2 nodes"
    ;;

"status")
    echo "AKS Status:"
    az aks show --name $AKS_CLUSTER --resource-group $RESOURCE_GROUP --query "powerState.code" -o tsv
    echo "Node Count:"
    az aks nodepool show --cluster-name $AKS_CLUSTER --name $NODE_POOL --resource-group $RESOURCE_GROUP --query "count" -o tsv
    ;;

*)
    echo "Usage: $0 [start|stop|status|work|pause|demo]"
    echo ""
    echo "Commands:"
    echo "  start  - Start the AKS cluster"
    echo "  stop   - Stop the AKS cluster completely"
    echo "  work   - Scale to 1 node for development"
    echo "  pause  - Scale to 0 nodes (maximum savings)"
    echo "  demo   - Scale to 2 nodes for demonstrations"
    echo "  status - Show current cluster status"
    echo ""
    ;;
esac

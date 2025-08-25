#!/bin/bash
# Purpose: Update ConfigMap with live infrastructure values from Terraform
# Automates: ConfigMap population with connection strings and endpoints

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${YELLOW}Updating ConfigMap with Terraform values${NC}"

cd infra
COSMOS_URI=$(terraform output -raw cosmosdb_endpoint)
COSMOS_KEY=$(terraform output -raw cosmosdb_primary_key)
EH_CS=$(terraform output -raw eventhubs_namespace_connection_string)
AI_CS=$(terraform output -raw application_insights_connection_string)
KV_URI=$(terraform output -raw key_vault_uri)
CLIENT_ID=$(terraform output -raw app_managed_identity_client_id)
cd ..

if [ -z "$COSMOS_URI" ] || [ -z "$EH_CS" ] || [ -z "$AI_CS" ] || [ -z "$KV_URI" ] || [ -z "$CLIENT_ID" ]; then
    echo "Error: Could not get required values from Terraform"
    exit 1
fi

echo "Updating service account with correct client ID..."
kubectl annotate serviceaccount finmon-app-sa -n finmon-app azure.workload.identity/client-id="$CLIENT_ID" --overwrite

kubectl patch configmap env-config -n finmon-app --type merge -p "{
  \"data\": {
    \"CosmosDb__EndpointUri\": \"$COSMOS_URI\",
    \"CosmosDb__PrimaryKey\": \"$COSMOS_KEY\",
    \"EventHubs__ConnectionString\": \"$EH_CS\",
    \"ApplicationInsights__ConnectionString\": \"$AI_CS\",
    \"KEY_VAULT_URI\": \"$KV_URI\",
    \"AZURE_CLIENT_ID\": \"$CLIENT_ID\"
  }
}"

echo -e "${GREEN}ConfigMap updated with live infrastructure values${NC}"


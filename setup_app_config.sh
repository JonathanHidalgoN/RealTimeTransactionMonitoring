#!/bin/bash
set -e

YELLOW='\033[1;33m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

echo -e "${YELLOW}======================================================${NC}"
echo -e "${YELLOW}     Application Identity & Secrets Setup Script      ${NC}"
echo -e "${YELLOW}======================================================${NC}"
echo ""
echo "This script will:"
echo "1. Fetch live infrastructure details (URIs, Keys, etc.) from Terraform."
echo "2. Create/update a Service Principal (SP) for the application."
echo "3. Grant the SP access to Key Vault and populate it with secrets."
echo "4. Generate a complete '.env' file for Docker Compose to run the application."
echo ""
echo -e "${YELLOW}Prerequisite:${NC} You must have already successfully run 'terraform apply'."
echo ""

read -p "Do you want to continue? (yes/no): " confirm
if [[ $confirm != "yes" ]]; then
    echo "Operation cancelled by user."
    exit 1
fi

echo -e "\n${YELLOW}--- Step 1: Verifying Prerequisites & Loading Config ---${NC}"
SECRETS_ENV_FILE=".env"
if [ ! -f "$SECRETS_ENV_FILE" ]; then
    echo -e "${YELLOW}Error: Configuration file '${SECRETS_ENV_FILE}' not found.${NC}" >&2
    echo "Please create a '${SECRETS_ENV_FILE}' file in the project root." >&2
    exit 1
fi
source "./${SECRETS_ENV_FILE}"
echo -e "${GREEN}✓ Successfully loaded static config from '${SECRETS_ENV_FILE}'.${NC}"

if ! az account show >/dev/null 2>&1; then
    echo "ERROR: Not logged into Azure. Please run 'az login' first." >&2
    exit 1
fi
echo "Running script as Azure user: $(az account show --query "user.name" -o tsv)"

echo -e "\n${YELLOW}--- Step 2: Fetching Live Infrastructure Details from Terraform ---${NC}"
cd ./infra
KV_NAME=$(terraform output -raw key_vault_name)
KV_ID=$(terraform output -raw key_vault_id)
KV_URI=$(terraform output -raw key_vault_uri)
AI_CS=$(terraform output -raw application_insights_connection_string)
COSMOS_URI=$(terraform output -raw cosmosdb_endpoint)
COSMOS_KEY=$(terraform output -raw cosmosdb_primary_key)
EH_CS=$(terraform output -raw eventhubs_namespace_connection_string)
EH_STORAGE_CS=$(terraform output -raw eventhub_checkpoint_storage_connection_string)
cd ..
echo -e "${GREEN}✓ Successfully retrieved infrastructure details from Terraform state.${NC}"

# --- Step 3: Create/Update Application Service Principal ---
echo -e "\n${YELLOW}--- Step 3: Creating/Updating Service Principal for the Application ---${NC}"
APP_SP_NAME="FinMonAppSP"
SP_APP_ID=$(az ad sp list --display-name "${APP_SP_NAME}" --query "[0].appId" -o tsv)

if [ -n "$SP_APP_ID" ]; then
    echo "Found existing Service Principal '${APP_SP_NAME}'. Resetting its credential..."
    SP_JSON_OUTPUT=$(az ad sp credential reset --id "${SP_APP_ID}" --output json)
else
    echo "Creating new Service Principal '${APP_SP_NAME}'..."
    SP_JSON_OUTPUT=$(az ad sp create-for-rbac --name "${APP_SP_NAME}" --skip-assignment --output json --only-show-errors)
fi

SP_APP_ID=$(echo "${SP_JSON_OUTPUT}" | jq -r '.appId')
SP_PASSWORD=$(echo "${SP_JSON_OUTPUT}" | jq -r '.password')
SP_TENANT_ID=$(echo "${SP_JSON_OUTPUT}" | jq -r '.tenant')
echo "Waiting for propagation in Azure AD..."
sleep 20
SP_OBJECT_ID=$(az ad sp show --id "${SP_APP_ID}" --query "id" -o tsv 2>/dev/null)
if [ -z "${SP_OBJECT_ID}" ]; then
    echo "ERROR: Failed to retrieve Object ID for SP." >&2
    exit 1
fi
echo -e "${GREEN}✓ Service Principal is ready.${NC}"

# --- Step 4: Grant SP Access to Key Vault ---
echo -e "\n${YELLOW}--- Step 4: Assigning 'Key Vault Secrets User' Role to SP ---${NC}"
az role assignment create \
    --assignee-object-id "${SP_OBJECT_ID}" \
    --role "Key Vault Secrets User" \
    --scope "${KV_ID}" \
    --assignee-principal-type "ServicePrincipal" \
    --output none \
    --only-show-errors || echo -e "${YELLOW}Warning: Role assignment for SP might already exist. Proceeding.${NC}"
echo -e "${GREEN}✓ Service Principal has been granted permission to read secrets from Key Vault.${NC}"

# --- Step 5: Add/Update Secrets in Key Vault ---
echo -e "\n${YELLOW}--- Step 5: Populating Secrets in Azure Key Vault ---${NC}"
az keyvault secret set --vault-name "$KV_NAME" --name "ApplicationInsights--ConnectionString" --value "$AI_CS" --output none
az keyvault secret set --vault-name "$KV_NAME" --name "CosmosDb--EndpointUri" --value "$COSMOS_URI" --output none
az keyvault secret set --vault-name "$KV_NAME" --name "CosmosDb--PrimaryKey" --value "$COSMOS_KEY" --output none
az keyvault secret set --vault-name "$KV_NAME" --name "EventHubs--ConnectionString" --value "$EH_CS" --output none
az keyvault secret set --vault-name "$KV_NAME" --name "EventHubs--BlobStorageConnectionString" --value "$EH_STORAGE_CS" --output none
echo -e "${GREEN}✓ All application secrets have been set in Key Vault '${KV_NAME}'.${NC}"

# --- Step 6: Generate .env File ---
PROJECT_ENV_FILE=".env"
echo -e "\n${YELLOW}--- Step 6: Creating/Updating '${PROJECT_ENV_FILE}' for Docker Compose ---${NC}"
cat >>"${PROJECT_ENV_FILE}" <<EOF
# This file was auto-generated by the setup_app_config.sh script.
# It contains secrets and configuration for your application environment.
# This file SHOULD be in .gitignore

# --- Azure Key Vault & Application Identity ---
KEY_VAULT_URI="${KV_URI}"
AZURE_CLIENT_ID="${SP_APP_ID}"
AZURE_CLIENT_SECRET="${SP_PASSWORD}"
AZURE_TENANT_ID="${SP_TENANT_ID}"

# --- Non-Secret Application Configuration ---
MESSAGING_PROVIDER="${MESSAGE_PROVIDER}"
CosmosDb__DatabaseName="${COSMOSDB_DATABASE_NAME}"
CosmosDb__ContainerName="${COSMOSDB_CONTAINER_NAME}"
CosmosDb__PartitionKeyPath="${COSMOSDB_PARTITION_KEY_PATH}"
EventHubs__EventHubName="${EVENTHUBS_EVENTHUB_NAME}"
EventHubs__BlobContainerName="${EVENTHUBS_BLOB_CONTAINER_NAME}"
EOF
echo -e "${GREEN}✓ '${PROJECT_ENV_FILE}' created/updated successfully.${NC}"

# --- Final Instructions ---
echo -e "\n${YELLOW}============================================${NC}"
echo -e "${YELLOW}      Application Configuration Complete!     ${NC}"
echo -e "${YELLOW}============================================${NC}"
echo ""
echo "You are now ready to run your application."
echo "The '.env' file has been fully populated with the necessary credentials and configuration."
echo ""
echo -e "${CYAN}Next Step: Run Your Application:${NC}"
echo "From the project root, run: ${GREEN}docker-compose up --build -d${NC}"
echo ""

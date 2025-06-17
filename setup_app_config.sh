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
echo "This script automates setting up the application's Service Principal and"
echo "populating Azure Key Vault with secrets for the local development environment."
echo ""
echo -e "${YELLOW}Prerequisites:${NC}"
echo "1. You must have already run 'terraform apply' to create the base infrastructure."
echo "2. You must create a '.env' file in this directory with the static secret values."
echo "   (This script will fail with instructions if the file is not found)."
echo ""

read -p "Do you want to continue? (yes/no): " confirm
if [[ $confirm != "yes" ]]; then
    echo "Operation cancelled by user."
    exit 1
fi

echo -e "\n${YELLOW}--- Step 1: Verifying Prerequisites & Loading Secrets ---${NC}"

ENV_FILE=".env"
if [ ! -f "$ENV_FILE" ]; then
    echo -e "${YELLOW}Error: Secret values file '${ENV_FILE}' not found.${NC}" >&2
    echo "Please create a file named '${ENV_FILE}' in the project root with the following format:" >&2
    echo "" >&2
    echo -e "${CYAN}# ${ENV_FILE} Contents" >&2
    echo 'KAFKA_BOOTSTRAP_SERVERS=""' >&2
    echo 'COSMOSDB_ENDPOINT_URI="' >&2
    echo 'COSMOSDB_PRIMARY_KEY=""' >&2
    echo 'COSMOSDB_PRIMARY_KEY=""' >&2
    echo "" >&2
    echo "Then add this file to your .gitignore and run this script again." >&2
    exit 1
fi
source "./${ENV_FILE}"
echo -e "${GREEN}✓ Successfully loaded secrets from '${ENV_FILE}'.${NC}"

# Check for Azure Login
if ! az account show >/dev/null 2>&1; then
    echo "ERROR: Not logged into Azure. Please run 'az login' and 'az account set ...' first." >&2
    exit 1
fi
LOGGED_IN_USER=$(az account show --query "user.name" -o tsv)
echo "Running script as Azure user: ${LOGGED_IN_USER}"

# Fetch details from Terraform outputs
echo "Fetching details from Terraform outputs in ./infra ..."
cd ./infra
KV_NAME=$(terraform output -raw key_vault_name)
KV_ID=$(terraform output -raw key_vault_id)
KV_URI=$(terraform output -raw key_vault_uri)
AI_CS=$(terraform output -raw application_insights_connection_string)
cd ..
if [ -z "$KV_NAME" ] || [ -z "$KV_URI" ] || [ -z "$AI_CS" ]; then
    echo "ERROR: Could not retrieve necessary outputs from Terraform. Please ensure 'terraform apply' has been run successfully in the './infra' directory." >&2
    exit 1
fi
echo -e "${GREEN}✓ Successfully retrieved infrastructure details from Terraform state.${NC}"

# --- Step 2: Create/Update Application Service Principal ---
echo -e "\n${YELLOW}--- Step 2: Creating/Updating Service Principal for the Application ---${NC}"
APP_SP_NAME="FinMonAppSP"
SP_APP_ID=$(az ad sp list --display-name "${APP_SP_NAME}" --query "[0].appId" -o tsv)

if [ -n "$SP_APP_ID" ]; then
    echo "Found existing Service Principal '${APP_SP_NAME}'. Resetting its credential to get a new secret..."
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
    echo "ERROR: Failed to retrieve Object ID for Service Principal ${SP_APP_ID}." >&2
    exit 1
fi
echo -e "${GREEN}✓ Service Principal is ready.${NC}"

# --- Step 3: Grant SP Access to Key Vault ---
echo -e "\n${YELLOW}--- Step 3: Assigning 'Key Vault Secrets User' Role to SP ---${NC}"
az role assignment create \
    --assignee-object-id "${SP_OBJECT_ID}" \
    --role "Key Vault Secrets User" \
    --scope "${KV_ID}" \
    --assignee-principal-type "ServicePrincipal" \
    --output none \
    --only-show-errors || echo -e "${YELLOW}Warning: Role assignment for SP might already exist. Proceeding.${NC}"
echo -e "${GREEN}✓ Service Principal has been granted permission to read secrets from Key Vault.${NC}"

# --- Step 4: Add/Update Secrets in Key Vault ---
echo -e "\n${YELLOW}--- Step 4: Populating Secrets in Azure Key Vault ---${NC}"
echo "Setting ApplicationInsights--ConnectionString secret..."
az keyvault secret set --vault-name "$KV_NAME" --name "ApplicationInsights--ConnectionString" --value "$AI_CS" --output none
echo "Setting Kafka--BootstrapServers secret..."
az keyvault secret set --vault-name "$KV_NAME" --name "Kafka--BootstrapServers" --value "$KAFKA_BOOTSTRAP_SERVERS" --output none
echo "Setting CosmosDb secrets..."
az keyvault secret set --vault-name "$KV_NAME" --name "CosmosDb--EndpointUri" --value "$COSMOSDB_ENDPOINT_URI" --output none
az keyvault secret set --vault-name "$KV_NAME" --name "CosmosDb--PrimaryKey" --value "$COSMOSDB_PRIMARY_KEY" --output none
echo -e "${GREEN}✓ All application secrets have been set in Key Vault '${KV_NAME}'.${NC}"

# --- Step 5: Generate .env File ---
PROJECT_ENV_FILE=".env"
echo -e "\n${YELLOW}--- Step 5: Creating/Updating '${PROJECT_ENV_FILE}' for Docker Compose ---${NC}"
cat >>"${PROJECT_ENV_FILE}" <<EOF
# This file was auto-generated by the setup_app_config.sh script.
# It contains secrets for local Docker development and should NOT be committed to Git.

# Azure Key Vault Configuration
KEY_VAULT_URI="${KV_URI}"

# Application Service Principal Credentials (for Key Vault Access)
AZURE_CLIENT_ID="${SP_APP_ID}"
AZURE_CLIENT_SECRET="${SP_PASSWORD}"
AZURE_TENANT_ID="${SP_TENANT_ID}"
EOF
echo -e "${GREEN}✓ '${PROJECT_ENV_FILE}' created/updated successfully.${NC}"
echo -e "${YELLOW}IMPORTANT: Ensure '${PROJECT_ENV_FILE}' and '${ENV_FILE}' are listed in your .gitignore file!${NC}"

# --- Final Instructions ---
echo -e "\n${YELLOW}============================================${NC}"
echo -e "${YELLOW}      Application Configuration Complete!     ${NC}"
echo -e "${YELLOW}============================================${NC}"
echo ""
echo "You are now ready to run your application."
echo "The '.env' file has been populated with the necessary credentials for the application SP to access Key Vault."
echo ""
echo -e "${CYAN}Next Step: Run Your Application:${NC}"
echo "From the project root, run: ${GREEN}docker-compose up --build -d${NC}"
echo ""

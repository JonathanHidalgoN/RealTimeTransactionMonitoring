#!/bin/bash
set -e

YELLOW='\033[1;33m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

echo -e "${YELLOW}======================================================${NC}"
echo -e "${YELLOW}  Azure Infrastructure Bootstrap for RealTimeFinancialMonitoring ${NC}"
echo -e "${YELLOW}======================================================${NC}"
echo ""
echo "This script will:"
echo "1. Log into Azure and set your active subscription."
echo "2. Create a dedicated Resource Group for your project."
echo "3. Create a Storage Account and Blob Container for Terraform remote state."
echo "4. Create a single Service Principal (SP) for both Terraform automation and application access."
echo "5. Assign the necessary RBAC roles to the SP for its tasks."
echo "6. Generate two files:"
echo -e "   - ${CYAN}infra/backend.tf${NC} (to configure Terraform's remote state)"
echo -e "   - ${CYAN}.terraform.env${NC} (with SP credentials for Terraform to use)"
echo ""
echo -e "${CYAN}After this script completes, you will be given clear manual steps to run Terraform and configure your application.${NC}"
echo -e "${YELLOW}WARNING: This script will create Azure resources which may incur costs.${NC}"
echo ""

read -p "Do you want to continue? (yes/no): " confirm
if [[ $confirm != "yes" ]]; then
    echo "Operation cancelled by user."
    exit 1
fi

#===================================================================================
# VARIABLES TO EDIT - Please customize these before running
#===================================================================================
AZURE_SUBSCRIPTION_ID="97479738-fc74-450b-999d-9a7567849002"
AZURE_LOCATION="mexicocentral"
RESOURCE_GROUP_NAME_BASE="finmon"
SERVICE_PRINCIPAL_NAME="FinMonInfraAppSP"
TF_STATE_STORAGE_ACCOUNT_NAME_BASE="finmontfstate"
#===================================================================================

# --- Derived Names ---
RESOURCE_GROUP_NAME="${RESOURCE_GROUP_NAME_BASE}-rg"
RANDOM_SUFFIX=$(head /dev/urandom | tr -dc a-z0-9 | head -c 6)
TF_STATE_STORAGE_ACCOUNT_NAME_RAW="${TF_STATE_STORAGE_ACCOUNT_NAME_BASE}${RANDOM_SUFFIX}"
TF_STATE_STORAGE_ACCOUNT_NAME=$(echo "${TF_STATE_STORAGE_ACCOUNT_NAME_RAW}" | tr '[:upper:]' '[:lower:]' | sed 's/[^a-z0-9]//g' | cut -c 1-24)

# --- Static Names ---
TF_STATE_CONTAINER_NAME="tfstate"
TF_STATE_KEY="realtimefinancialmonitoring.terraform.tfstate"
TERRAFORM_DIR="./infra"
TF_ENV_FILE=".terraform.env"
PROJECT_ENV_FILE=".env"

echo -e "\n${CYAN}Using Resource Group Name: ${RESOURCE_GROUP_NAME}${NC}"
echo -e "${CYAN}Using TF State Storage Account Name: ${TF_STATE_STORAGE_ACCOUNT_NAME}${NC}"

# --- Step 1: Azure Login & Subscription ---
echo -e "\n${YELLOW}--- Step 1: Logging into Azure & Setting Subscription ---${NC}"
if ! az account show >/dev/null 2>&1; then
    echo "Attempting Azure login..."
    az login --output none --only-show-errors
fi
LOGGED_IN_USER=$(az account show --query "user.name" -o tsv)
az account set --subscription "${AZURE_SUBSCRIPTION_ID}" --only-show-errors
CURRENT_SUB_NAME=$(az account show --query "name" -o tsv)
echo -e "${GREEN}✓ Logged in as ${LOGGED_IN_USER}. Active subscription set to: ${CURRENT_SUB_NAME}${NC}"

# --- Step 2: Create Resource Group ---
echo -e "\n${YELLOW}--- Step 2: Ensuring Resource Group '${RESOURCE_GROUP_NAME}' exists ---${NC}"
if az group show --name "${RESOURCE_GROUP_NAME}" &>/dev/null; then
    echo "Resource Group '${RESOURCE_GROUP_NAME}' already exists."
else
    echo "Creating Resource Group '${RESOURCE_GROUP_NAME}' in '${AZURE_LOCATION}'..."
    az group create --name "${RESOURCE_GROUP_NAME}" --location "${AZURE_LOCATION}" --output none --only-show-errors
    echo "Resource Group created."
fi
RESOURCE_GROUP_ID=$(az group show --name "${RESOURCE_GROUP_NAME}" --query "id" -o tsv)
echo -e "${GREEN}✓ Resource Group ready.${NC}"

# --- Step 3: Create Storage Account and Container for Terraform State ---
echo -e "\n${YELLOW}--- Step 3: Ensuring Storage Account for Terraform State ---${NC}"
if az storage account show --name "$TF_STATE_STORAGE_ACCOUNT_NAME" --resource-group "$RESOURCE_GROUP_NAME" &>/dev/null; then
    echo "Storage Account '$TF_STATE_STORAGE_ACCOUNT_NAME' already exists."
else
    echo "Creating Storage Account '$TF_STATE_STORAGE_ACCOUNT_NAME'..."
    az storage account create \
        --name "$TF_STATE_STORAGE_ACCOUNT_NAME" \
        --resource-group "$RESOURCE_GROUP_NAME" \
        --location "$AZURE_LOCATION" \
        --sku Standard_LRS \
        --kind StorageV2 \
        --https-only true \
        --allow-blob-public-access false \
        --min-tls-version TLS1_2 \
        --output none \
        --only-show-errors
    echo "Storage Account created."
fi

echo "Ensuring Blob Container '${TF_STATE_CONTAINER_NAME}' exists..."
az storage container create \
    --name "${TF_STATE_CONTAINER_NAME}" \
    --account-name "$TF_STATE_STORAGE_ACCOUNT_NAME" \
    --auth-mode login \
    --public-access off \
    --output none \
    --only-show-errors || echo -e "${YELLOW}Warning: Container '${TF_STATE_CONTAINER_NAME}' might already exist. Proceeding.${NC}"
TF_STATE_STORAGE_ACCOUNT_ID=$(az storage account show --name "$TF_STATE_STORAGE_ACCOUNT_NAME" --resource-group "$RESOURCE_GROUP_NAME" --query "id" -o tsv)
echo -e "${GREEN}✓ Storage for Terraform state ready.${NC}"

# --- Step 4: Create Service Principal ---
echo -e "\n${YELLOW}--- Step 4: Creating Service Principal '${SERVICE_PRINCIPAL_NAME}' ---${NC}"
SP_JSON_OUTPUT=$(az ad sp create-for-rbac --name "${SERVICE_PRINCIPAL_NAME}" --skip-assignment --output json --only-show-errors)
SP_APP_ID=$(echo "${SP_JSON_OUTPUT}" | jq -r '.appId')
SP_PASSWORD=$(echo "${SP_JSON_OUTPUT}" | jq -r '.password')
SP_TENANT_ID=$(echo "${SP_JSON_OUTPUT}" | jq -r '.tenant')
echo "Service Principal created. Waiting for propagation in Azure AD..."
sleep 20
SP_OBJECT_ID=$(az ad sp show --id "${SP_APP_ID}" --query "id" -o tsv 2>/dev/null)
ADMIN_OBJECT_ID=$(az ad signed-in-user show --query "id" -o tsv)
if [ -z "${SP_OBJECT_ID}" ] || [ -z "${ADMIN_OBJECT_ID}" ]; then
    echo "ERROR: Failed to retrieve Object ID for Service Principal or Admin User. Please check Azure AD." >&2
    exit 1
fi
echo -e "${GREEN}✓ Service Principal created and Admin User ID retrieved successfully.${NC}"
echo "  Admin User Object ID: ${ADMIN_OBJECT_ID}"

# --- Step 5: Assign RBAC Roles to Service Principal ---
echo -e "\n${YELLOW}--- Step 5: Assigning RBAC Roles to Service Principal ---${NC}"
echo "Waiting for SP to be available for role assignments..."
sleep 15

echo "Assigning 'Contributor' role to SP on Resource Group scope..."
az role assignment create --assignee-object-id "${SP_OBJECT_ID}" --role "Contributor" --scope "${RESOURCE_GROUP_ID}" --output none --only-show-errors
echo "  'Contributor' on Resource Group assigned."

echo "Assigning 'Storage Blob Data Contributor' role to SP on Storage Account scope..."
az role assignment create --assignee-object-id "${SP_OBJECT_ID}" --role "Storage Blob Data Contributor" --scope "${TF_STATE_STORAGE_ACCOUNT_ID}" --output none --only-show-errors
echo "  'Storage Blob Data Contributor' on Storage Account assigned."
echo -e "${GREEN}✓ RBAC roles assigned.${NC}"

echo "Assigning 'User Access Administrator' role to SP on Resource Group scope..."
az role assignment create --assignee-object-id "${SP_OBJECT_ID}" --role "User Access Administrator" --scope "${RESOURCE_GROUP_ID}" --output none --only-show-errors
echo "  'User Access Administrator' on Resource Group assigned."
# --- Step 6: Generate Terraform Configuration Files ---
echo -e "\n${YELLOW}--- Step 6: Generating Terraform Helper Files ---${NC}"

# Create/overwrite Terraform backend configuration
BACKEND_TF_FILE="${TERRAFORM_DIR}/backend.tf"
echo "Creating/Updating Terraform backend configuration in '${BACKEND_TF_FILE}'..."
cat >"${BACKEND_TF_FILE}" <<EOF
terraform {
  backend "azurerm" {
    resource_group_name  = "${RESOURCE_GROUP_NAME}"
    storage_account_name = "${TF_STATE_STORAGE_ACCOUNT_NAME}"
    container_name       = "${TF_STATE_CONTAINER_NAME}"
    key                  = "${TF_STATE_KEY}"
  }
}
EOF

echo "Creating/Updating Terraform environment file '${TF_ENV_FILE}'..."
cat >"${TF_ENV_FILE}" <<EOF
#!/bin/bash
# Terraform Service Principal Credentials for Azure Provider
export ARM_CLIENT_ID="${SP_APP_ID}"
export ARM_CLIENT_SECRET="${SP_PASSWORD}"
export ARM_SUBSCRIPTION_ID="${AZURE_SUBSCRIPTION_ID}"
export ARM_TENANT_ID="${SP_TENANT_ID}"
export TF_VAR_app_service_principal_object_id="${SP_OBJECT_ID}"
export TF_VAR_admin_user_object_id="${ADMIN_OBJECT_ID}"
export TF_VAR_resource_prefix="${RESOURCE_GROUP_NAME_BASE}"
export TF_VAR_azure_location="${AZURE_LOCATION}"
EOF
chmod +x "${TF_ENV_FILE}"
echo -e "${GREEN}✓ Helper files generated successfully.${NC}"

echo -e "\n${YELLOW}======================================================${NC}"
echo -e "${YELLOW}          Bootstrap Setup Complete!           ${NC}"
echo -e "${YELLOW}======================================================${NC}"
echo ""
echo "Your Azure environment is now prepared. Here is your checklist for the next manual steps:"
echo ""
echo -e "${CYAN}--- Part A: Provision Infrastructure with Terraform ---${NC}"
echo ""
echo "In this part, you will authenticate as the Terraform Service Principal and create the Azure resources."
echo ""
echo -e "${CYAN}1. Secure your generated credential files:${NC}"
echo "   Run this command from the project root to add them to .gitignore:"
echo -e "   ${GREEN}echo '${TF_ENV_FILE}' >> .gitignore && echo '${PROJECT_ENV_FILE}' >> .gitignore${NC}"
echo ""
echo -e "${CYAN}2. Authenticate as the Terraform SP in your current shell session:${NC}"
echo "   Run: ${GREEN}source ./${TF_ENV_FILE}${NC}"
echo ""
echo -e "${CYAN}3. Initialize Terraform with the Remote Backend:${NC}"
echo "   Run: ${GREEN}cd ${TERRAFORM_DIR}${NC}"
echo -e "   Run: ${GREEN}terraform init -upgrade${NC}"
echo ""
echo -e "${CYAN}4. (First time only) Import the Resource Group into Terraform state:${NC}"
echo "   Run: ${GREEN}terraform import azurerm_resource_group.rg \"${RESOURCE_GROUP_ID}\"${NC}"
echo ""
echo -e "${CYAN}5. Run Terraform Plan & Apply:${NC}"
echo "   This creates your Key Vault, Application Insights, etc."
echo -e "   Run: ${GREEN}terraform plan -out=tfplan${NC}"
echo -e "   Then: ${GREEN}terraform apply tfplan${NC}"

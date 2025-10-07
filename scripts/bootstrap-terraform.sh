#!/bin/bash
# Bootstrap Terraform Backend
# Run ONCE to initialize Terraform backend storage in Azure
# This creates the Resource Group, Storage Account, and Blob Container
# that Terraform uses to store its state files.

set -e # Exit on any error

LOCATION="mexicocentral"
RG_NAME="finmon-tfstate-rg"
STORAGE_NAME="finmontfstate"
CONTAINER_NAME="tfstate"

echo "Bootstrapping Terraform backend..."
echo "   Location: $LOCATION"
echo "   Resource Group: $RG_NAME"
echo "   Storage Account: $STORAGE_NAME"
echo "   Container: $CONTAINER_NAME"
echo ""
echo "WARNING: These values must match the backend configuration in:"
echo "   - infrastructure/shared/backend.tf"
echo "   - infrastructure/environments/dev/backend.tf"
echo "   - infrastructure/environments/*/backend.tf (any other environments)"
echo ""

if ! command -v az &>/dev/null; then
    echo "ERROR: Azure CLI is not installed. Please install it first:"
    echo "   https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
fi

if ! az account show &>/dev/null; then
    echo "ERROR: Not logged into Azure. Please run: az login"
    exit 1
fi

SUBSCRIPTION=$(az account show --query name -o tsv)
echo "Using Azure subscription: $SUBSCRIPTION"
echo ""

read -p "Do you want to proceed? (yes/no): " CONFIRM
if [ "$CONFIRM" != "yes" ]; then
    echo "Aborted."
    exit 0
fi
echo ""

echo "Creating resource group '$RG_NAME'..."
if az group show --name "$RG_NAME" &>/dev/null; then
    echo "   Resource group already exists, skipping..."
else
    az group create \
        --name "$RG_NAME" \
        --location "$LOCATION" \
        --output none
    echo "   Created"
fi

echo "Creating storage account '$STORAGE_NAME'..."
if az storage account show --name "$STORAGE_NAME" --resource-group "$RG_NAME" &>/dev/null; then
    echo "   Storage account already exists, skipping..."
else
    az storage account create \
        --name "$STORAGE_NAME" \
        --resource-group "$RG_NAME" \
        --location "$LOCATION" \
        --sku Standard_LRS \
        --encryption-services blob \
        --output none
    echo "   Created"
fi

echo "Retrieving storage account key..."
ACCOUNT_KEY=$(az storage account keys list \
    --resource-group "$RG_NAME" \
    --account-name "$STORAGE_NAME" \
    --query '[0].value' -o tsv)

if [ -z "$ACCOUNT_KEY" ]; then
    echo "ERROR: Failed to retrieve storage account key"
    exit 1
fi

echo "Creating blob container '$CONTAINER_NAME'..."
if az storage container exists \
    --name "$CONTAINER_NAME" \
    --account-name "$STORAGE_NAME" \
    --account-key "$ACCOUNT_KEY" \
    --query "exists" -o tsv | grep -q "true"; then
    echo "   Container already exists, skipping..."
else
    az storage container create \
        --name "$CONTAINER_NAME" \
        --account-name "$STORAGE_NAME" \
        --account-key "$ACCOUNT_KEY" \
        --output none
    echo "   Created"
fi

echo ""
echo "SUCCESS: Terraform backend is ready!"
echo ""

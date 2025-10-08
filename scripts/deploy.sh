#!/bin/bash
# Deploy Infrastructure and Applications
# This script orchestrates the full deployment process:
# 1. Deploy shared infrastructure (ACR)
# 2. Build and push Docker images
# 3. Deploy environment-specific infrastructure (Container Apps, etc.)

set -e

ENVIRONMENT=${1:-dev}

echo "========================================="
echo "Deploying to environment: $ENVIRONMENT"
echo "========================================="
echo ""

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

ENV_DIR="$PROJECT_ROOT/infrastructure/environments/$ENVIRONMENT"
if [ ! -d "$ENV_DIR" ]; then
    echo "ERROR: Environment '$ENVIRONMENT' not found at $ENV_DIR"
    echo "Available environments:"
    ls -1 "$PROJECT_ROOT/infrastructure/environments/"
    exit 1
fi

# Check terraform.tfvars exists
TFVARS_FILE="$ENV_DIR/terraform.tfvars"
if [ ! -f "$TFVARS_FILE" ]; then
    echo "ERROR: terraform.tfvars not found at $TFVARS_FILE"
    echo "Please create and populate it with required values (resource_prefix, location, admin_user_object_id)"
    exit 1
fi

echo "Please ensure $TFVARS_FILE contains the required values:"
echo "  - resource_prefix"
echo "  - location"
echo "  - admin_user_object_id"
echo ""
read -p "Have you configured terraform.tfvars? (yes/no): " TFVARS_CONFIRM
if [ "$TFVARS_CONFIRM" != "yes" ]; then
    echo "Aborted."
    exit 0
fi
echo ""

# Check prerequisites
if ! command -v az &>/dev/null; then
    echo "ERROR: Azure CLI is not installed"
    exit 1
fi

if ! command -v terraform &>/dev/null; then
    echo "ERROR: Terraform is not installed"
    exit 1
fi

if ! command -v docker &>/dev/null; then
    echo "ERROR: Docker is not installed"
    exit 1
fi

if ! az account show &>/dev/null; then
    echo "ERROR: Not logged into Azure. Please run: az login"
    exit 1
fi

SUBSCRIPTION=$(az account show --query name -o tsv)
echo "Using Azure subscription: $SUBSCRIPTION"
echo ""

read -p "Do you want to proceed with deployment? (yes/no): " CONFIRM
if [ "$CONFIRM" != "yes" ]; then
    echo "Aborted."
    exit 0
fi
echo ""

echo "========================================="
echo "Phase 1: Deploying shared infrastructure"
echo "========================================="
echo ""

cd "$PROJECT_ROOT/infrastructure/shared"

echo "Running terraform init..."
terraform init

echo "Running terraform apply..."
terraform apply -auto-approve

echo ""
echo "Retrieving ACR login server..."
ACR_SERVER=$(terraform output -raw acr_login_server)

if [ -z "$ACR_SERVER" ]; then
    echo "ERROR: Failed to retrieve ACR login server from Terraform output"
    exit 1
fi

echo "ACR Server: $ACR_SERVER"
echo ""

echo "========================================="
echo "Phase 2: Building and pushing images"
echo "========================================="
echo ""

cd "$PROJECT_ROOT"
"$SCRIPT_DIR/build-and-push-images.sh" "$ACR_SERVER"

echo ""

echo "========================================="
echo "Phase 3: Deploying $ENVIRONMENT environment"
echo "========================================="
echo ""

cd "$ENV_DIR"

echo "Running terraform init..."
terraform init

echo "Running terraform apply..."
terraform apply -auto-approve

echo ""
echo "========================================="
echo "Deployment Complete!"
echo "========================================="
echo ""

# Get outputs
echo "Infrastructure Outputs:"
terraform output

#!/bin/bash
# Deploy Infrastructure and Applications
# This script orchestrates the full deployment process:
# 1. Deploy shared infrastructure (ACR)
# 2. Build and push Docker images
# 3. Deploy environment-specific infrastructure (Container Apps, etc.)

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

ENVIRONMENT=${1:-dev}

echo -e "${YELLOW}=========================================${NC}"
echo -e "${YELLOW}Deploying to environment: $ENVIRONMENT${NC}"
echo -e "${YELLOW}=========================================${NC}"
echo ""

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

ENV_DIR="$PROJECT_ROOT/infrastructure/environments/$ENVIRONMENT"
if [ ! -d "$ENV_DIR" ]; then
    echo -e "${RED}ERROR: Environment '$ENVIRONMENT' not found at $ENV_DIR${NC}"
    echo "Available environments:"
    ls -1 "$PROJECT_ROOT/infrastructure/environments/"
    exit 1
fi

# Check terraform.tfvars exists
TFVARS_FILE="$ENV_DIR/terraform.tfvars"
if [ ! -f "$TFVARS_FILE" ]; then
    echo -e "${RED}ERROR: terraform.tfvars not found at $TFVARS_FILE${NC}"
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
    echo -e "${RED}ERROR: Azure CLI is not installed${NC}"
    exit 1
fi

if ! command -v terraform &>/dev/null; then
    echo -e "${RED}ERROR: Terraform is not installed${NC}"
    exit 1
fi

if ! command -v docker &>/dev/null; then
    echo -e "${RED}ERROR: Docker is not installed${NC}"
    exit 1
fi

if ! az account show &>/dev/null; then
    echo -e "${RED}ERROR: Not logged into Azure. Please run: az login${NC}"
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

echo -e "${YELLOW}=========================================${NC}"
echo -e "${YELLOW}Phase 1: Deploying shared infrastructure${NC}"
echo -e "${YELLOW}=========================================${NC}"
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
    echo -e "${RED}ERROR: Failed to retrieve ACR login server from Terraform output${NC}"
    exit 1
fi

echo -e "${GREEN}ACR Server: $ACR_SERVER${NC}"
echo ""

echo -e "${YELLOW}=========================================${NC}"
echo -e "${YELLOW}Phase 2: Building and pushing images${NC}"
echo -e "${YELLOW}=========================================${NC}"
echo ""

cd "$PROJECT_ROOT"
"$SCRIPT_DIR/build-and-push-images.sh" "$ACR_SERVER"

echo ""

echo -e "${YELLOW}=========================================${NC}"
echo -e "${YELLOW}Phase 3: Deploying $ENVIRONMENT environment${NC}"
echo -e "${YELLOW}=========================================${NC}"
echo ""

cd "$ENV_DIR"

echo "Running terraform init..."
terraform init

echo "Running terraform apply..."
terraform apply -auto-approve

echo ""
echo -e "${GREEN}=========================================${NC}"
echo -e "${GREEN}Deployment Complete!${NC}"
echo -e "${GREEN}=========================================${NC}"
echo ""

# Get outputs
echo "Infrastructure Outputs:"
terraform output

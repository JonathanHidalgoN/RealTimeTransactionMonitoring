#!/bin/bash
# Deploy Blazor WebAssembly Frontend
# This script:
# 1. Gets configuration from Terraform outputs
# 2. Builds the Blazor app
# 3. Replaces configuration placeholders
# 4. Deploys to Azure Static Web App

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

ENVIRONMENT=${1:-dev}

echo -e "${YELLOW}=========================================${NC}"
echo -e "${YELLOW}Deploying Frontend to $ENVIRONMENT${NC}"
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

if ! command -v az &>/dev/null; then
    echo -e "${RED}ERROR: Azure CLI is not installed${NC}"
    exit 1
fi

if ! command -v dotnet &>/dev/null; then
    echo -e "${RED}ERROR: .NET SDK is not installed${NC}"
    exit 1
fi

if ! az account show &>/dev/null; then
    echo -e "${RED}ERROR: Not logged into Azure. Please run: az login${NC}"
    exit 1
fi

if ! command -v swa &>/dev/null; then
    echo -e "${RED}ERROR: Azure Static Web Apps CLI is not installed${NC}"
    echo "Please install it by running:"
    echo "  npm install -g @azure/static-web-apps-cli"
    exit 1
fi

echo "Getting configuration from Terraform..."
cd "$ENV_DIR"

API_URL=$(terraform output -raw api_url 2>/dev/null || true)
API_KEY=$(terraform output -raw api_key 2>/dev/null || true)
DEPLOYMENT_TOKEN=$(terraform output -raw frontend_api_key 2>/dev/null || true)

if [ -z "$API_URL" ]; then
    echo -e "${RED}ERROR: Could not retrieve api_url from Terraform${NC}"
    echo "Make sure infrastructure is deployed: cd $ENV_DIR && terraform apply"
    exit 1
fi

if [ -z "$API_KEY" ]; then
    echo -e "${RED}ERROR: Could not retrieve api_key from Terraform${NC}"
    exit 1
fi

if [ -z "$DEPLOYMENT_TOKEN" ]; then
    echo -e "${RED}ERROR: Could not retrieve deployment token from Terraform${NC}"
    exit 1
fi

echo -e "${GREEN}Configuration retrieved:${NC}"
echo "  API URL: $API_URL"
echo ""

echo -e "${YELLOW}Building Blazor WebAssembly app...${NC}"
cd "$PROJECT_ROOT/src/FinancialMonitoring.WebApp"

dotnet publish -c Release -o "$PROJECT_ROOT/build/webapp"

if [ ! -d "$PROJECT_ROOT/build/webapp/wwwroot" ]; then
    echo -e "${RED}ERROR: Build failed - wwwroot directory not found${NC}"
    exit 1
fi

echo -e "${GREEN}Build completed${NC}"
echo ""

echo "Replacing configuration placeholders..."
PUBLISH_DIR="$PROJECT_ROOT/build/webapp/wwwroot"
APPSETTINGS_FILE="$PUBLISH_DIR/appsettings.json"

if [ ! -f "$APPSETTINGS_FILE" ]; then
    echo -e "${RED}ERROR: appsettings.json not found at $APPSETTINGS_FILE${NC}"
    exit 1
fi

sed -i "s|__ApiBaseUrl__|$API_URL|g" "$APPSETTINGS_FILE"
sed -i "s|__ApiKey__|$API_KEY|g" "$APPSETTINGS_FILE"

echo -e "${GREEN}Configuration updated${NC}"
echo "  API URL: $API_URL"
echo "  API Key: [REDACTED]"
echo ""

echo -e "${YELLOW}Deploying to Azure Static Web App...${NC}"

swa deploy "$PUBLISH_DIR" \
    --deployment-token "$DEPLOYMENT_TOKEN" \
    --env production

echo ""
echo -e "${GREEN}=========================================${NC}"
echo -e "${GREEN}Frontend Deployment Complete!${NC}"
echo -e "${GREEN}=========================================${NC}"
echo ""

FRONTEND_URL=$(cd "$ENV_DIR" && terraform output -raw frontend_url)
echo "Frontend URL: $FRONTEND_URL"
echo ""
echo "Note: It may take a few minutes for the deployment to propagate globally."

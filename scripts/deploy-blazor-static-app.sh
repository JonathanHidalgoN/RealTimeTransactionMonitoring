#!/bin/bash
# Purpose: Deploy Blazor WebApp to Azure Static Web Apps
# Configures: API endpoints, builds and deploys frontend

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${YELLOW}Deploying Blazor WebApp to Azure Static Web Apps${NC}"

if [ ! -f ".env" ]; then
    echo -e "${RED}Error: .env file not found in project root${NC}" >&2
    echo "Please create a .env file with API_KEY variable" >&2
    exit 1
fi

if [ ! -d "infra" ]; then
    echo -e "${RED}Error: infra directory not found${NC}" >&2
    echo "Please run this script from the project root" >&2
    exit 1
fi

echo -e "${CYAN}--- Step 1: Loading configuration ---${NC}"

source .env
if [ -z "$API_KEY" ]; then
    echo -e "${RED}Error: API_KEY not found in .env file${NC}" >&2
    echo "Please add API_KEY=your-api-key to your .env file" >&2
    exit 1
fi

cd infra
STATIC_WEB_APP_TOKEN=$(terraform output -raw static_web_app_api_key 2>/dev/null)

INGRESS_HOST=$(kubectl get ingress api-ingress -n finmon-app -o jsonpath='{.spec.rules[0].host}' 2>/dev/null)
if [ -n "$INGRESS_HOST" ]; then
    API_BASE_URL="https://${INGRESS_HOST}"
    echo "✓ Auto-detected API URL from ingress: ${API_BASE_URL}"
else
    API_BASE_URL="http://0.0.1.1"
    echo "Could not detect ingress. Using placeholder: ${API_BASE_URL}"
    echo "Update manually: kubectl get ingress api-ingress -n finmon-app"
fi

if [ -z "$STATIC_WEB_APP_TOKEN" ]; then
    echo -e "${RED}Error: Could not get Static Web App API token from Terraform${NC}" >&2
    echo "Please ensure 'terraform apply' has been run successfully" >&2
    exit 1
fi

cd ..

echo -e "${GREEN}✓ Configuration loaded${NC}"
echo "  API Base URL: ${API_BASE_URL}"
echo "  API Key: ${API_KEY:0:8}..."

echo -e "\n${CYAN}--- Step 2: Preparing appsettings.json ---${NC}"

# Create backup of original appsettings.json
APPSETTINGS_FILE="src/FinancialMonitoring.WebApp/wwwroot/appsettings.json"
BACKUP_FILE="${APPSETTINGS_FILE}.backup"

if [ ! -f "$BACKUP_FILE" ]; then
    cp "$APPSETTINGS_FILE" "$BACKUP_FILE"
    echo "✓ Created backup of appsettings.json"
fi
# Replace tokens with actual values
# Escape special characters in API_KEY for sed (including &)
ESCAPED_API_KEY=$(printf '%s\n' "$API_KEY" | sed 's/[[\.*^$()+?{|&]/\\&/g')
sed -i.tmp "s|__ApiBaseUrl__|${API_BASE_URL}|g" "$APPSETTINGS_FILE"
sed -i.tmp "s|__ApiKey__|${ESCAPED_API_KEY}|g" "$APPSETTINGS_FILE"
rm "${APPSETTINGS_FILE}.tmp"

echo -e "${GREEN}✓ Updated appsettings.json with current values${NC}"

# Check if SWA CLI is installed
if ! command -v swa &>/dev/null; then
    echo "Azure Static Web Apps CLI not found."
fi

echo -e "\n${CYAN}--- Step 4: Building Blazor WebApp ---${NC}"

cd src/FinancialMonitoring.WebApp

dotnet restore
dotnet build --configuration Release
dotnet publish --configuration Release --output ./bin/publish

echo -e "${GREEN}✓ Blazor app built successfully${NC}"

echo -e "\n${CYAN}--- Step 5: Deploying to Azure Static Web Apps ---${NC}"

# Deploy using SWA CLI
swa deploy \
    --deployment-token "$STATIC_WEB_APP_TOKEN" \
    --app-location "./bin/publish/wwwroot" \
    --verbose

cd ../..

echo -e "\n${CYAN}--- Step 6: Restoring original appsettings.json ---${NC}"

cp "$BACKUP_FILE" "$APPSETTINGS_FILE"
echo -e "${GREEN}✓ Restored original appsettings.json${NC}"

echo -e "\n${GREEN}✓ Blazor WebApp Deployment Complete${NC}"

STATIC_URL=$(cd infra && terraform output -raw static_web_app_default_hostname 2>/dev/null | sed 's/^/https:\/\//' || echo "Check Azure portal for URL")
echo "Web App URL: ${STATIC_URL}"
cd ..

echo ""
echo -e "${YELLOW}⚠️  CORS Configuration Required${NC}"
echo "Add this URL to API's CORS AllowedOrigins in ConfigMap:"
echo "${STATIC_URL}"

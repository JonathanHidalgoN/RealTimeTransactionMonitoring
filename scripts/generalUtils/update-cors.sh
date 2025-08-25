#!/bin/bash
# Purpose: Update CORS configuration with Static Web App URL
# Automates: CORS origins configuration and API restart

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${YELLOW}Updating CORS configuration${NC}"

STATIC_URL=$(cd infra && terraform output -raw static_web_app_default_hostname)
if [ -z "$STATIC_URL" ]; then
    echo "Error: Could not get Static Web App URL from Terraform"
    exit 1
fi

echo "Static Web App URL: https://$STATIC_URL"

PREVIEW_URL=$(echo $STATIC_URL | sed 's/\.1\.azurestaticapps\.net/-preview.centralus.1.azurestaticapps.net/')

echo "Production URL: https://$STATIC_URL"
echo "Preview URL: https://$PREVIEW_URL"

kubectl patch configmap env-config -n finmon-app --type merge -p "{
  \"data\": {
    \"AllowedOrigins__0\": \"http://localhost:5124\",
    \"AllowedOrigins__1\": \"https://localhost:7258\",
    \"AllowedOrigins__2\": \"https://$STATIC_URL\",
    \"AllowedOrigins__3\": \"https://$PREVIEW_URL\"
  }
}"

kubectl rollout restart deployment api-deployment -n finmon-app

echo -e "${GREEN}CORS configuration updated and API restarted${NC}"


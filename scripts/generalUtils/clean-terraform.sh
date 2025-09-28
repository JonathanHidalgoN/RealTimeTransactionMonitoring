#!/bin/bash
set -e

YELLOW='\033[1;33m'
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${YELLOW}Checking for existing Terraform state...${NC}"

# Check if infra/.terraform directory exists (relative to current working directory)
if [ -d "infra/.terraform" ] || [ -f "infra/.terraform.lock.hcl" ] || [ -f "infra/tfplan" ]; then
    echo -e "${YELLOW}Found existing Terraform state files:${NC}"

    if [ -d "infra/.terraform" ]; then
        echo "  • infra/.terraform/ (Terraform cache directory)"
    fi

    if [ -f "infra/.terraform.lock.hcl" ]; then
        echo "  • infra/.terraform.lock.hcl (Provider lock file)"
    fi

    if [ -f "infra/tfplan" ]; then
        echo "  • infra/tfplan (Terraform execution plan)"
    fi

    echo ""
    echo -e "${YELLOW}These files may cause backend configuration conflicts.${NC}"
    echo -e "${YELLOW}It's recommended to clean them before running terraform init.${NC}"
    echo ""

    read -p "Do you want to delete these files? (yes/no): " confirm

    if [[ $confirm == "yes" ]]; then
        echo ""
        echo -e "${YELLOW}Cleaning Terraform state files...${NC}"

        if [ -d "infra/.terraform" ]; then
            rm -rf infra/.terraform
            echo -e "${GREEN}✓ Removed infra/.terraform/${NC}"
        fi

        if [ -f "infra/.terraform.lock.hcl" ]; then
            rm -f infra/.terraform.lock.hcl
            echo -e "${GREEN}✓ Removed infra/.terraform.lock.hcl${NC}"
        fi

        if [ -f "infra/tfplan" ]; then
            rm -f infra/tfplan
            echo -e "${GREEN}✓ Removed infra/tfplan${NC}"
        fi

        echo ""
        echo -e "${GREEN}✓ Terraform state cleaned successfully!${NC}"
    else
        echo ""
        echo -e "${YELLOW}Terraform state files kept. You may encounter backend configuration issues.${NC}"
        echo -e "${YELLOW}If terraform init fails, run this script again and choose 'yes' to clean.${NC}"
    fi
else
    echo -e "${GREEN}✓ No existing Terraform state found. Ready for clean initialization.${NC}"
fi

echo ""
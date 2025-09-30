#!/bin/bash

set -e

ENVIRONMENT=""
DESTROY=false
INIT_ONLY=false

usage() {
    echo "Usage: $0 -e <environment> [-d] [-i]"
    echo "  -e: Environment (dev|prod|shared)"
    echo "  -d: Destroy infrastructure"
    echo "  -i: Initialize only (terraform init)"
    echo "Examples:"
    echo "  $0 -e shared    # Deploy shared infrastructure"
    echo "  $0 -e dev       # Deploy dev environment"
    echo "  $0 -e prod      # Deploy prod environment"
    echo "  $0 -e dev -d    # Destroy dev environment"
    echo "  $0 -e dev -i    # Initialize dev environment only"
    exit 1
}

while getopts "e:di" opt; do
    case $opt in
        e) ENVIRONMENT="$OPTARG" ;;
        d) DESTROY=true ;;
        i) INIT_ONLY=true ;;
        *) usage ;;
    esac
done

if [[ -z "$ENVIRONMENT" ]]; then
    echo "Error: Environment must be specified"
    usage
fi

if [[ ! "$ENVIRONMENT" =~ ^(dev|prod|shared)$ ]]; then
    echo "Error: Environment must be one of: dev, prod, shared"
    exit 1
fi

TERRAFORM_DIR="terraform"
if [[ "$ENVIRONMENT" == "shared" ]]; then
    TERRAFORM_DIR="terraform/shared"
else
    TERRAFORM_DIR="terraform/environments/$ENVIRONMENT"
fi

if [[ ! -d "$TERRAFORM_DIR" ]]; then
    echo "Error: Terraform directory $TERRAFORM_DIR does not exist"
    exit 1
fi

echo "=== Deploying $ENVIRONMENT environment ==="
echo "Directory: $TERRAFORM_DIR"
echo "Destroy: $DESTROY"
echo "Init Only: $INIT_ONLY"
echo

cd "$TERRAFORM_DIR"

echo "=== Running terraform init ==="
terraform init

if [[ "$INIT_ONLY" == true ]]; then
    echo "=== Initialization complete ==="
    exit 0
fi

if [[ "$DESTROY" == true ]]; then
    echo "=== Running terraform destroy ==="
    terraform destroy -auto-approve
    echo "=== Destroy complete ==="
else
    echo "=== Running terraform plan ==="
    terraform plan -out=tfplan

    echo "=== Running terraform apply ==="
    terraform apply tfplan

    echo "=== Deployment complete ==="
    echo "=== Outputs ==="
    terraform output
fi
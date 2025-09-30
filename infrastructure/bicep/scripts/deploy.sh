#!/bin/bash

# NumbatWallet Infrastructure Deployment Script
# Purpose: Deploy Azure infrastructure using Bicep templates

set -euo pipefail

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Default values
ENVIRONMENT=""
LOCATION="australiaeast"
SUBSCRIPTION=""
ADMIN_OBJECT_ID=""
POSTGRES_ADMIN_USERNAME="nwadmin"
POSTGRES_ADMIN_PASSWORD=""
WHAT_IF=false
VALIDATE_ONLY=false
DESTROY=false

# Function to print colored output
print_message() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
}

# Function to print usage
print_usage() {
    cat << EOF
Usage: $0 -e <environment> [OPTIONS]

Deploy NumbatWallet infrastructure to Azure using Bicep templates.

REQUIRED:
  -e, --environment <env>      Environment to deploy (dev, test, prod)

OPTIONS:
  -s, --subscription <id>      Azure subscription ID (defaults to current)
  -l, --location <region>      Azure region (default: australiaeast)
  -a, --admin-id <object-id>   Azure AD admin object ID
  -u, --pg-username <username> PostgreSQL admin username (default: nwadmin)
  -p, --pg-password <password> PostgreSQL admin password
  -w, --what-if                Run what-if deployment (preview changes)
  -v, --validate               Validate templates only
  -d, --destroy                Destroy infrastructure (careful!)
  -h, --help                   Show this help message

EXAMPLES:
  # Deploy to development environment
  $0 -e dev -a "00000000-0000-0000-0000-000000000000"

  # Preview production deployment
  $0 -e prod -w

  # Validate templates only
  $0 -e dev -v

EOF
    exit 0
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -e|--environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        -s|--subscription)
            SUBSCRIPTION="$2"
            shift 2
            ;;
        -l|--location)
            LOCATION="$2"
            shift 2
            ;;
        -a|--admin-id)
            ADMIN_OBJECT_ID="$2"
            shift 2
            ;;
        -u|--pg-username)
            POSTGRES_ADMIN_USERNAME="$2"
            shift 2
            ;;
        -p|--pg-password)
            POSTGRES_ADMIN_PASSWORD="$2"
            shift 2
            ;;
        -w|--what-if)
            WHAT_IF=true
            shift
            ;;
        -v|--validate)
            VALIDATE_ONLY=true
            shift
            ;;
        -d|--destroy)
            DESTROY=true
            shift
            ;;
        -h|--help)
            print_usage
            ;;
        *)
            print_message "$RED" "Unknown option: $1"
            print_usage
            ;;
    esac
done

# Validate required parameters
if [[ -z "$ENVIRONMENT" ]]; then
    print_message "$RED" "Error: Environment is required"
    print_usage
fi

if [[ ! "$ENVIRONMENT" =~ ^(dev|test|prod)$ ]]; then
    print_message "$RED" "Error: Invalid environment. Must be dev, test, or prod"
    exit 1
fi

# Set paths
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
BICEP_DIR="$(dirname "$SCRIPT_DIR")"
MAIN_BICEP="$BICEP_DIR/main.bicep"
PARAMS_FILE="$BICEP_DIR/parameters/${ENVIRONMENT}.parameters.json"

# Validate files exist
if [[ ! -f "$MAIN_BICEP" ]]; then
    print_message "$RED" "Error: main.bicep not found at $MAIN_BICEP"
    exit 1
fi

if [[ ! -f "$PARAMS_FILE" ]]; then
    print_message "$RED" "Error: Parameter file not found at $PARAMS_FILE"
    exit 1
fi

print_message "$BLUE" "╔══════════════════════════════════════════════════════╗"
print_message "$BLUE" "║     NumbatWallet Infrastructure Deployment Tool      ║"
print_message "$BLUE" "╚══════════════════════════════════════════════════════╝"
echo

# Check Azure CLI installation
if ! command -v az &> /dev/null; then
    print_message "$RED" "Error: Azure CLI is not installed"
    echo "Please install Azure CLI: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
fi

# Check Bicep CLI installation
if ! az bicep version &> /dev/null; then
    print_message "$YELLOW" "Bicep CLI not found. Installing..."
    az bicep install
fi

# Login check
print_message "$BLUE" "Checking Azure login status..."
if ! az account show &> /dev/null; then
    print_message "$YELLOW" "Not logged in to Azure. Please login..."
    az login
fi

# Set subscription if provided
if [[ -n "$SUBSCRIPTION" ]]; then
    print_message "$BLUE" "Setting subscription to: $SUBSCRIPTION"
    az account set --subscription "$SUBSCRIPTION"
fi

# Get current subscription info
CURRENT_SUB=$(az account show --query "name" -o tsv)
CURRENT_SUB_ID=$(az account show --query "id" -o tsv)
print_message "$GREEN" "Using subscription: $CURRENT_SUB ($CURRENT_SUB_ID)"

# Generate deployment name
DEPLOYMENT_NAME="numbatwallet-${ENVIRONMENT}-$(date +%Y%m%d-%H%M%S)"

# Handle admin object ID
if [[ -z "$ADMIN_OBJECT_ID" ]]; then
    print_message "$YELLOW" "Admin object ID not provided. Attempting to use current user..."
    ADMIN_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv 2>/dev/null || echo "")
    if [[ -z "$ADMIN_OBJECT_ID" ]]; then
        print_message "$RED" "Error: Could not determine admin object ID. Please provide with -a flag"
        exit 1
    fi
    print_message "$GREEN" "Using current user object ID: $ADMIN_OBJECT_ID"
fi

# Handle PostgreSQL password
if [[ -z "$POSTGRES_ADMIN_PASSWORD" ]] && [[ "$VALIDATE_ONLY" == false ]] && [[ "$WHAT_IF" == false ]]; then
    print_message "$YELLOW" "PostgreSQL admin password not provided."
    read -s -p "Enter PostgreSQL admin password: " POSTGRES_ADMIN_PASSWORD
    echo
    if [[ -z "$POSTGRES_ADMIN_PASSWORD" ]]; then
        # Generate a secure password if not provided
        POSTGRES_ADMIN_PASSWORD=$(openssl rand -base64 32)
        print_message "$YELLOW" "Generated PostgreSQL password (save this securely): $POSTGRES_ADMIN_PASSWORD"
    fi
fi

# Create temporary parameters file with values
TEMP_PARAMS=$(mktemp)
cp "$PARAMS_FILE" "$TEMP_PARAMS"

# Update parameters using jq if available, otherwise use sed
if command -v jq &> /dev/null; then
    jq ".parameters.administratorObjectId.value = \"$ADMIN_OBJECT_ID\"" "$TEMP_PARAMS" > "${TEMP_PARAMS}.tmp" && mv "${TEMP_PARAMS}.tmp" "$TEMP_PARAMS"
else
    # Fallback to sed (less reliable)
    sed -i.bak "s/\"administratorObjectId\": {[^}]*}/\"administratorObjectId\": {\"value\": \"$ADMIN_OBJECT_ID\"}/g" "$TEMP_PARAMS"
fi

# Deployment operations
if [[ "$DESTROY" == true ]]; then
    print_message "$RED" "╔══════════════════════════════════════════════════════╗"
    print_message "$RED" "║                    ⚠️  WARNING ⚠️                      ║"
    print_message "$RED" "║   You are about to DESTROY all infrastructure!       ║"
    print_message "$RED" "╚══════════════════════════════════════════════════════╝"
    read -p "Are you sure? Type 'DESTROY' to confirm: " CONFIRM
    if [[ "$CONFIRM" == "DESTROY" ]]; then
        RG_NAME="rg-numbatwallet-${ENVIRONMENT}-aue"
        print_message "$YELLOW" "Deleting resource group: $RG_NAME"
        az group delete --name "$RG_NAME" --yes
        print_message "$GREEN" "Infrastructure destroyed successfully"
    else
        print_message "$YELLOW" "Destruction cancelled"
    fi
    rm -f "$TEMP_PARAMS"
    exit 0
fi

if [[ "$VALIDATE_ONLY" == true ]]; then
    print_message "$BLUE" "Validating Bicep templates..."
    az deployment sub validate \
        --location "$LOCATION" \
        --template-file "$MAIN_BICEP" \
        --parameters "$TEMP_PARAMS" \
        --parameters postgresAdminUsername="$POSTGRES_ADMIN_USERNAME" \
        --parameters postgresAdminPassword="$POSTGRES_ADMIN_PASSWORD" \
        --name "$DEPLOYMENT_NAME"

    if [[ $? -eq 0 ]]; then
        print_message "$GREEN" "✓ Validation successful!"
    else
        print_message "$RED" "✗ Validation failed!"
        rm -f "$TEMP_PARAMS"
        exit 1
    fi
elif [[ "$WHAT_IF" == true ]]; then
    print_message "$BLUE" "Running what-if deployment..."
    az deployment sub what-if \
        --location "$LOCATION" \
        --template-file "$MAIN_BICEP" \
        --parameters "$TEMP_PARAMS" \
        --parameters postgresAdminUsername="$POSTGRES_ADMIN_USERNAME" \
        --parameters postgresAdminPassword="temp-password-for-whatif" \
        --name "$DEPLOYMENT_NAME"
else
    print_message "$BLUE" "Starting deployment: $DEPLOYMENT_NAME"
    print_message "$BLUE" "Environment: $ENVIRONMENT"
    print_message "$BLUE" "Location: $LOCATION"
    echo

    # Confirm deployment
    read -p "Proceed with deployment? (y/n): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        print_message "$YELLOW" "Deployment cancelled"
        rm -f "$TEMP_PARAMS"
        exit 0
    fi

    # Run deployment
    print_message "$BLUE" "Deploying infrastructure..."
    az deployment sub create \
        --location "$LOCATION" \
        --template-file "$MAIN_BICEP" \
        --parameters "$TEMP_PARAMS" \
        --parameters postgresAdminUsername="$POSTGRES_ADMIN_USERNAME" \
        --parameters postgresAdminPassword="$POSTGRES_ADMIN_PASSWORD" \
        --name "$DEPLOYMENT_NAME" \
        --output table

    if [[ $? -eq 0 ]]; then
        print_message "$GREEN" "✓ Deployment successful!"

        # Get outputs
        print_message "$BLUE" "Retrieving deployment outputs..."
        az deployment sub show \
            --name "$DEPLOYMENT_NAME" \
            --query "properties.outputs" \
            --output json > "${ENVIRONMENT}-outputs.json"

        print_message "$GREEN" "Outputs saved to: ${ENVIRONMENT}-outputs.json"

        # Store PostgreSQL password in Key Vault
        if [[ -n "$POSTGRES_ADMIN_PASSWORD" ]]; then
            KV_NAME="kv-numbatwallet-${ENVIRONMENT}-aue"
            print_message "$BLUE" "Storing PostgreSQL password in Key Vault: $KV_NAME"
            az keyvault secret set \
                --vault-name "$KV_NAME" \
                --name "PostgresAdminPassword" \
                --value "$POSTGRES_ADMIN_PASSWORD" \
                --output none
            print_message "$GREEN" "✓ Password stored securely in Key Vault"
        fi
    else
        print_message "$RED" "✗ Deployment failed!"
        rm -f "$TEMP_PARAMS"
        exit 1
    fi
fi

# Clean up
rm -f "$TEMP_PARAMS"

print_message "$GREEN" "╔══════════════════════════════════════════════════════╗"
print_message "$GREEN" "║            Deployment Complete! 🎉                   ║"
print_message "$GREEN" "╚══════════════════════════════════════════════════════╝"
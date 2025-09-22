#!/bin/bash
# Deploy NumbatWallet Infrastructure - Development Environment
# HSM Provider: Software (file-based keys for development)

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
ENVIRONMENT="dev"
LOCATION="australiaeast"
LOCATION_CODE="aue"
SUBSCRIPTION_NAME="NumbatWallet-Development"
RESOURCE_GROUP="rg-numbatwallet-${ENVIRONMENT}-${LOCATION_CODE}"

# Function to print colored output
print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check prerequisites
check_prerequisites() {
    print_info "Checking prerequisites..."

    # Check Azure CLI
    if ! command -v az &> /dev/null; then
        print_error "Azure CLI is not installed. Please install it first."
        exit 1
    fi

    # Check if logged in
    if ! az account show &> /dev/null; then
        print_error "Not logged in to Azure. Please run 'az login' first."
        exit 1
    fi

    print_success "Prerequisites check passed"
}

# Set subscription
set_subscription() {
    print_info "Setting Azure subscription..."

    if az account set --subscription "$SUBSCRIPTION_NAME" 2>/dev/null; then
        print_success "Subscription set to: $SUBSCRIPTION_NAME"
    else
        print_warning "Subscription '$SUBSCRIPTION_NAME' not found. Using current subscription."
    fi

    CURRENT_SUB=$(az account show --query name -o tsv)
    print_info "Current subscription: $CURRENT_SUB"
}

# Get current user object ID
get_user_object_id() {
    print_info "Getting current user object ID..."
    USER_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv)
    print_success "User object ID: $USER_OBJECT_ID"
}

# Create resource group
create_resource_group() {
    print_info "Creating resource group: $RESOURCE_GROUP..."

    if az group exists --name "$RESOURCE_GROUP" | grep -q true; then
        print_warning "Resource group already exists"
    else
        az group create \
            --name "$RESOURCE_GROUP" \
            --location "$LOCATION" \
            --tags Environment="Development" Application="NumbatWallet" ManagedBy="Bicep"
        print_success "Resource group created"
    fi
}

# Deploy infrastructure
deploy_infrastructure() {
    print_info "Starting infrastructure deployment..."
    print_info "Environment: $ENVIRONMENT"
    print_info "HSM Provider: Software (Development)"
    print_info "Location: $LOCATION"

    # Generate secure passwords
    POSTGRES_PASSWORD=$(openssl rand -base64 32)
    JWT_KEY=$(openssl rand -base64 32)

    # Save credentials securely
    CREDS_FILE="./deploy-output-${ENVIRONMENT}-$(date +%Y%m%d-%H%M%S).json"

    # Deploy Bicep template
    DEPLOYMENT_NAME="numbatwallet-${ENVIRONMENT}-$(date +%Y%m%d%H%M%S)"

    print_info "Deploying Bicep templates..."

    az deployment sub create \
        --name "$DEPLOYMENT_NAME" \
        --location "$LOCATION" \
        --template-file "../main.bicep" \
        --parameters \
            environment="$ENVIRONMENT" \
            location="$LOCATION" \
            locationCode="$LOCATION_CODE" \
            administratorObjectId="$USER_OBJECT_ID" \
            managedIdentityObjectId="$USER_OBJECT_ID" \
            postgresAdminUsername="nwadmin" \
            postgresAdminPassword="$POSTGRES_PASSWORD" \
            jwtSigningKey="$JWT_KEY" \
            enablePrivateEndpoints=false \
            hsmProvider="Software" \
            enableEnvelopeEncryption=false \
            enableTenantIsolation=true \
            maxTenants=10 \
            enableKeyRotation=false \
            deployManagedHsm=false \
        --output json > "$CREDS_FILE"

    print_success "Deployment completed successfully!"
    print_warning "Credentials saved to: $CREDS_FILE"
    print_warning "IMPORTANT: Store this file securely and delete after saving credentials to Key Vault"
}

# Configure application settings
configure_app_settings() {
    print_info "Generating application configuration..."

    cat > "./appsettings.Development.json" << EOF
{
  "Hsm": {
    "Provider": "Software",
    "EnablePermanentDelete": true
  },
  "SoftwareHsm": {
    "KeyStorePath": "/tmp/numbatwallet/keys",
    "MasterKeyPassword": "DevOnly-ChangeInProduction!"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=numbatwallet_dev;Username=nwadmin;Password=DevPassword123!;",
    "RedisCache": "localhost:6379"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "Environment": "Development",
  "EnableSwagger": true,
  "EnableDetailedErrors": true
}
EOF

    print_success "Application configuration generated: ./appsettings.Development.json"
}

# Main execution
main() {
    print_info "========================================="
    print_info "NumbatWallet Infrastructure Deployment"
    print_info "Environment: DEVELOPMENT"
    print_info "========================================="

    check_prerequisites
    set_subscription
    get_user_object_id
    create_resource_group
    deploy_infrastructure
    configure_app_settings

    print_info "========================================="
    print_success "Deployment completed successfully!"
    print_info "Next steps:"
    print_info "1. Review the deployment output file"
    print_info "2. Update your local appsettings with connection strings"
    print_info "3. Run database migrations"
    print_info "4. Start the application"
    print_info "========================================="
}

# Run main function
main "$@"
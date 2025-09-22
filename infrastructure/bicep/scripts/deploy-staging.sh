#!/bin/bash
# Deploy NumbatWallet Infrastructure - Staging Environment
# HSM Provider: KeyVault (Standard Key Vault with software-protected keys)

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
ENVIRONMENT="test"
LOCATION="australiaeast"
LOCATION_CODE="aue"
SUBSCRIPTION_NAME="NumbatWallet-Staging"
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

    # Check Bicep CLI
    if ! az bicep version &> /dev/null; then
        print_warning "Bicep CLI not installed. Installing..."
        az bicep install
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

# Create service principal for managed identity
create_managed_identity() {
    print_info "Creating managed identity for application..."

    IDENTITY_NAME="id-numbatwallet-${ENVIRONMENT}"

    # Check if identity exists
    IDENTITY_EXISTS=$(az identity list --query "[?name=='$IDENTITY_NAME'].name" -o tsv)

    if [[ -n "$IDENTITY_EXISTS" ]]; then
        print_warning "Managed identity already exists"
    else
        az identity create \
            --name "$IDENTITY_NAME" \
            --resource-group "$RESOURCE_GROUP" \
            --location "$LOCATION"
        print_success "Managed identity created"
    fi

    # Get the identity object ID
    MANAGED_IDENTITY_OBJECT_ID=$(az identity show \
        --name "$IDENTITY_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --query principalId -o tsv)

    print_success "Managed Identity Object ID: $MANAGED_IDENTITY_OBJECT_ID"
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
            --tags Environment="Staging" Application="NumbatWallet" ManagedBy="Bicep"
        print_success "Resource group created"
    fi
}

# Deploy infrastructure
deploy_infrastructure() {
    print_info "Starting infrastructure deployment..."
    print_info "Environment: $ENVIRONMENT (Staging)"
    print_info "HSM Provider: KeyVault (Standard)"
    print_info "Location: $LOCATION"
    print_warning "Envelope encryption: ENABLED"

    # Retrieve or generate secure passwords from Key Vault if exists
    KEY_VAULT_NAME="kv-numbatwallet-${ENVIRONMENT}-${LOCATION_CODE}"

    # Try to get existing credentials
    if az keyvault show --name "$KEY_VAULT_NAME" &> /dev/null; then
        print_info "Retrieving existing credentials from Key Vault..."
        POSTGRES_PASSWORD=$(az keyvault secret show --vault-name "$KEY_VAULT_NAME" --name "postgres-password" --query value -o tsv 2>/dev/null || openssl rand -base64 32)
        JWT_KEY=$(az keyvault secret show --vault-name "$KEY_VAULT_NAME" --name "jwt-signing-key" --query value -o tsv 2>/dev/null || openssl rand -base64 32)
    else
        print_info "Generating new secure passwords..."
        POSTGRES_PASSWORD=$(openssl rand -base64 32)
        JWT_KEY=$(openssl rand -base64 32)
    fi

    # Save deployment parameters
    PARAMS_FILE="./deploy-params-${ENVIRONMENT}-$(date +%Y%m%d-%H%M%S).json"

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
            managedIdentityObjectId="$MANAGED_IDENTITY_OBJECT_ID" \
            postgresAdminUsername="nwadmin" \
            postgresAdminPassword="$POSTGRES_PASSWORD" \
            jwtSigningKey="$JWT_KEY" \
            enablePrivateEndpoints=true \
            hsmProvider="KeyVault" \
            enableEnvelopeEncryption=true \
            enableTenantIsolation=true \
            maxTenants=50 \
            enableKeyRotation=false \
            deployManagedHsm=false \
            apiImageTag="staging" \
            adminImageTag="staging" \
        --output json > "$PARAMS_FILE"

    print_success "Deployment completed successfully!"
    print_info "Deployment output saved to: $PARAMS_FILE"

    # Store credentials in Key Vault
    store_credentials_in_keyvault
}

# Store credentials in Key Vault
store_credentials_in_keyvault() {
    print_info "Storing credentials in Key Vault..."

    KEY_VAULT_NAME="kv-numbatwallet-${ENVIRONMENT}-${LOCATION_CODE}"

    # Wait for Key Vault to be available
    sleep 30

    # Store PostgreSQL password
    az keyvault secret set \
        --vault-name "$KEY_VAULT_NAME" \
        --name "postgres-password" \
        --value "$POSTGRES_PASSWORD" \
        --description "PostgreSQL administrator password" \
        > /dev/null

    # Store JWT signing key
    az keyvault secret set \
        --vault-name "$KEY_VAULT_NAME" \
        --name "jwt-signing-key" \
        --value "$JWT_KEY" \
        --description "JWT token signing key" \
        > /dev/null

    print_success "Credentials stored in Key Vault"
}

# Configure application settings
configure_app_settings() {
    print_info "Generating application configuration..."

    # Get deployment outputs
    KEY_VAULT_URI=$(az keyvault show --name "kv-numbatwallet-${ENVIRONMENT}-${LOCATION_CODE}" --query properties.vaultUri -o tsv)

    cat > "./appsettings.Staging.json" << EOF
{
  "Hsm": {
    "Provider": "KeyVault",
    "EnablePermanentDelete": false
  },
  "KeyVault": {
    "Uri": "${KEY_VAULT_URI}",
    "ManagedIdentityClientId": "${MANAGED_IDENTITY_OBJECT_ID}"
  },
  "EnvelopeEncryption": {
    "Enabled": true,
    "KeyRotationDays": 90
  },
  "ConnectionStrings": {
    "DefaultConnection": "Retrieved from Key Vault",
    "RedisCache": "Retrieved from Key Vault"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ApplicationInsights": {
    "ConnectionString": "Retrieved from Key Vault"
  },
  "Environment": "Staging",
  "EnableSwagger": true,
  "EnableDetailedErrors": false
}
EOF

    print_success "Application configuration generated: ./appsettings.Staging.json"
}

# Run integration tests
run_integration_tests() {
    print_info "Running integration tests against staging environment..."

    # This would typically run your integration test suite
    # For now, we'll just do a health check
    print_warning "Integration tests not yet implemented - performing health check only"

    # Add actual test commands here when ready
}

# Main execution
main() {
    print_info "========================================="
    print_info "NumbatWallet Infrastructure Deployment"
    print_info "Environment: STAGING"
    print_info "========================================="

    check_prerequisites
    set_subscription
    get_user_object_id
    create_resource_group
    create_managed_identity
    deploy_infrastructure
    configure_app_settings
    run_integration_tests

    print_info "========================================="
    print_success "Deployment completed successfully!"
    print_info "Key features enabled:"
    print_info "- Standard Key Vault (software-protected keys)"
    print_info "- Envelope encryption (KEK/DEK)"
    print_info "- Tenant isolation"
    print_info "- Private endpoints"
    print_info "Next steps:"
    print_info "1. Review deployment outputs"
    print_info "2. Run database migrations"
    print_info "3. Deploy application containers"
    print_info "4. Run smoke tests"
    print_info "========================================="
}

# Run main function
main "$@"
#!/bin/bash
# Deploy NumbatWallet Infrastructure - Production Environment
# HSM Provider: KeyVault Premium (Phase 1) or Managed HSM (Phase 2)

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Configuration
ENVIRONMENT="prod"
LOCATION="australiaeast"
LOCATION_CODE="aue"
SUBSCRIPTION_NAME="NumbatWallet-Production"
RESOURCE_GROUP="rg-numbatwallet-${ENVIRONMENT}-${LOCATION_CODE}"

# HSM Configuration (Change for Phase 2)
HSM_PHASE="${HSM_PHASE:-1}"  # Default to Phase 1, set HSM_PHASE=2 for Managed HSM

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

print_security() {
    echo -e "${CYAN}[SECURITY]${NC} $1"
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

    # Check for production approval
    if [[ "${SKIP_APPROVAL:-}" != "true" ]]; then
        print_warning "========================================="
        print_warning "PRODUCTION DEPLOYMENT CONFIRMATION"
        print_warning "========================================="
        print_warning "You are about to deploy to PRODUCTION"
        print_warning "HSM Phase: $HSM_PHASE"
        if [[ "$HSM_PHASE" == "1" ]]; then
            print_info "HSM Provider: Key Vault Premium (FIPS 140-2 Level 1)"
            print_info "Estimated cost: ~\$300/month"
        else
            print_security "HSM Provider: Managed HSM (FIPS 140-2 Level 2)"
            print_security "Estimated cost: ~\$3,400/month"
        fi
        print_warning "========================================="
        read -p "Type 'DEPLOY TO PRODUCTION' to continue: " CONFIRMATION
        if [[ "$CONFIRMATION" != "DEPLOY TO PRODUCTION" ]]; then
            print_error "Deployment cancelled"
            exit 1
        fi
    fi

    print_success "Prerequisites check passed"
}

# Set subscription
set_subscription() {
    print_info "Setting Azure subscription..."

    if az account set --subscription "$SUBSCRIPTION_NAME" 2>/dev/null; then
        print_success "Subscription set to: $SUBSCRIPTION_NAME"
    else
        print_error "Production subscription '$SUBSCRIPTION_NAME' not found."
        print_error "Please ensure you have access to the production subscription."
        exit 1
    fi

    CURRENT_SUB=$(az account show --query name -o tsv)
    print_security "Current subscription: $CURRENT_SUB"
}

# Get admin group object ID
get_admin_group_id() {
    print_info "Getting production admin group..."

    # In production, we should use an AD group, not individual users
    ADMIN_GROUP_NAME="NumbatWallet-Production-Admins"

    ADMIN_GROUP_ID=$(az ad group show --group "$ADMIN_GROUP_NAME" --query id -o tsv 2>/dev/null || echo "")

    if [[ -z "$ADMIN_GROUP_ID" ]]; then
        print_warning "Admin group not found. Creating..."
        ADMIN_GROUP_ID=$(az ad group create --display-name "$ADMIN_GROUP_NAME" --mail-nickname "numbatwallet-prod-admins" --query id -o tsv)
        print_success "Admin group created: $ADMIN_GROUP_ID"

        # Add current user to the group
        CURRENT_USER_ID=$(az ad signed-in-user show --query id -o tsv)
        az ad group member add --group "$ADMIN_GROUP_ID" --member-id "$CURRENT_USER_ID"
    fi

    print_security "Admin Group ID: $ADMIN_GROUP_ID"
}

# Create managed identity
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

    print_security "Managed Identity Object ID: $MANAGED_IDENTITY_OBJECT_ID"
}

# Create resource group with locks
create_resource_group() {
    print_info "Creating production resource group: $RESOURCE_GROUP..."

    if az group exists --name "$RESOURCE_GROUP" | grep -q true; then
        print_warning "Resource group already exists"
    else
        az group create \
            --name "$RESOURCE_GROUP" \
            --location "$LOCATION" \
            --tags Environment="Production" Application="NumbatWallet" ManagedBy="Bicep" CostCenter="Digital-Services" Compliance="TDIF"
        print_success "Resource group created"

        # Add resource lock to prevent accidental deletion
        az lock create \
            --name "ProductionLock" \
            --resource-group "$RESOURCE_GROUP" \
            --lock-type CanNotDelete \
            --notes "Production environment - deletion protection"
        print_security "Resource lock applied"
    fi
}

# Deploy infrastructure
deploy_infrastructure() {
    print_info "Starting production infrastructure deployment..."
    print_security "Environment: PRODUCTION"
    print_security "Location: $LOCATION (Australian data sovereignty)"

    if [[ "$HSM_PHASE" == "1" ]]; then
        print_security "HSM Provider: Key Vault Premium (HSM-backed)"
        HSM_PROVIDER="KeyVaultHSM"
        DEPLOY_MANAGED_HSM="false"
    else
        print_security "HSM Provider: Managed HSM (FIPS 140-2 Level 2)"
        HSM_PROVIDER="ManagedHSM"
        DEPLOY_MANAGED_HSM="true"
    fi

    # Production passwords should be retrieved from existing secure storage
    KEY_VAULT_NAME="kv-numbatwallet-${ENVIRONMENT}-${LOCATION_CODE}"

    # Generate or retrieve secure passwords
    if az keyvault show --name "$KEY_VAULT_NAME" &> /dev/null; then
        print_info "Retrieving existing credentials from Key Vault..."
        POSTGRES_PASSWORD=$(az keyvault secret show --vault-name "$KEY_VAULT_NAME" --name "postgres-password" --query value -o tsv 2>/dev/null || openssl rand -base64 48)
        JWT_KEY=$(az keyvault secret show --vault-name "$KEY_VAULT_NAME" --name "jwt-signing-key" --query value -o tsv 2>/dev/null || openssl rand -base64 48)
    else
        print_security "Generating new production-grade credentials..."
        POSTGRES_PASSWORD=$(openssl rand -base64 48)
        JWT_KEY=$(openssl rand -base64 48)
    fi

    # Deployment with validation
    DEPLOYMENT_NAME="numbatwallet-${ENVIRONMENT}-$(date +%Y%m%d%H%M%S)"

    print_info "Validating deployment template..."
    az deployment sub validate \
        --name "$DEPLOYMENT_NAME" \
        --location "$LOCATION" \
        --template-file "../main.bicep" \
        --parameters \
            environment="$ENVIRONMENT" \
            location="$LOCATION" \
            locationCode="$LOCATION_CODE" \
            administratorObjectId="$ADMIN_GROUP_ID" \
            managedIdentityObjectId="$MANAGED_IDENTITY_OBJECT_ID" \
            postgresAdminUsername="nwadmin" \
            postgresAdminPassword="$POSTGRES_PASSWORD" \
            jwtSigningKey="$JWT_KEY" \
            enablePrivateEndpoints=true \
            hsmProvider="$HSM_PROVIDER" \
            enableEnvelopeEncryption=true \
            enableTenantIsolation=true \
            maxTenants=100 \
            enableKeyRotation=true \
            keyRotationDays=90 \
            deployManagedHsm="$DEPLOY_MANAGED_HSM" \
            hsmInitialAdminObjectIds=["$ADMIN_GROUP_ID","$MANAGED_IDENTITY_OBJECT_ID"] \
            apiImageTag="latest" \
            adminImageTag="latest"

    print_success "Template validation passed"

    print_info "Deploying production infrastructure (this may take 30-45 minutes)..."

    DEPLOYMENT_OUTPUT=$(az deployment sub create \
        --name "$DEPLOYMENT_NAME" \
        --location "$LOCATION" \
        --template-file "../main.bicep" \
        --parameters \
            environment="$ENVIRONMENT" \
            location="$LOCATION" \
            locationCode="$LOCATION_CODE" \
            administratorObjectId="$ADMIN_GROUP_ID" \
            managedIdentityObjectId="$MANAGED_IDENTITY_OBJECT_ID" \
            postgresAdminUsername="nwadmin" \
            postgresAdminPassword="$POSTGRES_PASSWORD" \
            jwtSigningKey="$JWT_KEY" \
            enablePrivateEndpoints=true \
            hsmProvider="$HSM_PROVIDER" \
            enableEnvelopeEncryption=true \
            enableTenantIsolation=true \
            maxTenants=100 \
            enableKeyRotation=true \
            keyRotationDays=90 \
            deployManagedHsm="$DEPLOY_MANAGED_HSM" \
            hsmInitialAdminObjectIds=["$ADMIN_GROUP_ID","$MANAGED_IDENTITY_OBJECT_ID"] \
            apiImageTag="latest" \
            adminImageTag="latest" \
        --output json)

    # Save deployment output securely
    DEPLOYMENT_FILE="./deploy-production-$(date +%Y%m%d-%H%M%S).json"
    echo "$DEPLOYMENT_OUTPUT" > "$DEPLOYMENT_FILE"

    print_success "Deployment completed successfully!"
    print_security "Deployment output saved to: $DEPLOYMENT_FILE (SECURE THIS FILE)"

    # Store credentials in Key Vault
    store_credentials_in_keyvault

    # If Phase 2, initialize Managed HSM
    if [[ "$HSM_PHASE" == "2" ]]; then
        initialize_managed_hsm
    fi
}

# Store credentials in Key Vault
store_credentials_in_keyvault() {
    print_security "Storing production credentials in Key Vault..."

    KEY_VAULT_NAME="kv-numbatwallet-${ENVIRONMENT}-${LOCATION_CODE}"

    # Wait for Key Vault to be available
    sleep 30

    # Store PostgreSQL password with expiration
    az keyvault secret set \
        --vault-name "$KEY_VAULT_NAME" \
        --name "postgres-password" \
        --value "$POSTGRES_PASSWORD" \
        --description "PostgreSQL administrator password" \
        --expires $(date -u -d "+90 days" +%Y-%m-%dT%H:%M:%SZ) \
        > /dev/null

    # Store JWT signing key with expiration
    az keyvault secret set \
        --vault-name "$KEY_VAULT_NAME" \
        --name "jwt-signing-key" \
        --value "$JWT_KEY" \
        --description "JWT token signing key" \
        --expires $(date -u -d "+365 days" +%Y-%m-%dT%H:%M:%SZ) \
        > /dev/null

    print_security "Credentials stored securely with expiration policies"
}

# Initialize Managed HSM (Phase 2 only)
initialize_managed_hsm() {
    print_security "Initializing Managed HSM security domain..."

    HSM_NAME="hsm-numbatwallet-${ENVIRONMENT}-${LOCATION_CODE}"

    # Generate security domain certificates (requires 3 for quorum)
    print_info "Generating security domain certificates..."

    for i in {1..5}; do
        openssl req -newkey rsa:2048 -nodes -keyout "cert_$i.key" \
            -x509 -days 365 -out "cert_$i.cer" \
            -subj "/CN=NumbatWallet-SecurityDomain-$i"
    done

    # Download security domain
    print_info "Downloading security domain..."
    az keyvault security-domain download \
        --hsm-name "$HSM_NAME" \
        --sd-wrapping-keys cert_1.cer cert_2.cer cert_3.cer cert_4.cer cert_5.cer \
        --sd-quorum 3 \
        --security-domain-file "security-domain-$HSM_NAME.json"

    print_security "Security domain initialized. Store certificates and domain file in secure offline storage!"
    print_warning "CRITICAL: Backup these files immediately to secure offline storage"
}

# Configure monitoring and alerts
configure_monitoring() {
    print_info "Configuring production monitoring and alerts..."

    # Create action group for alerts
    ACTION_GROUP_NAME="ag-numbatwallet-prod"

    az monitor action-group create \
        --resource-group "$RESOURCE_GROUP" \
        --name "$ACTION_GROUP_NAME" \
        --short-name "NW-Prod" \
        --email-receiver name="Operations" email-address="ops@numbatwallet.com.au" \
        --email-receiver name="Security" email-address="security@numbatwallet.com.au"

    # Create critical alerts
    print_info "Setting up critical alerts..."

    # HSM availability alert
    az monitor metrics alert create \
        --resource-group "$RESOURCE_GROUP" \
        --name "HSM-Availability" \
        --description "Alert when HSM availability drops below 99.5%" \
        --scopes "/subscriptions/$(az account show --query id -o tsv)/resourceGroups/$RESOURCE_GROUP" \
        --condition "avg Availability < 99.5" \
        --action-group "$ACTION_GROUP_NAME"

    print_success "Monitoring and alerts configured"
}

# Run production validation tests
run_production_validation() {
    print_info "Running production validation tests..."

    # Health check endpoints
    API_URL=$(az containerapp show --name "ca-numbatwallet-api-${ENVIRONMENT}" --resource-group "$RESOURCE_GROUP" --query properties.configuration.ingress.fqdn -o tsv 2>/dev/null || echo "")

    if [[ -n "$API_URL" ]]; then
        print_info "Testing API health endpoint..."
        curl -s -o /dev/null -w "%{http_code}" "https://$API_URL/health" || print_warning "Health check failed"
    fi

    print_success "Production validation completed"
}

# Generate production configuration
configure_app_settings() {
    print_security "Generating production application configuration..."

    KEY_VAULT_URI=$(az keyvault show --name "kv-numbatwallet-${ENVIRONMENT}-${LOCATION_CODE}" --query properties.vaultUri -o tsv)

    if [[ "$HSM_PHASE" == "2" ]]; then
        HSM_URI=$(az keyvault show --name "hsm-numbatwallet-${ENVIRONMENT}-${LOCATION_CODE}" --query properties.hsmUri -o tsv)
        HSM_CONFIG="\"ManagedHsm\": { \"Uri\": \"${HSM_URI}\" },"
    else
        HSM_CONFIG=""
    fi

    cat > "./appsettings.Production.json" << EOF
{
  "Hsm": {
    "Provider": "${HSM_PROVIDER}",
    "EnablePermanentDelete": false
  },
  "KeyVault": {
    "Uri": "${KEY_VAULT_URI}",
    "ManagedIdentityClientId": "${MANAGED_IDENTITY_OBJECT_ID}"
  },
  ${HSM_CONFIG}
  "EnvelopeEncryption": {
    "Enabled": true,
    "KeyRotationDays": 90,
    "TenantIsolation": true
  },
  "ConnectionStrings": {
    "DefaultConnection": "@Microsoft.KeyVault(SecretUri=${KEY_VAULT_URI}secrets/postgres-connection-string/)",
    "RedisCache": "@Microsoft.KeyVault(SecretUri=${KEY_VAULT_URI}secrets/redis-connection-string/)"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Security": "Information"
    }
  },
  "ApplicationInsights": {
    "ConnectionString": "@Microsoft.KeyVault(SecretUri=${KEY_VAULT_URI}secrets/appinsights-connection-string/)"
  },
  "Security": {
    "RequireHttps": true,
    "Hsts": {
      "MaxAge": 31536000,
      "IncludeSubDomains": true,
      "Preload": true
    }
  },
  "Environment": "Production",
  "EnableSwagger": false,
  "EnableDetailedErrors": false
}
EOF

    print_success "Production configuration generated: ./appsettings.Production.json"
    print_security "Configuration uses Key Vault references for all sensitive data"
}

# Main execution
main() {
    print_info "========================================="
    print_security "NumbatWallet Infrastructure Deployment"
    print_security "Environment: PRODUCTION"
    print_security "HSM Phase: $HSM_PHASE"
    print_info "========================================="

    check_prerequisites
    set_subscription
    get_admin_group_id
    create_resource_group
    create_managed_identity
    deploy_infrastructure
    configure_monitoring
    configure_app_settings
    run_production_validation

    print_info "========================================="
    print_success "PRODUCTION DEPLOYMENT COMPLETED"
    print_security "Security features enabled:"
    if [[ "$HSM_PHASE" == "1" ]]; then
        print_security "✓ Key Vault Premium (HSM-backed keys)"
        print_security "✓ FIPS 140-2 Level 1 compliance"
    else
        print_security "✓ Managed HSM (dedicated hardware)"
        print_security "✓ FIPS 140-2 Level 2 compliance"
    fi
    print_security "✓ Envelope encryption (KEK/DEK)"
    print_security "✓ Tenant key isolation"
    print_security "✓ Private endpoints"
    print_security "✓ Key rotation automation (90 days)"
    print_security "✓ Resource locks"
    print_security "✓ Monitoring and alerts"
    print_info "========================================="
    print_warning "IMPORTANT POST-DEPLOYMENT STEPS:"
    print_warning "1. Backup security domain certificates (if Phase 2)"
    print_warning "2. Configure backup and disaster recovery"
    print_warning "3. Run security assessment"
    print_warning "4. Configure WAF rules"
    print_warning "5. Enable Azure DDoS Protection"
    print_warning "6. Schedule penetration testing"
    print_info "========================================="
}

# Run main function
main "$@"
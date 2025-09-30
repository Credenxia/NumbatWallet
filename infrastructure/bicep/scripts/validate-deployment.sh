#!/bin/bash
# NumbatWallet Infrastructure Validation Script
# Validates deployed resources and performs health checks

set -euo pipefail

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Print colored messages
print_message() {
    echo -e "${1}${2}${NC}"
}

# Check resource existence
check_resource() {
    local resource_type=$1
    local resource_name=$2
    local resource_group=$3

    print_message "$YELLOW" "Checking $resource_type: $resource_name..."

    if az resource show \
        --resource-group "$resource_group" \
        --resource-type "$resource_type" \
        --name "$resource_name" &> /dev/null; then
        print_message "$GREEN" "✓ $resource_name exists"
        return 0
    else
        print_message "$RED" "✗ $resource_name not found"
        return 1
    fi
}

# Test Container App health endpoint
test_health_endpoint() {
    local app_url=$1
    local endpoint=$2

    print_message "$YELLOW" "Testing health endpoint: ${app_url}${endpoint}..."

    response=$(curl -s -o /dev/null -w "%{http_code}" "https://${app_url}${endpoint}" || echo "000")

    if [[ "$response" == "200" ]]; then
        print_message "$GREEN" "✓ Health endpoint responding (HTTP $response)"
        return 0
    else
        print_message "$RED" "✗ Health endpoint not responding (HTTP $response)"
        return 1
    fi
}

# Test database connectivity
test_database_connection() {
    local server_fqdn=$1
    local admin_user=$2

    print_message "$YELLOW" "Testing PostgreSQL connectivity..."

    if command -v psql &> /dev/null; then
        if psql -h "$server_fqdn" -U "$admin_user" -d postgres -c "SELECT 1;" &> /dev/null; then
            print_message "$GREEN" "✓ Database connection successful"
            return 0
        else
            print_message "$RED" "✗ Database connection failed"
            return 1
        fi
    else
        print_message "$YELLOW" "⚠ psql not installed, skipping database test"
        return 0
    fi
}

# Main validation function
main() {
    ENVIRONMENT="${1:-}"

    if [[ -z "$ENVIRONMENT" ]]; then
        print_message "$RED" "Usage: $0 <environment>"
        exit 1
    fi

    print_message "$GREEN" "====================================="
    print_message "$GREEN" "NumbatWallet Infrastructure Validator"
    print_message "$GREEN" "====================================="
    print_message "$YELLOW" "Environment: $ENVIRONMENT"

    # Set resource group name
    RG_NAME="rg-numbatwallet-${ENVIRONMENT}-aue"

    # Check if resource group exists
    if ! az group exists --name "$RG_NAME" | grep -q true; then
        print_message "$RED" "Resource group not found: $RG_NAME"
        exit 1
    fi

    print_message "$GREEN" "Resource group found: $RG_NAME"

    # Load deployment outputs
    OUTPUTS_FILE="../outputs/${ENVIRONMENT}-outputs.json"

    if [[ ! -f "$OUTPUTS_FILE" ]]; then
        print_message "$RED" "Outputs file not found: $OUTPUTS_FILE"
        print_message "$YELLOW" "Getting outputs from Azure..."

        # Try to get latest deployment
        deployment=$(az deployment group list \
            --resource-group "$RG_NAME" \
            --query "[0].name" -o tsv)

        if [[ -n "$deployment" ]]; then
            az deployment group show \
                --resource-group "$RG_NAME" \
                --name "$deployment" \
                --query properties.outputs \
                -o json > "$OUTPUTS_FILE"
        else
            print_message "$RED" "No deployments found"
            exit 1
        fi
    fi

    # Parse outputs
    API_URL=$(jq -r '.apiAppUrl.value // empty' "$OUTPUTS_FILE")
    ADMIN_URL=$(jq -r '.adminAppUrl.value // empty' "$OUTPUTS_FILE")
    KV_URI=$(jq -r '.keyVaultUri.value // empty' "$OUTPUTS_FILE")
    PG_FQDN=$(jq -r '.postgresServerFqdn.value // empty' "$OUTPUTS_FILE")

    print_message "$YELLOW" "\nValidating resources..."

    # Validate core resources
    ERRORS=0

    # Check Key Vault
    check_resource "Microsoft.KeyVault/vaults" "kv-numbatwallet-${ENVIRONMENT}-aue" "$RG_NAME" || ((ERRORS++))

    # Check PostgreSQL
    check_resource "Microsoft.DBforPostgreSQL/flexibleServers" "psql-numbatwallet-${ENVIRONMENT}-aue" "$RG_NAME" || ((ERRORS++))

    # Check Redis
    check_resource "Microsoft.Cache/redis" "redis-numbatwallet-${ENVIRONMENT}-aue" "$RG_NAME" || ((ERRORS++))

    # Check Container Apps Environment
    check_resource "Microsoft.App/managedEnvironments" "cae-numbatwallet-${ENVIRONMENT}-aue" "$RG_NAME" || ((ERRORS++))

    # Check Container Registry
    check_resource "Microsoft.ContainerRegistry/registries" "crnumbatwallet${ENVIRONMENT}aue" "$RG_NAME" || ((ERRORS++))

    # Check Storage Account
    check_resource "Microsoft.Storage/storageAccounts" "stnumbatwallet${ENVIRONMENT}aue" "$RG_NAME" || ((ERRORS++))

    # Test endpoints if available
    if [[ -n "$API_URL" ]]; then
        print_message "$YELLOW" "\nTesting API endpoints..."
        test_health_endpoint "$API_URL" "/health/live" || ((ERRORS++))
        test_health_endpoint "$API_URL" "/health/ready" || ((ERRORS++))
    fi

    if [[ -n "$ADMIN_URL" ]]; then
        print_message "$YELLOW" "\nTesting Admin endpoints..."
        test_health_endpoint "$ADMIN_URL" "/health" || ((ERRORS++))
    fi

    # Summary
    print_message "$YELLOW" "\n====================================="

    if [[ $ERRORS -eq 0 ]]; then
        print_message "$GREEN" "✓ All validation checks passed!"
    else
        print_message "$RED" "✗ $ERRORS validation checks failed"
        exit 1
    fi

    # Display key endpoints
    print_message "$GREEN" "\nDeployment Endpoints:"
    [[ -n "$API_URL" ]] && echo "  API: https://$API_URL"
    [[ -n "$ADMIN_URL" ]] && echo "  Admin: https://$ADMIN_URL"
    [[ -n "$KV_URI" ]] && echo "  Key Vault: $KV_URI"
}

main "$@"
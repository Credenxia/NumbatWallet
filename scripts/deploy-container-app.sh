#!/bin/bash
# Deploy NumbatWallet API to Azure Container Apps
# POA: Issue #37 - Deploy backend to Azure Container Apps

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Default values
ENVIRONMENT=${1:-dev}
RESOURCE_GROUP="rg-numbatwallet-${ENVIRONMENT}"
LOCATION=${LOCATION:-australiaeast}
ACR_NAME="numbatwallet"
IMAGE_NAME="numbatwallet-api"
IMAGE_TAG=${2:-latest}

echo -e "${GREEN}🚀 Deploying NumbatWallet API to Azure Container Apps${NC}"
echo -e "Environment: ${YELLOW}${ENVIRONMENT}${NC}"
echo -e "Resource Group: ${YELLOW}${RESOURCE_GROUP}${NC}"
echo -e "Image: ${YELLOW}${ACR_NAME}.azurecr.io/${IMAGE_NAME}:${IMAGE_TAG}${NC}"

# Function to check if logged in to Azure
check_azure_login() {
    if ! az account show &> /dev/null; then
        echo -e "${RED}❌ Not logged in to Azure. Please run 'az login'${NC}"
        exit 1
    fi
    echo -e "${GREEN}✅ Azure login verified${NC}"
}

# Function to create resource group if it doesn't exist
ensure_resource_group() {
    if ! az group show -n $RESOURCE_GROUP &> /dev/null; then
        echo -e "${YELLOW}Creating resource group ${RESOURCE_GROUP}...${NC}"
        az group create -n $RESOURCE_GROUP -l $LOCATION
    fi
    echo -e "${GREEN}✅ Resource group ready${NC}"
}

# Function to check if ACR exists
check_acr() {
    if ! az acr show -n $ACR_NAME &> /dev/null; then
        echo -e "${RED}❌ Azure Container Registry ${ACR_NAME} not found${NC}"
        echo -e "${YELLOW}Create it with: az acr create -n ${ACR_NAME} -g ${RESOURCE_GROUP} --sku Basic${NC}"
        exit 1
    fi
    echo -e "${GREEN}✅ Container registry verified${NC}"
}

# Function to build and push Docker image
build_and_push_image() {
    echo -e "${YELLOW}Building Docker image...${NC}"

    # Build from repository root
    cd $(dirname "$0")/..

    docker build -f src/NumbatWallet.Web.Api/Dockerfile \
        -t ${ACR_NAME}.azurecr.io/${IMAGE_NAME}:${IMAGE_TAG} \
        -t ${ACR_NAME}.azurecr.io/${IMAGE_NAME}:latest \
        .

    echo -e "${GREEN}✅ Docker image built${NC}"

    # Login to ACR
    echo -e "${YELLOW}Logging in to Azure Container Registry...${NC}"
    az acr login -n $ACR_NAME

    # Push image
    echo -e "${YELLOW}Pushing image to registry...${NC}"
    docker push ${ACR_NAME}.azurecr.io/${IMAGE_NAME}:${IMAGE_TAG}

    if [ "$IMAGE_TAG" != "latest" ]; then
        docker push ${ACR_NAME}.azurecr.io/${IMAGE_NAME}:latest
    fi

    echo -e "${GREEN}✅ Image pushed to registry${NC}"
}

# Function to get secrets from environment or prompt
get_secrets() {
    if [ -z "$POSTGRES_CONNECTION" ]; then
        read -p "PostgreSQL Connection String: " -s POSTGRES_CONNECTION
        echo
    fi

    if [ -z "$KEYVAULT_URI" ]; then
        read -p "Key Vault URI (e.g., https://kv-numbatwallet-dev.vault.azure.net): " KEYVAULT_URI
    fi

    if [ -z "$APPINSIGHTS_CONNECTION" ]; then
        read -p "Application Insights Connection String: " -s APPINSIGHTS_CONNECTION
        echo
    fi
}

# Function to deploy using Bicep
deploy_bicep() {
    echo -e "${YELLOW}Deploying Container Apps infrastructure...${NC}"

    # Set replica counts based on environment
    if [ "$ENVIRONMENT" == "prod" ]; then
        MIN_REPLICAS=2
        MAX_REPLICAS=10
    else
        MIN_REPLICAS=1
        MAX_REPLICAS=3
    fi

    # Deploy Bicep template
    DEPLOYMENT_OUTPUT=$(az deployment group create \
        --resource-group $RESOURCE_GROUP \
        --template-file infra/bicep/container-apps.bicep \
        --parameters \
            environment=$ENVIRONMENT \
            containerImage=${ACR_NAME}.azurecr.io/${IMAGE_NAME}:${IMAGE_TAG} \
            containerRegistryName=$ACR_NAME \
            postgresConnectionString="$POSTGRES_CONNECTION" \
            keyVaultUri="$KEYVAULT_URI" \
            appInsightsConnectionString="$APPINSIGHTS_CONNECTION" \
            minReplicas=$MIN_REPLICAS \
            maxReplicas=$MAX_REPLICAS \
        --query "properties.outputs" -o json)

    # Extract outputs
    APP_URL=$(echo $DEPLOYMENT_OUTPUT | jq -r '.containerAppUrl.value')
    APP_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.containerAppId.value')
    IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityClientId.value')

    echo -e "${GREEN}✅ Deployment completed${NC}"
    echo -e "App URL: ${YELLOW}${APP_URL}${NC}"
    echo -e "App ID: ${APP_ID}"
    echo -e "Managed Identity Client ID: ${IDENTITY_CLIENT_ID}"
}

# Function to test deployment
test_deployment() {
    echo -e "${YELLOW}Testing deployment...${NC}"

    # Wait for app to be ready
    sleep 30

    # Test health endpoint
    if curl -f "${APP_URL}/health" &> /dev/null; then
        echo -e "${GREEN}✅ Health check passed${NC}"
    else
        echo -e "${RED}❌ Health check failed${NC}"
        return 1
    fi

    # Test readiness endpoint
    if curl -f "${APP_URL}/ready" &> /dev/null; then
        echo -e "${GREEN}✅ Readiness check passed${NC}"
    else
        echo -e "${RED}❌ Readiness check failed${NC}"
        return 1
    fi

    echo -e "${GREEN}✅ All tests passed${NC}"
}

# Main execution
main() {
    echo -e "${GREEN}=== NumbatWallet Container Apps Deployment ===${NC}"

    # Check prerequisites
    check_azure_login
    ensure_resource_group
    check_acr

    # Build and push image
    read -p "Build and push Docker image? (y/n) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        build_and_push_image
    fi

    # Get secrets
    get_secrets

    # Deploy
    deploy_bicep

    # Test
    test_deployment

    echo -e "${GREEN}=== Deployment Complete ===${NC}"
    echo -e "Your API is available at: ${YELLOW}${APP_URL}${NC}"
    echo -e "GraphQL endpoint: ${YELLOW}${APP_URL}/graphql${NC}"
    echo -e "REST endpoints: ${YELLOW}${APP_URL}/api/v1/*${NC}"
}

# Run main function
main
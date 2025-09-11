// NumbatWallet POA Infrastructure as Code
// Version: 1.0
// Date: September 10, 2025
// Description: Main Bicep template for POA deployment with environment-based scaling

targetScope = 'subscription'

// ==================== PARAMETERS ====================
@description('Environment name (dev, test, demo, prod)')
@allowed(['dev', 'test', 'demo', 'prod'])
param environment string = 'dev'

@description('Azure region for all resources')
param location string = 'australiaeast'

@description('Resource group name')
param resourceGroupName string = 'rg-numbat-poa-${environment}'

@description('Project name prefix')
param projectName string = 'numbat'

@description('Tenant ID for multi-tenancy')
param tenantId string = 'poa-tenant'

@description('Administrator email for alerts')
param adminEmail string

@description('Enable auto-scaling')
param enableAutoScaling bool = environment == 'prod' || environment == 'demo'

@description('Tags for all resources')
param tags object = {
  Project: 'NumbatWallet'
  Environment: environment
  Phase: 'POA'
  ManagedBy: 'Bicep'
  CreatedDate: utcNow('yyyy-MM-dd')
}

// ==================== VARIABLES ====================
// Environment-based sizing configuration
var environmentConfig = {
  dev: {
    // Minimal resources for development
    containerCpu: '0.25'
    containerMemory: '0.5Gi'
    containerMinReplicas: 1
    containerMaxReplicas: 2
    postgresqlSku: 'Standard_B1ms'  // 1 vCore, 2GB RAM
    postgresqlStorage: 32
    redisSku: 'Basic'
    redisCapacity: 0  // 250MB
    serviceBusSku: 'Basic'
  }
  test: {
    // Slightly more for testing
    containerCpu: '0.5'
    containerMemory: '1Gi'
    containerMinReplicas: 1
    containerMaxReplicas: 3
    postgresqlSku: 'Standard_B2s'  // 2 vCores, 4GB RAM
    postgresqlStorage: 64
    redisSku: 'Basic'
    redisCapacity: 1  // 1GB
    serviceBusSku: 'Standard'
  }
  demo: {
    // Demo/POA showcase environment
    containerCpu: '1'
    containerMemory: '2Gi'
    containerMinReplicas: 2
    containerMaxReplicas: 5
    postgresqlSku: 'Standard_D2ds_v5'  // 2 vCores, 8GB RAM
    postgresqlStorage: 128
    redisSku: 'Standard'
    redisCapacity: 1  // 1GB
    serviceBusSku: 'Standard'
  }
  prod: {
    // Production-ready configuration
    containerCpu: '2'
    containerMemory: '4Gi'
    containerMinReplicas: 3
    containerMaxReplicas: 10
    postgresqlSku: 'Standard_D4ds_v5'  // 4 vCores, 16GB RAM
    postgresqlStorage: 256
    redisSku: 'Standard'
    redisCapacity: 6  // 6GB
    serviceBusSku: 'Premium'
  }
}

var config = environmentConfig[environment]
var uniqueSuffix = uniqueString(subscription().id, resourceGroupName)
var namingPrefix = '${projectName}-${environment}'

// ==================== RESOURCE GROUP ====================
resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

// ==================== MODULES ====================
// Networking Module
module networking 'modules/networking.bicep' = {
  scope: rg
  name: 'networking-deployment'
  params: {
    location: location
    namingPrefix: namingPrefix
    tags: tags
    environment: environment
  }
}

// Security Module (Key Vault and Managed Identity)
module security 'modules/security.bicep' = {
  scope: rg
  name: 'security-deployment'
  params: {
    location: location
    namingPrefix: namingPrefix
    uniqueSuffix: uniqueSuffix
    tags: tags
    adminEmail: adminEmail
  }
}

// Storage Module
module storage 'modules/storage.bicep' = {
  scope: rg
  name: 'storage-deployment'
  params: {
    location: location
    namingPrefix: namingPrefix
    uniqueSuffix: uniqueSuffix
    tags: tags
    subnetId: networking.outputs.storageSubnetId
  }
}

// Database Module (PostgreSQL)
module database 'modules/database.bicep' = {
  scope: rg
  name: 'database-deployment'
  params: {
    location: location
    namingPrefix: namingPrefix
    tags: tags
    skuName: config.postgresqlSku
    storageSizeGB: config.postgresqlStorage
    subnetId: networking.outputs.databaseSubnetId
    keyVaultName: security.outputs.keyVaultName
    environment: environment
  }
}

// Cache Module (Redis)
module cache 'modules/cache.bicep' = {
  scope: rg
  name: 'cache-deployment'
  params: {
    location: location
    namingPrefix: namingPrefix
    tags: tags
    skuName: config.redisSku
    capacity: config.redisCapacity
    subnetId: networking.outputs.cacheSubnetId
    keyVaultName: security.outputs.keyVaultName
  }
}

// Messaging Module (Service Bus)
module messaging 'modules/messaging.bicep' = {
  scope: rg
  name: 'messaging-deployment'
  params: {
    location: location
    namingPrefix: namingPrefix
    uniqueSuffix: uniqueSuffix
    tags: tags
    skuName: config.serviceBusSku
  }
}

// Container Apps Environment
module containerEnvironment 'modules/container-environment.bicep' = {
  scope: rg
  name: 'container-environment-deployment'
  params: {
    location: location
    namingPrefix: namingPrefix
    tags: tags
    subnetId: networking.outputs.containerSubnetId
    logAnalyticsWorkspaceId: monitoring.outputs.logAnalyticsWorkspaceId
  }
}

// Container Apps
module containerApps 'modules/container-apps.bicep' = {
  scope: rg
  name: 'container-apps-deployment'
  params: {
    location: location
    namingPrefix: namingPrefix
    tags: tags
    containerEnvironmentId: containerEnvironment.outputs.environmentId
    containerCpu: config.containerCpu
    containerMemory: config.containerMemory
    minReplicas: config.containerMinReplicas
    maxReplicas: config.containerMaxReplicas
    enableAutoScaling: enableAutoScaling
    keyVaultName: security.outputs.keyVaultName
    managedIdentityId: security.outputs.managedIdentityId
    databaseHost: database.outputs.serverFQDN
    cacheHost: cache.outputs.hostName
    serviceBusNamespace: messaging.outputs.namespaceName
    storageAccountName: storage.outputs.storageAccountName
    environment: environment
  }
}

// API Management (only for demo/prod)
module apiManagement 'modules/api-management.bicep' = if (environment == 'demo' || environment == 'prod') {
  scope: rg
  name: 'api-management-deployment'
  params: {
    location: location
    namingPrefix: namingPrefix
    uniqueSuffix: uniqueSuffix
    tags: tags
    publisherEmail: adminEmail
    publisherName: 'NumbatWallet Team'
    sku: environment == 'prod' ? 'Standard' : 'Developer'
    subnetId: networking.outputs.apimSubnetId
  }
}

// Monitoring Module
module monitoring 'modules/monitoring.bicep' = {
  scope: rg
  name: 'monitoring-deployment'
  params: {
    location: location
    namingPrefix: namingPrefix
    tags: tags
    actionGroupEmail: adminEmail
  }
}

// Front Door (only for demo/prod)
module frontDoor 'modules/front-door.bicep' = if (environment == 'demo' || environment == 'prod') {
  scope: rg
  name: 'front-door-deployment'
  params: {
    namingPrefix: namingPrefix
    uniqueSuffix: uniqueSuffix
    tags: tags
    backendAddress: containerApps.outputs.apiUrl
    enableWAF: environment == 'prod'
  }
}

// ==================== OUTPUTS ====================
output resourceGroupName string = rg.name
output apiUrl string = containerApps.outputs.apiUrl
output apiManagementUrl string = (environment == 'demo' || environment == 'prod') ? apiManagement.outputs.gatewayUrl : 'N/A'
output frontDoorUrl string = (environment == 'demo' || environment == 'prod') ? frontDoor.outputs.frontDoorUrl : 'N/A'
output keyVaultName string = security.outputs.keyVaultName
output databaseServer string = database.outputs.serverFQDN
output environment string = environment
output deploymentTime string = utcNow('yyyy-MM-dd HH:mm:ss')

// ==================== DEPLOYMENT SCRIPT ====================
/*
Deployment Instructions:

1. Login to Azure:
   az login
   az account set --subscription "YOUR_SUBSCRIPTION_ID"

2. Deploy DEV environment (minimal resources):
   az deployment sub create \
     --location australiaeast \
     --template-file main.bicep \
     --parameters environment=dev adminEmail=admin@numbat.com

3. Deploy TEST environment:
   az deployment sub create \
     --location australiaeast \
     --template-file main.bicep \
     --parameters environment=test adminEmail=admin@numbat.com

4. Deploy DEMO environment (for POA demonstration):
   az deployment sub create \
     --location australiaeast \
     --template-file main.bicep \
     --parameters environment=demo adminEmail=admin@numbat.com

5. Deploy PROD environment (if POA successful):
   az deployment sub create \
     --location australiaeast \
     --template-file main.bicep \
     --parameters environment=prod adminEmail=admin@numbat.com

6. Clean up environment:
   az group delete --name rg-numbat-poa-dev --yes --no-wait

Cost Estimates (Monthly):
- DEV: ~$150 AUD (minimal resources)
- TEST: ~$300 AUD
- DEMO: ~$800 AUD (POA showcase)
- PROD: ~$1,500 AUD (production-ready)
*/
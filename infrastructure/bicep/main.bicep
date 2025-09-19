// Main Bicep Orchestration File for NumbatWallet Infrastructure
// This file coordinates deployment of all Azure resources

targetScope = 'subscription'

// ========================================
// Parameters
// ========================================

@description('Environment name (dev, test, prod)')
@allowed([
  'dev'
  'test'
  'prod'
])
param environment string

@description('Azure region for resources')
param location string = 'australiaeast'

@description('Azure region code for naming')
param locationCode string = 'aue'

@description('Application name')
param applicationName string = 'numbatwallet'

@description('Resource group name')
param resourceGroupName string = 'rg-${applicationName}-${environment}-${locationCode}'

@description('Existing Virtual Network resource group name (if using existing VNet)')
param existingVnetResourceGroup string = ''

@description('Existing Virtual Network name (if using existing VNet)')
param existingVnetName string = ''

@description('Existing subnet ID for private endpoints')
param privateEndpointSubnetId string = ''

@description('Administrator object ID for Key Vault access')
param administratorObjectId string

@description('Managed Identity object ID for application')
param managedIdentityObjectId string = ''

@description('Log Analytics workspace name')
param logAnalyticsWorkspaceName string = 'law-${applicationName}-${environment}-${locationCode}'

@description('PostgreSQL administrator login')
@secure()
param postgresAdminUsername string

@description('PostgreSQL administrator password')
@secure()
param postgresAdminPassword string

@description('Enable private endpoints for all services')
param enablePrivateEndpoints bool = true

@description('Allowed IP addresses for public access (if not using private endpoints)')
param allowedIpAddresses array = []

@description('JWT signing key for API authentication')
@secure()
param jwtSigningKey string = newGuid()

@description('API container image tag')
param apiImageTag string = 'latest'

@description('Admin container image tag')
param adminImageTag string = 'latest'

@description('Tags to apply to all resources')
param tags object = {
  Application: 'NumbatWallet'
  Environment: environment
  ManagedBy: 'Bicep'
  DeploymentDate: utcNow('yyyy-MM-dd')
  CostCenter: 'Digital-Services'
  Owner: 'WA-Government'
}

// ========================================
// Variables
// ========================================

var namingPrefix = '${applicationName}-${environment}-${locationCode}'
var keyVaultName = take('kv-${namingPrefix}', 24)
var storageAccountName = replace('st${namingPrefix}', '-', '')
var appInsightsName = 'ai-${namingPrefix}'
var redisCacheName = 'redis-${namingPrefix}'
var containerAppsEnvName = 'cae-${namingPrefix}'
var containerRegistryName = replace('cr${namingPrefix}', '-', '')
var postgresServerName = 'psql-${namingPrefix}'
var vnetName = 'vnet-${namingPrefix}'

// ========================================
// Resource Group
// ========================================

resource resourceGroup 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

// ========================================
// Modules
// ========================================

// Deploy Virtual Network
module virtualNetworkModule 'modules/virtual-network.bicep' = if (empty(existingVnetName)) {
  name: 'deploy-vnet-${environment}'
  scope: resourceGroup
  params: {
    vnetName: vnetName
    location: location
    environment: environment
    enableDdosProtection: environment == 'prod'
    tags: tags
  }
}

// Deploy Key Vault
module keyVaultModule 'modules/key-vault.bicep' = {
  name: 'deploy-keyvault-${environment}'
  scope: resourceGroup
  params: {
    keyVaultName: keyVaultName
    location: location
    sku: environment == 'prod' ? 'premium' : 'standard'
    enableSoftDelete: true
    softDeleteRetentionDays: environment == 'prod' ? 90 : 30
    enablePurgeProtection: environment == 'prod'
    enableRbacAuthorization: true
    enablePublicNetworkAccess: !enablePrivateEndpoints
    allowedIpAddresses: allowedIpAddresses
    managedIdentityObjectId: managedIdentityObjectId
    enablePrivateEndpoint: enablePrivateEndpoints
    privateEndpointSubnetId: !empty(existingVnetName) ? privateEndpointSubnetId : virtualNetworkModule.outputs.privateEndpointSubnetId
    logAnalyticsWorkspaceId: logAnalyticsModule.outputs.workspaceId
    tags: tags
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: administratorObjectId
        permissions: {
          secrets: ['all']
          certificates: ['all']
          keys: ['all']
        }
      }
    ]
  }
}

// Deploy Log Analytics Workspace
module logAnalyticsModule 'modules/log-analytics.bicep' = {
  name: 'deploy-loganalytics-${environment}'
  scope: resourceGroup
  params: {
    workspaceName: logAnalyticsWorkspaceName
    location: location
    retentionInDays: environment == 'prod' ? 90 : 30
    tags: tags
  }
}

// Deploy Application Insights
module appInsightsModule 'modules/application-insights.bicep' = {
  name: 'deploy-appinsights-${environment}'
  scope: resourceGroup
  params: {
    appInsightsName: appInsightsName
    location: location
    logAnalyticsWorkspaceId: logAnalyticsModule.outputs.workspaceId
    tags: tags
  }
  dependsOn: [
    logAnalyticsModule
  ]
}

// Deploy Storage Account
module storageAccountModule 'modules/storage-account.bicep' = {
  name: 'deploy-storage-${environment}'
  scope: resourceGroup
  params: {
    storageAccountName: storageAccountName
    location: location
    sku: environment == 'prod' ? 'Standard_ZRS' : 'Standard_LRS'
    enablePrivateEndpoints: enablePrivateEndpoints
    privateEndpointSubnetId: !empty(existingVnetName) ? privateEndpointSubnetId : virtualNetworkModule.outputs.privateEndpointSubnetId
    allowedIpAddresses: allowedIpAddresses
    tags: tags
  }
}

// Deploy Redis Cache
module redisCacheModule 'modules/redis-cache.bicep' = {
  name: 'deploy-redis-${environment}'
  scope: resourceGroup
  params: {
    redisCacheName: redisCacheName
    location: location
    skuName: environment == 'prod' ? 'Premium' : 'Standard'
    skuFamily: environment == 'prod' ? 'P' : 'C'
    skuCapacity: environment == 'prod' ? 1 : 0
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    enablePrivateEndpoint: enablePrivateEndpoints
    privateEndpointSubnetId: !empty(existingVnetName) ? privateEndpointSubnetId : virtualNetworkModule.outputs.privateEndpointSubnetId
    tags: tags
  }
}

// Deploy PostgreSQL Flexible Server
module postgresModule 'modules/postgresql.bicep' = {
  name: 'deploy-postgres-${environment}'
  scope: resourceGroup
  params: {
    serverName: postgresServerName
    location: location
    administratorLogin: postgresAdminUsername
    administratorLoginPassword: postgresAdminPassword
    skuName: environment == 'prod' ? 'Standard_D4ds_v4' : 'Standard_B2ms'
    storageSizeGB: environment == 'prod' ? 256 : 32
    backupRetentionDays: environment == 'prod' ? 35 : 7
    geoRedundantBackup: environment == 'prod' ? 'Enabled' : 'Disabled'
    highAvailability: environment == 'prod' ? 'ZoneRedundant' : 'Disabled'
    enablePrivateEndpoint: enablePrivateEndpoints
    privateEndpointSubnetId: !empty(existingVnetName) ? privateEndpointSubnetId : virtualNetworkModule.outputs.privateEndpointSubnetId
    tags: tags
  }
}

// Deploy Container Registry
module containerRegistryModule 'modules/container-registry.bicep' = {
  name: 'deploy-acr-${environment}'
  scope: resourceGroup
  params: {
    registryName: containerRegistryName
    location: location
    sku: environment == 'prod' ? 'Premium' : 'Basic'
    enableAdminUser: false
    enablePrivateEndpoint: enablePrivateEndpoints
    privateEndpointSubnetId: !empty(existingVnetName) ? privateEndpointSubnetId : virtualNetworkModule.outputs.privateEndpointSubnetId
    tags: tags
  }
}

// Deploy Container Apps Environment
module containerAppsEnvModule 'modules/container-apps-env.bicep' = {
  name: 'deploy-containerappenv-${environment}'
  scope: resourceGroup
  params: {
    environmentName: containerAppsEnvName
    location: location
    logAnalyticsWorkspaceId: logAnalyticsModule.outputs.workspaceId
    enableZoneRedundancy: environment == 'prod'
    tags: tags
  }
  dependsOn: [
    logAnalyticsModule
  ]
}

// Deploy Container Apps (API and Admin)
module containerAppsModule 'modules/container-apps.bicep' = {
  name: 'deploy-containerapps-${environment}'
  scope: resourceGroup
  params: {
    containerAppsEnvId: containerAppsEnvModule.outputs.environmentId
    location: location
    containerRegistryServer: containerRegistryModule.outputs.loginServer
    appInsightsConnectionString: appInsightsModule.outputs.connectionString
    keyVaultUri: keyVaultModule.outputs.keyVaultUri
    postgresConnectionString: 'Host=${postgresModule.outputs.fqdn};Database=numbatwallet;Username=${postgresAdminUsername};Password=${postgresAdminPassword};SSL Mode=Require'
    redisConnectionString: '${redisCacheModule.outputs.hostName},password=${redisCacheModule.outputs.primaryKey},ssl=True,abortConnect=False'
    jwtSigningKey: jwtSigningKey
    environment: environment
    apiImageTag: apiImageTag
    adminImageTag: adminImageTag
    tags: tags
  }
  dependsOn: [
    containerAppsEnvModule
    containerRegistryModule
    postgresModule
    redisCacheModule
    keyVaultModule
    appInsightsModule
  ]
}

// ========================================
// Outputs
// ========================================

output resourceGroupName string = resourceGroup.name
output vnetId string = empty(existingVnetName) ? virtualNetworkModule.outputs.vnetId : ''
output vnetName string = empty(existingVnetName) ? virtualNetworkModule.outputs.vnetName : existingVnetName
output keyVaultId string = keyVaultModule.outputs.keyVaultId
output keyVaultUri string = keyVaultModule.outputs.keyVaultUri
output appInsightsInstrumentationKey string = appInsightsModule.outputs.instrumentationKey
output appInsightsConnectionString string = appInsightsModule.outputs.connectionString
output storageAccountId string = storageAccountModule.outputs.storageAccountId
output storageAccountPrimaryEndpoint string = storageAccountModule.outputs.primaryBlobEndpoint
output redisCacheHostName string = redisCacheModule.outputs.hostName
output redisCacheConnectionString string = redisCacheModule.outputs.connectionString
output postgresServerFqdn string = postgresModule.outputs.fqdn
output containerRegistryLoginServer string = containerRegistryModule.outputs.loginServer
output containerAppsEnvironmentId string = containerAppsEnvModule.outputs.environmentId
output logAnalyticsWorkspaceId string = logAnalyticsModule.outputs.workspaceId
output apiAppUrl string = containerAppsModule.outputs.apiAppUrl
output adminAppUrl string = containerAppsModule.outputs.adminAppUrl
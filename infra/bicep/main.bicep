// NumbatWallet Infrastructure - Main Bicep Template
// Purpose: Deploy complete Azure infrastructure for NumbatWallet POA
// Australian regions only for data sovereignty

targetScope = 'subscription'

// Parameters
@description('Environment name (dev, staging, prod)')
@allowed(['dev', 'staging', 'prod'])
param environment string = 'dev'

@description('Azure region for deployment')
@allowed(['australiaeast', 'australiasoutheast'])
param location string = 'australiaeast'

@description('Base name for all resources')
@minLength(3)
@maxLength(24)
param baseName string = 'numbatwallet'

@description('Administrator email for alerts')
param adminEmail string

@description('Tags to apply to all resources')
param tags object = {
  Environment: environment
  Project: 'NumbatWallet'
  Purpose: 'Digital Identity Wallet'
  Compliance: 'TDIF'
  DataClassification: 'Sensitive'
  ManagedBy: 'Terraform'
}

// Variables
var resourceGroupName = 'rg-${baseName}-${environment}-${location}'
var uniqueSuffix = uniqueString(subscription().id, resourceGroupName)
var namingPrefix = '${baseName}${environment}'

// Resource Group
resource resourceGroup 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

// Modules
module networking './modules/networking.bicep' = {
  name: 'networking-deployment'
  scope: resourceGroup
  params: {
    location: location
    namingPrefix: namingPrefix
    environment: environment
    tags: tags
  }
}

module identity './modules/identity.bicep' = {
  name: 'identity-deployment'
  scope: resourceGroup
  params: {
    location: location
    namingPrefix: namingPrefix
    environment: environment
    tags: tags
  }
}

module storage './modules/storage.bicep' = {
  name: 'storage-deployment'
  scope: resourceGroup
  params: {
    location: location
    namingPrefix: namingPrefix
    uniqueSuffix: uniqueSuffix
    environment: environment
    virtualNetworkId: networking.outputs.vnetId
    privateEndpointSubnetId: networking.outputs.privateEndpointSubnetId
    tags: tags
  }
}

module database './modules/database.bicep' = {
  name: 'database-deployment'
  scope: resourceGroup
  params: {
    location: location
    namingPrefix: namingPrefix
    environment: environment
    virtualNetworkId: networking.outputs.vnetId
    databaseSubnetId: networking.outputs.databaseSubnetId
    administratorLogin: 'numbatwallet_admin'
    tags: tags
  }
}

module keyVault './modules/keyvault.bicep' = {
  name: 'keyvault-deployment'
  scope: resourceGroup
  params: {
    location: location
    namingPrefix: namingPrefix
    uniqueSuffix: uniqueSuffix
    environment: environment
    virtualNetworkId: networking.outputs.vnetId
    privateEndpointSubnetId: networking.outputs.privateEndpointSubnetId
    managedIdentityPrincipalId: identity.outputs.principalId
    tags: tags
  }
}

module containerRegistry './modules/acr.bicep' = {
  name: 'acr-deployment'
  scope: resourceGroup
  params: {
    location: location
    namingPrefix: namingPrefix
    uniqueSuffix: uniqueSuffix
    environment: environment
    virtualNetworkId: networking.outputs.vnetId
    privateEndpointSubnetId: networking.outputs.privateEndpointSubnetId
    tags: tags
  }
}

module appService './modules/appservice.bicep' = {
  name: 'appservice-deployment'
  scope: resourceGroup
  params: {
    location: location
    namingPrefix: namingPrefix
    environment: environment
    virtualNetworkId: networking.outputs.vnetId
    appSubnetId: networking.outputs.appSubnetId
    managedIdentityId: identity.outputs.managedIdentityId
    tags: tags
  }
}

module applicationGateway './modules/appgateway.bicep' = {
  name: 'appgateway-deployment'
  scope: resourceGroup
  params: {
    location: location
    namingPrefix: namingPrefix
    environment: environment
    virtualNetworkId: networking.outputs.vnetId
    gatewaySubnetId: networking.outputs.gatewaySubnetId
    backendPoolFqdns: [appService.outputs.defaultHostName]
    tags: tags
  }
}

module monitoring './modules/monitoring.bicep' = {
  name: 'monitoring-deployment'
  scope: resourceGroup
  params: {
    location: location
    namingPrefix: namingPrefix
    environment: environment
    adminEmail: adminEmail
    tags: tags
  }
}

module redis './modules/redis.bicep' = {
  name: 'redis-deployment'
  scope: resourceGroup
  params: {
    location: location
    namingPrefix: namingPrefix
    environment: environment
    virtualNetworkId: networking.outputs.vnetId
    cacheSubnetId: networking.outputs.cacheSubnetId
    tags: tags
  }
}

// Outputs
output resourceGroupName string = resourceGroup.name
output vnetId string = networking.outputs.vnetId
output keyVaultUri string = keyVault.outputs.keyVaultUri
output storageAccountName string = storage.outputs.storageAccountName
output databaseServerName string = database.outputs.serverName
output containerRegistryLoginServer string = containerRegistry.outputs.loginServer
output appServiceUrl string = 'https://${appService.outputs.defaultHostName}'
output applicationGatewayFqdn string = applicationGateway.outputs.fqdn
output logAnalyticsWorkspaceId string = monitoring.outputs.workspaceId
output redisCacheHostName string = redis.outputs.hostName
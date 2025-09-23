// Container Registry Module
@description('Azure region for resources')
param location string
@description('Naming prefix for resources')
param namingPrefix string
@description('Unique suffix for global resources')
param uniqueSuffix string
@description('Environment name')
param environment string
@description('Virtual network ID')
param virtualNetworkId string
@description('Subnet ID for private endpoint')
param privateEndpointSubnetId string
@description('Tags to apply to resources')
param tags object

var acrName = 'acr${take(replace(namingPrefix, '-', ''), 14)}${take(uniqueSuffix, 6)}'

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' = {
  name: acrName
  location: location
  tags: tags
  sku: {
    name: environment == 'prod' ? 'Premium' : 'Basic'
  }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Disabled'
  }
}

output loginServer string = containerRegistry.properties.loginServer
output registryId string = containerRegistry.id

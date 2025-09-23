// Redis Cache Module
@description('Azure region')
param location string
@description('Naming prefix')
param namingPrefix string
@description('Environment')
param environment string
@description('VNet ID')
param virtualNetworkId string
@description('Cache subnet ID')
param cacheSubnetId string
@description('Tags')
param tags object

resource redisCache 'Microsoft.Cache/redis@2023-08-01' = {
  name: 'redis-${namingPrefix}'
  location: location
  tags: tags
  properties: {
    sku: {
      name: environment == 'prod' ? 'Premium' : 'Basic'
      family: environment == 'prod' ? 'P' : 'C'
      capacity: environment == 'prod' ? 1 : 0
    }
    minimumTlsVersion: '1.2'
  }
}

output hostName string = redisCache.properties.hostName

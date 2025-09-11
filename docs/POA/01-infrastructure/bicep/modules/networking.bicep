// Networking Module - Virtual Network and Subnets
// Version: 1.0
// Description: Creates VNet with subnets for different services

@description('Azure region for resources')
param location string

@description('Naming prefix for resources')
param namingPrefix string

@description('Environment name')
param environment string

@description('Resource tags')
param tags object

// ==================== VARIABLES ====================
var vnetName = 'vnet-${namingPrefix}'
var vnetAddressSpace = environment == 'prod' ? '10.0.0.0/16' : '10.1.0.0/16'

// Subnet configurations based on environment
var subnetConfig = {
  dev: {
    container: '10.1.1.0/26'      // 64 IPs
    database: '10.1.1.64/28'       // 16 IPs
    cache: '10.1.1.80/28'          // 16 IPs
    storage: '10.1.1.96/28'        // 16 IPs
    apim: '10.1.1.112/28'          // 16 IPs
  }
  test: {
    container: '10.1.1.0/25'       // 128 IPs
    database: '10.1.1.128/27'      // 32 IPs
    cache: '10.1.1.160/27'         // 32 IPs
    storage: '10.1.1.192/27'       // 32 IPs
    apim: '10.1.1.224/27'          // 32 IPs
  }
  demo: {
    container: '10.1.1.0/24'       // 256 IPs
    database: '10.1.2.0/26'        // 64 IPs
    cache: '10.1.2.64/26'          // 64 IPs
    storage: '10.1.2.128/26'       // 64 IPs
    apim: '10.1.2.192/26'          // 64 IPs
  }
  prod: {
    container: '10.0.1.0/24'       // 256 IPs
    database: '10.0.2.0/24'        // 256 IPs
    cache: '10.0.3.0/24'           // 256 IPs
    storage: '10.0.4.0/24'         // 256 IPs
    apim: '10.0.5.0/24'            // 256 IPs
  }
}

var subnets = subnetConfig[environment]

// ==================== NETWORK SECURITY GROUPS ====================
// Container Apps NSG
resource containerNsg 'Microsoft.Network/networkSecurityGroups@2023-05-01' = {
  name: 'nsg-${namingPrefix}-container'
  location: location
  tags: tags
  properties: {
    securityRules: [
      {
        name: 'AllowHTTPS'
        properties: {
          priority: 100
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '443'
          sourceAddressPrefix: 'Internet'
          destinationAddressPrefix: '*'
        }
      }
      {
        name: 'AllowContainerAppsMgmt'
        properties: {
          priority: 110
          direction: 'Inbound'
          access: 'Allow'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: 'AzureLoadBalancer'
          destinationAddressPrefix: '*'
        }
      }
    ]
  }
}

// Database NSG
resource databaseNsg 'Microsoft.Network/networkSecurityGroups@2023-05-01' = {
  name: 'nsg-${namingPrefix}-database'
  location: location
  tags: tags
  properties: {
    securityRules: [
      {
        name: 'AllowPostgreSQL'
        properties: {
          priority: 100
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '5432'
          sourceAddressPrefix: subnets.container
          destinationAddressPrefix: '*'
        }
      }
      {
        name: 'DenyAllInbound'
        properties: {
          priority: 200
          direction: 'Inbound'
          access: 'Deny'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
        }
      }
    ]
  }
}

// Cache NSG
resource cacheNsg 'Microsoft.Network/networkSecurityGroups@2023-05-01' = {
  name: 'nsg-${namingPrefix}-cache'
  location: location
  tags: tags
  properties: {
    securityRules: [
      {
        name: 'AllowRedis'
        properties: {
          priority: 100
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '6379-6380'
          sourceAddressPrefix: subnets.container
          destinationAddressPrefix: '*'
        }
      }
    ]
  }
}

// Storage NSG
resource storageNsg 'Microsoft.Network/networkSecurityGroups@2023-05-01' = {
  name: 'nsg-${namingPrefix}-storage'
  location: location
  tags: tags
  properties: {
    securityRules: [
      {
        name: 'AllowStorageFromContainer'
        properties: {
          priority: 100
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '443'
          sourceAddressPrefix: subnets.container
          destinationAddressPrefix: '*'
        }
      }
    ]
  }
}

// API Management NSG
resource apimNsg 'Microsoft.Network/networkSecurityGroups@2023-05-01' = {
  name: 'nsg-${namingPrefix}-apim'
  location: location
  tags: tags
  properties: {
    securityRules: [
      {
        name: 'AllowAPIMManagement'
        properties: {
          priority: 100
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '3443'
          sourceAddressPrefix: 'ApiManagement'
          destinationAddressPrefix: 'VirtualNetwork'
        }
      }
      {
        name: 'AllowHTTPS'
        properties: {
          priority: 110
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '443'
          sourceAddressPrefix: 'Internet'
          destinationAddressPrefix: 'VirtualNetwork'
        }
      }
    ]
  }
}

// ==================== VIRTUAL NETWORK ====================
resource vnet 'Microsoft.Network/virtualNetworks@2023-05-01' = {
  name: vnetName
  location: location
  tags: tags
  properties: {
    addressSpace: {
      addressPrefixes: [
        vnetAddressSpace
      ]
    }
    subnets: [
      {
        name: 'snet-container-apps'
        properties: {
          addressPrefix: subnets.container
          networkSecurityGroup: {
            id: containerNsg.id
          }
          delegations: []
          serviceEndpoints: [
            {
              service: 'Microsoft.KeyVault'
            }
            {
              service: 'Microsoft.Storage'
            }
          ]
        }
      }
      {
        name: 'snet-database'
        properties: {
          addressPrefix: subnets.database
          networkSecurityGroup: {
            id: databaseNsg.id
          }
          delegations: [
            {
              name: 'Microsoft.DBforPostgreSQL.flexibleServers'
              properties: {
                serviceName: 'Microsoft.DBforPostgreSQL/flexibleServers'
              }
            }
          ]
          serviceEndpoints: []
        }
      }
      {
        name: 'snet-cache'
        properties: {
          addressPrefix: subnets.cache
          networkSecurityGroup: {
            id: cacheNsg.id
          }
          privateEndpointNetworkPolicies: 'Disabled'
          privateLinkServiceNetworkPolicies: 'Enabled'
        }
      }
      {
        name: 'snet-storage'
        properties: {
          addressPrefix: subnets.storage
          networkSecurityGroup: {
            id: storageNsg.id
          }
          privateEndpointNetworkPolicies: 'Disabled'
          privateLinkServiceNetworkPolicies: 'Enabled'
        }
      }
      {
        name: 'snet-apim'
        properties: {
          addressPrefix: subnets.apim
          networkSecurityGroup: {
            id: apimNsg.id
          }
          delegations: []
        }
      }
    ]
  }
}

// ==================== OUTPUTS ====================
output vnetId string = vnet.id
output vnetName string = vnet.name
output containerSubnetId string = vnet.properties.subnets[0].id
output databaseSubnetId string = vnet.properties.subnets[1].id
output cacheSubnetId string = vnet.properties.subnets[2].id
output storageSubnetId string = vnet.properties.subnets[3].id
output apimSubnetId string = vnet.properties.subnets[4].id
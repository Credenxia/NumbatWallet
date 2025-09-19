// Virtual Network Module for NumbatWallet Infrastructure
// Provides network isolation, security boundaries, and private connectivity

@description('Virtual Network name')
param vnetName string

@description('Location for the virtual network')
param location string

@description('Address space for the virtual network')
param addressPrefix string = '10.0.0.0/16'

@description('Environment name')
@allowed([
  'dev'
  'test'
  'prod'
])
param environment string

@description('Enable DDoS protection')
param enableDdosProtection bool = false

@description('Enable VM protection')
param enableVmProtection bool = true

@description('DNS servers for the VNet (optional)')
param dnsServers array = []

@description('Tags to apply to resources')
param tags object = {}

// ========================================
// Variables
// ========================================

var subnets = [
  {
    name: 'snet-gateway'
    addressPrefix: '10.0.1.0/24'
    serviceEndpoints: []
    delegations: []
    privateEndpointNetworkPolicies: 'Disabled'
    privateLinkServiceNetworkPolicies: 'Disabled'
  }
  {
    name: 'snet-privateendpoints'
    addressPrefix: '10.0.2.0/24'
    serviceEndpoints: []
    delegations: []
    privateEndpointNetworkPolicies: 'Disabled'
    privateLinkServiceNetworkPolicies: 'Disabled'
  }
  {
    name: 'snet-containerapps'
    addressPrefix: '10.0.3.0/23'  // Larger subnet for Container Apps
    serviceEndpoints: [
      {
        service: 'Microsoft.Storage'
        locations: [location]
      }
      {
        service: 'Microsoft.KeyVault'
        locations: [location]
      }
      {
        service: 'Microsoft.Sql'
        locations: [location]
      }
    ]
    delegations: []
    privateEndpointNetworkPolicies: 'Enabled'
    privateLinkServiceNetworkPolicies: 'Enabled'
  }
  {
    name: 'snet-data'
    addressPrefix: '10.0.5.0/24'
    serviceEndpoints: [
      {
        service: 'Microsoft.Storage'
        locations: [location]
      }
      {
        service: 'Microsoft.Sql'
        locations: [location]
      }
    ]
    delegations: []
    privateEndpointNetworkPolicies: 'Disabled'
    privateLinkServiceNetworkPolicies: 'Disabled'
  }
  {
    name: 'snet-management'
    addressPrefix: '10.0.10.0/24'
    serviceEndpoints: [
      {
        service: 'Microsoft.KeyVault'
        locations: [location]
      }
      {
        service: 'Microsoft.Storage'
        locations: [location]
      }
    ]
    delegations: []
    privateEndpointNetworkPolicies: 'Enabled'
    privateLinkServiceNetworkPolicies: 'Enabled'
  }
]

// ========================================
// Resources
// ========================================

// Virtual Network
resource virtualNetwork 'Microsoft.Network/virtualNetworks@2023-09-01' = {
  name: vnetName
  location: location
  tags: tags
  properties: {
    addressSpace: {
      addressPrefixes: [
        addressPrefix
      ]
    }
    dhcpOptions: empty(dnsServers) ? {} : {
      dnsServers: dnsServers
    }
    enableDdosProtection: enableDdosProtection
    enableVmProtection: enableVmProtection
    subnets: [for subnet in subnets: {
      name: subnet.name
      properties: {
        addressPrefix: subnet.addressPrefix
        networkSecurityGroup: networkSecurityGroups[subnet.name].id == null ? null : {
          id: networkSecurityGroups[subnet.name].id
        }
        serviceEndpoints: subnet.serviceEndpoints
        delegations: subnet.delegations
        privateEndpointNetworkPolicies: subnet.privateEndpointNetworkPolicies
        privateLinkServiceNetworkPolicies: subnet.privateLinkServiceNetworkPolicies
      }
    }]
  }
}

// Network Security Groups
resource nsgGateway 'Microsoft.Network/networkSecurityGroups@2023-09-01' = {
  name: 'nsg-gateway-${environment}'
  location: location
  tags: tags
  properties: {
    securityRules: [
      {
        name: 'AllowHttpsInbound'
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
        name: 'AllowHealthProbes'
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
      {
        name: 'DenyAllInbound'
        properties: {
          priority: 4096
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

resource nsgPrivateEndpoints 'Microsoft.Network/networkSecurityGroups@2023-09-01' = {
  name: 'nsg-privateendpoints-${environment}'
  location: location
  tags: tags
  properties: {
    securityRules: [
      {
        name: 'AllowVnetInbound'
        properties: {
          priority: 100
          direction: 'Inbound'
          access: 'Allow'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: 'VirtualNetwork'
          destinationAddressPrefix: 'VirtualNetwork'
        }
      }
      {
        name: 'DenyAllInbound'
        properties: {
          priority: 4096
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

resource nsgContainerApps 'Microsoft.Network/networkSecurityGroups@2023-09-01' = {
  name: 'nsg-containerapps-${environment}'
  location: location
  tags: tags
  properties: {
    securityRules: [
      {
        name: 'AllowContainerAppsControl'
        properties: {
          priority: 100
          direction: 'Inbound'
          access: 'Allow'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRanges: ['443', '8443']
          sourceAddressPrefix: 'AzureContainerApps'
          destinationAddressPrefix: '*'
        }
      }
      {
        name: 'AllowVnetInbound'
        properties: {
          priority: 110
          direction: 'Inbound'
          access: 'Allow'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: 'VirtualNetwork'
          destinationAddressPrefix: 'VirtualNetwork'
        }
      }
      {
        name: 'AllowAzureLoadBalancer'
        properties: {
          priority: 120
          direction: 'Inbound'
          access: 'Allow'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: 'AzureLoadBalancer'
          destinationAddressPrefix: '*'
        }
      }
      {
        name: 'DenyAllInbound'
        properties: {
          priority: 4096
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

resource nsgData 'Microsoft.Network/networkSecurityGroups@2023-09-01' = {
  name: 'nsg-data-${environment}'
  location: location
  tags: tags
  properties: {
    securityRules: [
      {
        name: 'AllowFromContainerApps'
        properties: {
          priority: 100
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRanges: ['5432', '6379', '1433']
          sourceAddressPrefix: '10.0.3.0/23'
          destinationAddressPrefix: '*'
        }
      }
      {
        name: 'AllowFromManagement'
        properties: {
          priority: 110
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRanges: ['5432', '6379', '1433']
          sourceAddressPrefix: '10.0.10.0/24'
          destinationAddressPrefix: '*'
        }
      }
      {
        name: 'DenyAllInbound'
        properties: {
          priority: 4096
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

resource nsgManagement 'Microsoft.Network/networkSecurityGroups@2023-09-01' = {
  name: 'nsg-management-${environment}'
  location: location
  tags: tags
  properties: {
    securityRules: [
      {
        name: 'AllowBastionInbound'
        properties: {
          priority: 100
          direction: 'Inbound'
          access: 'Allow'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRanges: ['3389', '22']
          sourceAddressPrefix: 'BastionHost'
          destinationAddressPrefix: '*'
        }
      }
      {
        name: 'AllowVnetInbound'
        properties: {
          priority: 110
          direction: 'Inbound'
          access: 'Allow'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: 'VirtualNetwork'
          destinationAddressPrefix: 'VirtualNetwork'
        }
      }
      {
        name: 'DenyAllInbound'
        properties: {
          priority: 4096
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

// Map NSGs to subnet names
var networkSecurityGroups = {
  'snet-gateway': nsgGateway
  'snet-privateendpoints': nsgPrivateEndpoints
  'snet-containerapps': nsgContainerApps
  'snet-data': nsgData
  'snet-management': nsgManagement
}

// Network Watcher Flow Logs (for production)
resource networkWatcherFlowLog 'Microsoft.Network/networkWatchers/flowLogs@2023-09-01' = if (environment == 'prod') {
  name: 'NetworkWatcher_${location}/fl-${vnetName}'
  location: location
  tags: tags
  properties: {
    targetResourceId: virtualNetwork.id
    storageId: storageAccountForLogs.id
    enabled: true
    retentionPolicy: {
      days: 30
      enabled: true
    }
    format: {
      type: 'JSON'
      version: 2
    }
    flowAnalyticsConfiguration: {
      networkWatcherFlowAnalyticsConfiguration: {
        enabled: true
        workspaceRegion: location
        workspaceResourceId: logAnalyticsWorkspace.id
        trafficAnalyticsInterval: 10
      }
    }
  }
}

// Storage account for flow logs (production only)
resource storageAccountForLogs 'Microsoft.Storage/storageAccounts@2023-01-01' = if (environment == 'prod') {
  name: 'stflowlogs${uniqueString(resourceGroup().id)}'
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    encryption: {
      services: {
        file: {
          keyType: 'Account'
          enabled: true
        }
        blob: {
          keyType: 'Account'
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
    networkAcls: {
      defaultAction: 'Deny'
      bypass: 'AzureServices'
      virtualNetworkRules: []
      ipRules: []
    }
  }
}

// Placeholder for Log Analytics workspace reference
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' existing = if (environment == 'prod') {
  name: 'law-numbatwallet-${environment}'
  scope: resourceGroup()
}

// ========================================
// Outputs
// ========================================

output vnetId string = virtualNetwork.id
output vnetName string = virtualNetwork.name
output addressSpace string = addressPrefix

// Subnet outputs
output gatewaySubnetId string = virtualNetwork.properties.subnets[0].id
output privateEndpointSubnetId string = virtualNetwork.properties.subnets[1].id
output containerAppsSubnetId string = virtualNetwork.properties.subnets[2].id
output dataSubnetId string = virtualNetwork.properties.subnets[3].id
output managementSubnetId string = virtualNetwork.properties.subnets[4].id

// NSG outputs
output nsgGatewayId string = nsgGateway.id
output nsgPrivateEndpointsId string = nsgPrivateEndpoints.id
output nsgContainerAppsId string = nsgContainerApps.id
output nsgDataId string = nsgData.id
output nsgManagementId string = nsgManagement.id
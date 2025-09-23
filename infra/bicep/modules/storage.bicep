// Storage Module - Blob Storage for documents and credentials
// Purpose: Secure storage for documents with encryption at rest

@description('Azure region for resources')
param location string

@description('Naming prefix for resources')
param namingPrefix string

@description('Unique suffix for global resources')
param uniqueSuffix string

@description('Environment name')
param environment string

@description('Virtual network ID for private endpoint')
param virtualNetworkId string

@description('Subnet ID for private endpoint')
param privateEndpointSubnetId string

@description('Tags to apply to resources')
param tags object

// Variables
var storageAccountName = 'st${take(replace(namingPrefix, '-', ''), 14)}${take(uniqueSuffix, 8)}'
var containerNames = ['documents', 'credentials', 'presentations', 'backups']

// Storage Account
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  tags: tags
  sku: {
    name: environment == 'prod' ? 'Standard_ZRS' : 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
    networkAcls: {
      defaultAction: 'Deny'
      bypass: 'AzureServices'
      ipRules: []
      virtualNetworkRules: []
    }
    encryption: {
      services: {
        blob: {
          enabled: true
        }
        file: {
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
    isHnsEnabled: false
    isSftpEnabled: false
  }
}

// Blob Service
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    cors: {
      corsRules: []
    }
    deleteRetentionPolicy: {
      enabled: true
      days: environment == 'prod' ? 30 : 7
    }
    containerDeleteRetentionPolicy: {
      enabled: true
      days: environment == 'prod' ? 30 : 7
    }
  }
}

// Containers
resource containers 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = [for containerName in containerNames: {
  parent: blobService
  name: containerName
  properties: {
    publicAccess: 'None'
    metadata: {
      environment: environment
      purpose: containerName
    }
  }
}]

// Private Endpoint for Blob Storage
resource storagePrivateEndpoint 'Microsoft.Network/privateEndpoints@2023-09-01' = {
  name: 'pe-${storageAccountName}'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: 'plsc-${storageAccountName}'
        properties: {
          privateLinkServiceId: storageAccount.id
          groupIds: ['blob']
        }
      }
    ]
  }
}

// Private DNS Zone for Blob Storage
resource storagePrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.blob.core.windows.net'
  location: 'global'
  tags: tags
}

// Link Private DNS Zone to VNet
resource storagePrivateDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: storagePrivateDnsZone
  name: 'vnet-link-${storageAccountName}'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: virtualNetworkId
    }
  }
}

// Private DNS Zone Group
resource storagePrivateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-09-01' = {
  parent: storagePrivateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'privatelink-blob-core-windows-net'
        properties: {
          privateDnsZoneId: storagePrivateDnsZone.id
        }
      }
    ]
  }
}

// Outputs
output storageAccountId string = storageAccount.id
output storageAccountName string = storageAccount.name
output primaryBlobEndpoint string = storageAccount.properties.primaryEndpoints.blob
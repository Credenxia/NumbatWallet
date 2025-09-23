// Key Vault Module - Secure Secret Management
// Purpose: Store and manage sensitive configuration data

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

@description('Principal ID of managed identity')
param managedIdentityPrincipalId string

@description('Tags to apply to resources')
param tags object

// Variables
var keyVaultName = 'kv-${take(namingPrefix, 14)}-${take(uniqueSuffix, 8)}'
var skuName = environment == 'prod' ? 'premium' : 'standard'

// Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: skuName
    }
    tenantId: subscription().tenantId
    enabledForDeployment: true
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: true
    enableSoftDelete: true
    softDeleteRetentionInDays: environment == 'prod' ? 90 : 7
    enableRbacAuthorization: false
    enablePurgeProtection: environment == 'prod' ? true : false
    networkAcls: {
      defaultAction: 'Deny'
      bypass: 'AzureServices'
      ipRules: []
      virtualNetworkRules: []
    }
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: managedIdentityPrincipalId
        permissions: {
          keys: [
            'get'
            'list'
            'create'
            'update'
            'import'
            'delete'
            'backup'
            'restore'
            'recover'
            'purge'
            'encrypt'
            'decrypt'
            'unwrapKey'
            'wrapKey'
            'verify'
            'sign'
          ]
          secrets: [
            'get'
            'list'
            'set'
            'delete'
            'backup'
            'restore'
            'recover'
            'purge'
          ]
          certificates: [
            'get'
            'list'
            'create'
            'update'
            'delete'
            'managecontacts'
            'getissuers'
            'listissuers'
            'setissuers'
            'deleteissuers'
            'manageissuers'
            'recover'
            'backup'
            'restore'
            'purge'
          ]
        }
      }
    ]
  }
}

// Private Endpoint for Key Vault
resource keyVaultPrivateEndpoint 'Microsoft.Network/privateEndpoints@2023-09-01' = {
  name: 'pe-${keyVaultName}'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: 'plsc-${keyVaultName}'
        properties: {
          privateLinkServiceId: keyVault.id
          groupIds: ['vault']
        }
      }
    ]
  }
}

// Private DNS Zone for Key Vault
resource keyVaultPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.vaultcore.azure.net'
  location: 'global'
  tags: tags
}

// Link Private DNS Zone to VNet
resource keyVaultPrivateDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: keyVaultPrivateDnsZone
  name: 'vnet-link-${keyVaultName}'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: virtualNetworkId
    }
  }
}

// Private DNS Zone Group
resource keyVaultPrivateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-09-01' = {
  parent: keyVaultPrivateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'privatelink-vaultcore-azure-net'
        properties: {
          privateDnsZoneId: keyVaultPrivateDnsZone.id
        }
      }
    ]
  }
}

// Diagnostic Settings
resource keyVaultDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (environment == 'prod') {
  name: 'diag-${keyVaultName}'
  scope: keyVault
  properties: {
    logs: [
      {
        category: 'AuditEvent'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 90
        }
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 90
        }
      }
    ]
  }
}

// Outputs
output keyVaultId string = keyVault.id
output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
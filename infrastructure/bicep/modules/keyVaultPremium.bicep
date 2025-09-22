// Azure Key Vault Premium Module with HSM Support for NumbatWallet
// Purpose: Hardware Security Module backed key operations for production environments
// Supports phased migration: Standard → Premium → Managed HSM → Dedicated HSM

@description('The name of the Key Vault')
@minLength(3)
@maxLength(24)
param keyVaultName string

@description('Location for the Key Vault')
param location string = resourceGroup().location

@description('Environment type for HSM provider selection')
@allowed([
  'Development'
  'Staging'
  'Production'
])
param environmentType string = 'Development'

@description('HSM Provider Type')
@allowed([
  'Software'      // Development only - file-based keys
  'KeyVault'      // Standard Key Vault (software-protected)
  'KeyVaultHSM'   // Key Vault Premium (HSM-protected)
  'ManagedHSM'    // Managed HSM (FIPS 140-2 Level 2)
  'DedicatedHSM'  // Dedicated HSM (future - FIPS 140-2 Level 3)
])
param hsmProvider string = environmentType == 'Development' ? 'Software' : environmentType == 'Staging' ? 'KeyVault' : 'KeyVaultHSM'

@description('Enable envelope encryption (KEK/DEK pattern)')
param enableEnvelopeEncryption bool = true

@description('Enable key rotation automation')
param enableKeyRotation bool = true

@description('Key rotation period in days')
@minValue(30)
@maxValue(365)
param keyRotationDays int = 90

@description('Enable multi-tenant key isolation')
param enableTenantIsolation bool = true

@description('Maximum number of tenants (for capacity planning)')
param maxTenants int = 100

@description('Enable soft delete protection')
param enableSoftDelete bool = true

@description('Soft delete retention in days')
@minValue(7)
@maxValue(90)
param softDeleteRetentionDays int = 90

@description('Enable purge protection')
param enablePurgeProtection bool = environmentType != 'Development'

@description('Enable RBAC authorization')
param enableRbacAuthorization bool = true

@description('Enable public network access')
param enablePublicNetworkAccess bool = environmentType == 'Development'

@description('Allowed IP addresses for firewall rules')
param allowedIpAddresses array = []

@description('Virtual network rules for service endpoints')
param virtualNetworkRules array = []

@description('Object ID of the managed identity for key vault access')
param managedIdentityObjectId string

@description('Additional access policies')
param accessPolicies array = []

@description('Enable private endpoint')
param enablePrivateEndpoint bool = environmentType != 'Development'

@description('Subnet ID for private endpoint')
param privateEndpointSubnetId string = ''

@description('Private DNS Zone ID for private endpoint')
param privateDnsZoneId string = ''

@description('Tags for the resource')
param tags object = {}

@description('Enable diagnostics')
param enableDiagnostics bool = true

@description('Log Analytics Workspace ID for diagnostics')
param logAnalyticsWorkspaceId string = ''

// Determine SKU based on HSM provider
var keyVaultSku = hsmProvider == 'KeyVaultHSM' || hsmProvider == 'ManagedHSM' ? 'premium' : 'standard'

// Resource: Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: union(tags, {
    Environment: environmentType
    HSMProvider: hsmProvider
    EnvelopeEncryption: string(enableEnvelopeEncryption)
    TenantIsolation: string(enableTenantIsolation)
    Purpose: 'NumbatWallet-HSM'
  })
  properties: {
    sku: {
      family: 'A'
      name: keyVaultSku
    }
    tenantId: tenant().tenantId
    enableSoftDelete: enableSoftDelete
    softDeleteRetentionInDays: softDeleteRetentionDays
    enablePurgeProtection: enablePurgeProtection
    enableRbacAuthorization: enableRbacAuthorization
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    publicNetworkAccess: enablePublicNetworkAccess ? 'Enabled' : 'Disabled'

    // Network ACLs
    networkAcls: {
      defaultAction: enablePublicNetworkAccess ? 'Allow' : 'Deny'
      bypass: 'AzureServices'
      ipRules: [for ip in allowedIpAddresses: {
        value: ip
      }]
      virtualNetworkRules: [for vnetRule in virtualNetworkRules: {
        id: vnetRule
        ignoreMissingVnetServiceEndpoint: false
      }]
    }

    // Access policies (if not using RBAC)
    accessPolicies: !enableRbacAuthorization ? concat([
      {
        tenantId: tenant().tenantId
        objectId: managedIdentityObjectId
        permissions: {
          keys: [
            'get'
            'list'
            'update'
            'create'
            'import'
            'delete'
            'recover'
            'backup'
            'restore'
            'decrypt'
            'encrypt'
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
            'recover'
            'backup'
            'restore'
          ]
          certificates: [
            'get'
            'list'
            'update'
            'create'
            'import'
            'delete'
            'recover'
            'backup'
            'restore'
            'managecontacts'
            'manageissuers'
            'getissuers'
            'listissuers'
            'setissuers'
            'deleteissuers'
          ]
        }
      }
    ], accessPolicies) : []
  }
}

// Resource: Key Encryption Keys (KEK) for Envelope Encryption
resource keyEncryptionKey 'Microsoft.KeyVault/vaults/keys@2023-07-01' = if (enableEnvelopeEncryption && hsmProvider != 'Software') {
  parent: keyVault
  name: 'numbatwallet-kek-master'
  properties: {
    kty: hsmProvider == 'KeyVaultHSM' || hsmProvider == 'ManagedHSM' ? 'RSA-HSM' : 'RSA'
    keySize: 4096
    keyOps: [
      'encrypt'
      'decrypt'
      'wrapKey'
      'unwrapKey'
    ]
    attributes: {
      enabled: true
      exportable: false
    }
    rotationPolicy: enableKeyRotation ? {
      attributes: {
        expiryTime: 'P${keyRotationDays}D'
      }
      lifetimeActions: [
        {
          action: {
            type: 'rotate'
          }
          trigger: {
            timeBeforeExpiry: 'P30D'
          }
        }
        {
          action: {
            type: 'notify'
          }
          trigger: {
            timeBeforeExpiry: 'P7D'
          }
        }
      ]
    } : null
  }
}

// Resource: Tenant-Specific KEKs (for multi-tenancy)
resource tenantKEKs 'Microsoft.KeyVault/vaults/keys@2023-07-01' = [for i in range(1, min(maxTenants, 10)): if (enableTenantIsolation && enableEnvelopeEncryption && hsmProvider != 'Software') {
  parent: keyVault
  name: 'tenant-kek-${padLeft(i, 3, '0')}'
  properties: {
    kty: hsmProvider == 'KeyVaultHSM' || hsmProvider == 'ManagedHSM' ? 'RSA-HSM' : 'RSA'
    keySize: 2048
    keyOps: [
      'encrypt'
      'decrypt'
      'wrapKey'
      'unwrapKey'
    ]
    attributes: {
      enabled: true
      exportable: false
    }
    rotationPolicy: enableKeyRotation ? {
      attributes: {
        expiryTime: 'P${keyRotationDays}D'
      }
      lifetimeActions: [
        {
          action: {
            type: 'rotate'
          }
          trigger: {
            timeBeforeExpiry: 'P30D'
          }
        }
      ]
    } : null
  }
}]

// Resource: Signing Keys for Document/Credential Signing
resource signingKey 'Microsoft.KeyVault/vaults/keys@2023-07-01' = if (hsmProvider != 'Software') {
  parent: keyVault
  name: 'numbatwallet-signing-key'
  properties: {
    kty: hsmProvider == 'KeyVaultHSM' || hsmProvider == 'ManagedHSM' ? 'EC-HSM' : 'EC'
    crv: 'P-256'
    keyOps: [
      'sign'
      'verify'
    ]
    attributes: {
      enabled: true
      exportable: false
    }
    rotationPolicy: enableKeyRotation ? {
      attributes: {
        expiryTime: 'P365D'
      }
      lifetimeActions: [
        {
          action: {
            type: 'rotate'
          }
          trigger: {
            timeBeforeExpiry: 'P30D'
          }
        }
        {
          action: {
            type: 'notify'
          }
          trigger: {
            timeBeforeExpiry: 'P14D'
          }
        }
      ]
    } : null
  }
}

// Resource: Private Endpoint
resource privateEndpoint 'Microsoft.Network/privateEndpoints@2023-09-01' = if (enablePrivateEndpoint && privateEndpointSubnetId != '') {
  name: '${keyVaultName}-pe'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${keyVaultName}-pe-connection'
        properties: {
          privateLinkServiceId: keyVault.id
          groupIds: [
            'vault'
          ]
        }
      }
    ]
  }
}

// Resource: Private DNS Zone Group
resource privateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-09-01' = if (enablePrivateEndpoint && privateDnsZoneId != '') {
  parent: privateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'privatelink-vaultcore-azure-net'
        properties: {
          privateDnsZoneId: privateDnsZoneId
        }
      }
    ]
  }
}

// Resource: Diagnostic Settings
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (enableDiagnostics && logAnalyticsWorkspaceId != '') {
  name: '${keyVaultName}-diagnostics'
  scope: keyVault
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        category: 'AuditEvent'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 90
        }
      }
      {
        category: 'AzurePolicyEvaluationDetails'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 30
        }
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 30
        }
      }
    ]
  }
}

// Outputs
output keyVaultId string = keyVault.id
output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
output hsmProvider string = hsmProvider
output keyVaultSku string = keyVaultSku

// KEK outputs for envelope encryption
output kekMasterKeyId string = enableEnvelopeEncryption && hsmProvider != 'Software' ? keyEncryptionKey.id : ''
output kekMasterKeyUri string = enableEnvelopeEncryption && hsmProvider != 'Software' ? keyEncryptionKey.properties.keyUri : ''

// Signing key output
output signingKeyId string = hsmProvider != 'Software' ? signingKey.id : ''
output signingKeyUri string = hsmProvider != 'Software' ? signingKey.properties.keyUri : ''

// Private endpoint output
output privateEndpointId string = enablePrivateEndpoint ? privateEndpoint.id : ''

// Configuration for application
output applicationConfig object = {
  provider: hsmProvider
  keyVaultUri: keyVault.properties.vaultUri
  enableEnvelopeEncryption: enableEnvelopeEncryption
  enableTenantIsolation: enableTenantIsolation
  keyRotationEnabled: enableKeyRotation
  keyRotationDays: keyRotationDays
  securityLevel: hsmProvider == 'Software' ? 'None' : hsmProvider == 'KeyVault' ? 'Software' : hsmProvider == 'KeyVaultHSM' ? 'FIPS-140-2-Level-1' : 'FIPS-140-2-Level-2'
}
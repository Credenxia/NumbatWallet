// Azure Managed HSM Module for NumbatWallet
// Purpose: FIPS 140-2 Level 2 compliant hardware security module for production
// Phase 2 of HSM implementation roadmap

@description('The name of the Managed HSM')
@minLength(3)
@maxLength(24)
param hsmName string

@description('Location for the Managed HSM')
param location string = resourceGroup().location

@description('Initial administrator object IDs')
param initialAdminObjectIds array

@description('Enable purge protection')
param enablePurgeProtection bool = true

@description('Soft delete retention in days')
@minValue(7)
@maxValue(90)
param softDeleteRetentionDays int = 90

@description('Enable public network access')
param enablePublicNetworkAccess bool = false

@description('Allowed IP addresses for firewall rules')
param allowedIpAddresses array = []

@description('Virtual network rules for service endpoints')
param virtualNetworkRules array = []

@description('Tags for the resource')
param tags object = {}

@description('Enable diagnostics')
param enableDiagnostics bool = true

@description('Log Analytics Workspace ID for diagnostics')
param logAnalyticsWorkspaceId string = ''

@description('Private endpoint subnet ID')
param privateEndpointSubnetId string = ''

@description('Private DNS Zone ID')
param privateDnsZoneId string = ''

@description('Enable key rotation')
param enableKeyRotation bool = true

@description('Key rotation period in days')
param keyRotationDays int = 90

@description('Number of tenant isolation keys to pre-create')
param tenantKeyCount int = 10

// Resource: Managed HSM
resource managedHsm 'Microsoft.KeyVault/managedHSMs@2023-07-01' = {
  name: hsmName
  location: location
  tags: union(tags, {
    Purpose: 'NumbatWallet-ManagedHSM'
    SecurityLevel: 'FIPS-140-2-Level-2'
    Environment: 'Production'
  })
  sku: {
    family: 'B'
    name: 'Standard_B1'
  }
  properties: {
    tenantId: tenant().tenantId
    initialAdminObjectIds: initialAdminObjectIds
    enableSoftDelete: true
    softDeleteRetentionInDays: softDeleteRetentionDays
    enablePurgeProtection: enablePurgeProtection
    publicNetworkAccess: enablePublicNetworkAccess ? 'Enabled' : 'Disabled'
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: enablePublicNetworkAccess ? 'Allow' : 'Deny'
      ipRules: [for ip in allowedIpAddresses: {
        value: ip
      }]
      virtualNetworkRules: [for vnet in virtualNetworkRules: {
        id: vnet
      }]
    }
  }
}

// Resource: Master Key Encryption Key
resource masterKEK 'Microsoft.KeyVault/managedHSMs/keys@2023-07-01' = {
  parent: managedHsm
  name: 'master-kek'
  properties: {
    kty: 'RSA-HSM'
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

// Resource: Tenant-specific KEKs
resource tenantKEKs 'Microsoft.KeyVault/managedHSMs/keys@2023-07-01' = [for i in range(1, tenantKeyCount): {
  parent: managedHsm
  name: 'tenant-kek-${padLeft(i, 3, '0')}'
  properties: {
    kty: 'RSA-HSM'
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

// Resource: Document Signing Key (ECDSA)
resource documentSigningKey 'Microsoft.KeyVault/managedHSMs/keys@2023-07-01' = {
  parent: managedHsm
  name: 'document-signing'
  properties: {
    kty: 'EC-HSM'
    crv: 'P-256'
    keyOps: [
      'sign'
      'verify'
    ]
    attributes: {
      enabled: true
      exportable: false
    }
    rotationPolicy: {
      attributes: {
        expiryTime: 'P365D'
      }
      lifetimeActions: [
        {
          action: {
            type: 'rotate'
          }
          trigger: {
            timeBeforeExpiry: 'P60D'
          }
        }
        {
          action: {
            type: 'notify'
          }
          trigger: {
            timeBeforeExpiry: 'P30D'
          }
        }
      ]
    }
  }
}

// Resource: Credential Signing Key (RSA)
resource credentialSigningKey 'Microsoft.KeyVault/managedHSMs/keys@2023-07-01' = {
  parent: managedHsm
  name: 'credential-signing'
  properties: {
    kty: 'RSA-HSM'
    keySize: 4096
    keyOps: [
      'sign'
      'verify'
    ]
    attributes: {
      enabled: true
      exportable: false
    }
    rotationPolicy: {
      attributes: {
        expiryTime: 'P180D'
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
    }
  }
}

// Resource: Backup Encryption Key
resource backupKey 'Microsoft.KeyVault/managedHSMs/keys@2023-07-01' = {
  parent: managedHsm
  name: 'backup-encryption'
  properties: {
    kty: 'RSA-HSM'
    keySize: 4096
    keyOps: [
      'encrypt'
      'decrypt'
    ]
    attributes: {
      enabled: true
      exportable: false
    }
  }
}

// Resource: Private Endpoint
resource privateEndpoint 'Microsoft.Network/privateEndpoints@2023-09-01' = if (privateEndpointSubnetId != '') {
  name: '${hsmName}-pe'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${hsmName}-pe-connection'
        properties: {
          privateLinkServiceId: managedHsm.id
          groupIds: [
            'managedhsm'
          ]
        }
      }
    ]
  }
}

// Resource: Private DNS Zone Group
resource privateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-09-01' = if (privateDnsZoneId != '') {
  parent: privateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'privatelink-managedhsm-azure-net'
        properties: {
          privateDnsZoneId: privateDnsZoneId
        }
      }
    ]
  }
}

// Resource: Diagnostic Settings
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (enableDiagnostics && logAnalyticsWorkspaceId != '') {
  name: '${hsmName}-diagnostics'
  scope: managedHsm
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        category: 'AuditEvent'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 365
        }
      }
      {
        category: 'KeyManagement'
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

// Resource: Role Assignment for Managed Identity
resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for objectId in initialAdminObjectIds: {
  name: guid(managedHsm.id, objectId, 'Managed HSM Crypto Officer')
  scope: managedHsm
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '515eb02d-2335-4d2d-92f2-b1cbdf9c3778') // Managed HSM Crypto Officer
    principalId: objectId
    principalType: 'ServicePrincipal'
  }
}]

// Outputs
output hsmId string = managedHsm.id
output hsmName string = managedHsm.name
output hsmUri string = managedHsm.properties.hsmUri
output securityDomainActivationStatus string = managedHsm.properties.provisioningState

// Key outputs
output masterKEKId string = masterKEK.id
output masterKEKUri string = masterKEK.properties.keyUri
output documentSigningKeyUri string = documentSigningKey.properties.keyUri
output credentialSigningKeyUri string = credentialSigningKey.properties.keyUri
output backupKeyUri string = backupKey.properties.keyUri

// Private endpoint output
output privateEndpointId string = privateEndpointSubnetId != '' ? privateEndpoint.id : ''

// Application configuration
output applicationConfig object = {
  provider: 'ManagedHSM'
  hsmUri: managedHsm.properties.hsmUri
  securityLevel: 'FIPS-140-2-Level-2'
  keyRotationEnabled: enableKeyRotation
  keyRotationDays: keyRotationDays
  tenantIsolation: true
  backupKeyUri: backupKey.properties.keyUri
}

// Migration readiness
output migrationInfo object = {
  fromProvider: 'KeyVaultHSM'
  toProvider: 'ManagedHSM'
  readyForMigration: managedHsm.properties.provisioningState == 'Succeeded'
  estimatedMigrationTime: '4-6 hours'
  rollbackSupported: true
}
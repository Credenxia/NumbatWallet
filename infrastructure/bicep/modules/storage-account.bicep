// Azure Storage Account Module for NumbatWallet
// Purpose: Blob storage for credentials, documents, backups, and audit logs

@description('Storage account name (must be globally unique)')
@minLength(3)
@maxLength(24)
param storageAccountName string

@description('Location for the storage account')
param location string = resourceGroup().location

@description('Storage account SKU')
@allowed([
  'Standard_LRS'
  'Standard_GRS'
  'Standard_RAGRS'
  'Standard_ZRS'
  'Premium_LRS'
  'Premium_ZRS'
])
param sku string = 'Standard_ZRS'

@description('Storage account kind')
@allowed([
  'Storage'
  'StorageV2'
  'BlobStorage'
  'FileStorage'
  'BlockBlobStorage'
])
param kind string = 'StorageV2'

@description('Access tier for blob storage')
@allowed([
  'Hot'
  'Cool'
])
param accessTier string = 'Hot'

@description('Enable hierarchical namespace for Data Lake')
param enableHierarchicalNamespace bool = false

@description('Enable blob soft delete')
param enableBlobSoftDelete bool = true

@description('Blob soft delete retention days')
@minValue(1)
@maxValue(365)
param blobSoftDeleteRetentionDays int = 30

@description('Enable container soft delete')
param enableContainerSoftDelete bool = true

@description('Container soft delete retention days')
@minValue(1)
@maxValue(365)
param containerSoftDeleteRetentionDays int = 7

@description('Enable versioning for blobs')
param enableVersioning bool = true

@description('Enable change feed')
param enableChangeFeed bool = true

@description('Enable point-in-time restore')
param enablePointInTimeRestore bool = true

@description('Point-in-time restore days')
@minValue(1)
@maxValue(365)
param pointInTimeRestoreDays int = 7

@description('Minimum TLS version')
@allowed([
  'TLS1_0'
  'TLS1_1'
  'TLS1_2'
])
param minimumTlsVersion string = 'TLS1_2'

@description('Allow blob public access')
param allowBlobPublicAccess bool = false

@description('Allow shared key access')
param allowSharedKeyAccess bool = true

@description('Default to OAuth authentication')
param defaultToOAuthAuthentication bool = true

@description('Allowed IP addresses for firewall')
param allowedIpAddresses array = []

@description('Virtual network rules')
param virtualNetworkRules array = []

@description('Enable private endpoints')
param enablePrivateEndpoints bool = true

@description('Private endpoint subnet ID')
param privateEndpointSubnetId string = ''

@description('Log Analytics workspace ID for diagnostics')
param logAnalyticsWorkspaceId string = ''

@description('Tags to apply to resources')
param tags object = {
  Component: 'Storage'
  Purpose: 'Blob Storage'
}

// Storage Account Resource
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  tags: tags
  sku: {
    name: sku
  }
  kind: kind
  properties: {
    accessTier: accessTier
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: minimumTlsVersion
    allowBlobPublicAccess: allowBlobPublicAccess
    allowSharedKeyAccess: allowSharedKeyAccess
    defaultToOAuthAuthentication: defaultToOAuthAuthentication
    allowCrossTenantReplication: false
    isHnsEnabled: enableHierarchicalNamespace
    isNfsV3Enabled: false
    isSftpEnabled: false
    isLocalUserEnabled: false

    networkAcls: {
      defaultAction: length(allowedIpAddresses) > 0 || length(virtualNetworkRules) > 0 ? 'Deny' : 'Allow'
      bypass: 'AzureServices'
      ipRules: [for ip in allowedIpAddresses: {
        value: ip
        action: 'Allow'
      }]
      virtualNetworkRules: [for rule in virtualNetworkRules: {
        id: rule.subnetId
        action: 'Allow'
        state: 'Succeeded'
      }]
    }

    encryption: {
      services: {
        file: {
          enabled: true
          keyType: 'Account'
        }
        blob: {
          enabled: true
          keyType: 'Account'
        }
        table: {
          enabled: true
          keyType: 'Account'
        }
        queue: {
          enabled: true
          keyType: 'Account'
        }
      }
      keySource: 'Microsoft.Storage'
      requireInfrastructureEncryption: true
    }
  }
}

// Blob Service Configuration
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    cors: {
      corsRules: [
        {
          allowedOrigins: ['https://*.servicewa.wa.gov.au']
          allowedMethods: ['GET', 'HEAD', 'POST', 'PUT', 'OPTIONS']
          allowedHeaders: ['*']
          exposedHeaders: ['*']
          maxAgeInSeconds: 3600
        }
      ]
    }

    deleteRetentionPolicy: {
      enabled: enableBlobSoftDelete
      days: blobSoftDeleteRetentionDays
      allowPermanentDelete: false
    }

    containerDeleteRetentionPolicy: {
      enabled: enableContainerSoftDelete
      days: containerSoftDeleteRetentionDays
    }

    isVersioningEnabled: enableVersioning

    changeFeed: {
      enabled: enableChangeFeed
      retentionInDays: 365
    }

    restorePolicy: enablePointInTimeRestore ? {
      enabled: true
      days: pointInTimeRestoreDays
    } : null

    lastAccessTimeTrackingPolicy: {
      enable: true
      name: 'AccessTimeTracking'
      trackingGranularityInDays: 1
    }
  }
}

// Blob Containers
var containers = [
  {
    name: 'credentials'
    publicAccess: 'None'
    metadata: {
      purpose: 'Verifiable Credential storage'
      encrypted: 'true'
      compliance: 'TDIF'
    }
  }
  {
    name: 'documents'
    publicAccess: 'None'
    metadata: {
      purpose: 'Supporting documents and attachments'
      encrypted: 'true'
    }
  }
  {
    name: 'backups'
    publicAccess: 'None'
    metadata: {
      purpose: 'Wallet and credential backups'
      encrypted: 'true'
      retention: 'long-term'
    }
  }
  {
    name: 'audit'
    publicAccess: 'None'
    immutableStorage: true
    metadata: {
      purpose: 'Immutable audit logs'
      compliance: 'required'
      retention: 'regulatory'
    }
  }
  {
    name: 'logs'
    publicAccess: 'None'
    metadata: {
      purpose: 'Application and diagnostic logs'
      retention: 'short-term'
    }
  }
  {
    name: 'temp'
    publicAccess: 'None'
    metadata: {
      purpose: 'Temporary file storage'
      retention: '24-hours'
    }
  }
]

resource blobContainers 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = [for container in containers: {
  parent: blobService
  name: container.name
  properties: {
    publicAccess: container.publicAccess
    metadata: container.?metadata ?? {}
    immutableStorageWithVersioning: contains(container, 'immutableStorage') && container.immutableStorage ? {
      enabled: true
    } : null
  }
}]

// Lifecycle Management Policies
resource lifecyclePolicy 'Microsoft.Storage/storageAccounts/managementPolicies@2023-01-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    policy: {
      rules: [
        {
          name: 'MoveToCoolTier'
          enabled: true
          type: 'Lifecycle'
          definition: {
            filters: {
              blobTypes: ['blockBlob']
              prefixMatch: ['logs/', 'temp/']
            }
            actions: {
              baseBlob: {
                tierToCool: {
                  daysAfterLastAccessTimeGreaterThan: 30
                }
                tierToArchive: {
                  daysAfterLastAccessTimeGreaterThan: 90
                }
              }
            }
          }
        }
        {
          name: 'DeleteOldLogs'
          enabled: true
          type: 'Lifecycle'
          definition: {
            filters: {
              blobTypes: ['blockBlob']
              prefixMatch: ['logs/']
            }
            actions: {
              baseBlob: {
                delete: {
                  daysAfterCreationGreaterThan: 365
                }
              }
              snapshot: {
                delete: {
                  daysAfterCreationGreaterThan: 90
                }
              }
            }
          }
        }
        {
          name: 'DeleteTempFiles'
          enabled: true
          type: 'Lifecycle'
          definition: {
            filters: {
              blobTypes: ['blockBlob']
              prefixMatch: ['temp/']
            }
            actions: {
              baseBlob: {
                delete: {
                  daysAfterCreationGreaterThan: 1
                }
              }
            }
          }
        }
        {
          name: 'ArchiveOldBackups'
          enabled: true
          type: 'Lifecycle'
          definition: {
            filters: {
              blobTypes: ['blockBlob']
              prefixMatch: ['backups/']
            }
            actions: {
              baseBlob: {
                tierToArchive: {
                  daysAfterLastAccessTimeGreaterThan: 180
                }
              }
            }
          }
        }
      ]
    }
  }
}

// Private Endpoints for Blob Storage
resource privateEndpointBlob 'Microsoft.Network/privateEndpoints@2023-06-01' = if (enablePrivateEndpoints && !empty(privateEndpointSubnetId)) {
  name: '${storageAccountName}-blob-pe'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${storageAccountName}-blob-pe-connection'
        properties: {
          privateLinkServiceId: storageAccount.id
          groupIds: ['blob']
        }
      }
    ]
  }
}

// Private DNS Zone Group for Blob
resource privateDnsZoneGroupBlob 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-06-01' = if (enablePrivateEndpoints && !empty(privateEndpointSubnetId)) {
  parent: privateEndpointBlob
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'privatelink-blob-core-windows-net'
        properties: {
          privateDnsZoneId: '/subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.Network/privateDnsZones/privatelink.blob.core.windows.net'
        }
      }
    ]
  }
}

// Diagnostic Settings
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (!empty(logAnalyticsWorkspaceId)) {
  name: 'diag-${storageAccountName}'
  scope: storageAccount
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    metrics: [
      {
        category: 'Transaction'
        enabled: true
        retentionPolicy: {
          days: 30
          enabled: true
        }
      }
      {
        category: 'Capacity'
        enabled: true
        retentionPolicy: {
          days: 30
          enabled: true
        }
      }
    ]
  }
}

// Blob Service Diagnostic Settings
resource blobDiagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (!empty(logAnalyticsWorkspaceId)) {
  name: 'diag-${storageAccountName}-blob'
  scope: blobService
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        categoryGroup: 'allLogs'
        enabled: true
        retentionPolicy: {
          days: 30
          enabled: true
        }
      }
    ]
    metrics: [
      {
        category: 'Transaction'
        enabled: true
        retentionPolicy: {
          days: 30
          enabled: true
        }
      }
      {
        category: 'Capacity'
        enabled: true
        retentionPolicy: {
          days: 30
          enabled: true
        }
      }
    ]
  }
}

// Outputs
output storageAccountId string = storageAccount.id
output storageAccountName string = storageAccount.name
output primaryEndpoints object = storageAccount.properties.primaryEndpoints
output primaryBlobEndpoint string = storageAccount.properties.primaryEndpoints.blob
output primaryConnectionString string = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
output primaryAccessKey string = storageAccount.listKeys().keys[0].value
output privateEndpointId string = enablePrivateEndpoints && !empty(privateEndpointSubnetId) ? privateEndpointBlob.id : ''
output blobContainerNames array = [for container in containers: container.name]
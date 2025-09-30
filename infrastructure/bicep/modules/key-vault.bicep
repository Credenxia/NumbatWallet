// Azure Key Vault Module for NumbatWallet
// Purpose: Secure storage for application secrets, certificates, and encryption keys

@description('The name of the Key Vault')
@minLength(3)
@maxLength(24)
param keyVaultName string

@description('Location for the Key Vault')
param location string = resourceGroup().location

@description('Key Vault SKU')
@allowed([
  'standard'
  'premium'
])
param sku string = 'standard'

@description('Enable soft delete protection')
param enableSoftDelete bool = true

@description('Soft delete retention in days')
@minValue(7)
@maxValue(90)
param softDeleteRetentionDays int = 90

@description('Enable purge protection')
param enablePurgeProtection bool = true

@description('Enable RBAC authorization')
param enableRbacAuthorization bool = true

@description('Enable public network access')
param enablePublicNetworkAccess bool = false

@description('Allowed IP addresses for firewall rules')
param allowedIpAddresses array = []

@description('Virtual network rules for service endpoints')
param virtualNetworkRules array = []

@description('Object ID of the managed identity for key vault access')
param managedIdentityObjectId string = ''

@description('Additional access policies')
param accessPolicies array = []

@description('Enable private endpoint')
param enablePrivateEndpoint bool = true

@description('Private endpoint subnet ID')
param privateEndpointSubnetId string = ''

@description('Log Analytics workspace ID for diagnostics')
param logAnalyticsWorkspaceId string = ''

@description('Tags to apply to the Key Vault')
param tags object = {
  Environment: 'Production'
  Application: 'NumbatWallet'
  Component: 'KeyVault'
  ManagedBy: 'Bicep'
}

// Key Vault Resource
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: sku
    }
    tenantId: subscription().tenantId

    // Access policies (if not using RBAC)
    accessPolicies: enableRbacAuthorization ? [] : concat(
      // Managed Identity access policy
      !empty(managedIdentityObjectId) ? [
        {
          tenantId: subscription().tenantId
          objectId: managedIdentityObjectId
          permissions: {
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
              'create'
              'import'
              'update'
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
            keys: [
              'get'
              'list'
              'create'
              'import'
              'update'
              'delete'
              'recover'
              'backup'
              'restore'
              'encrypt'
              'decrypt'
              'wrapKey'
              'unwrapKey'
              'sign'
              'verify'
            ]
          }
        }
      ] : [],
      // Additional access policies
      accessPolicies
    )

    // RBAC Authorization
    enableRbacAuthorization: enableRbacAuthorization

    // Soft Delete & Purge Protection
    enableSoftDelete: enableSoftDelete
    softDeleteRetentionInDays: softDeleteRetentionDays
    enablePurgeProtection: enablePurgeProtection

    // Network ACLs
    publicNetworkAccess: enablePublicNetworkAccess ? 'Enabled' : 'Disabled'
    networkAcls: {
      defaultAction: length(allowedIpAddresses) > 0 || length(virtualNetworkRules) > 0 ? 'Deny' : 'Allow'
      bypass: 'AzureServices'
      ipRules: [for ip in allowedIpAddresses: {
        value: ip
      }]
      virtualNetworkRules: [for rule in virtualNetworkRules: {
        id: rule.subnetId
        ignoreMissingVnetServiceEndpoint: false
      }]
    }

    // Additional security settings
    enabledForDeployment: false
    enabledForDiskEncryption: true
    enabledForTemplateDeployment: true
  }
}

// Private Endpoint for Key Vault
resource privateEndpoint 'Microsoft.Network/privateEndpoints@2023-06-01' = if (enablePrivateEndpoint && !empty(privateEndpointSubnetId)) {
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
          groupIds: ['vault']
        }
      }
    ]
  }
}

// Private DNS Zone Group (for private endpoint)
resource privateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-06-01' = if (enablePrivateEndpoint && !empty(privateEndpointSubnetId)) {
  parent: privateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'privatelink-vaultcore-azure-net'
        properties: {
          privateDnsZoneId: '/subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.Network/privateDnsZones/privatelink.vaultcore.azure.net'
        }
      }
    ]
  }
}

// Diagnostic Settings
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (!empty(logAnalyticsWorkspaceId)) {
  name: 'diag-${keyVaultName}'
  scope: keyVault
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        categoryGroup: 'audit'
        enabled: true
        retentionPolicy: {
          days: 90
          enabled: true
        }
      }
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
        category: 'AllMetrics'
        enabled: true
        retentionPolicy: {
          days: 30
          enabled: true
        }
      }
    ]
  }
}

// RBAC Role Assignments (if RBAC is enabled)
resource keyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (enableRbacAuthorization && !empty(managedIdentityObjectId)) {
  name: guid(keyVault.id, managedIdentityObjectId, 'Key Vault Secrets User')
  scope: keyVault
  properties: {
    principalId: managedIdentityObjectId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6') // Key Vault Secrets User
    principalType: 'ServicePrincipal'
  }
}

resource keyVaultCryptoUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (enableRbacAuthorization && !empty(managedIdentityObjectId)) {
  name: guid(keyVault.id, managedIdentityObjectId, 'Key Vault Crypto User')
  scope: keyVault
  properties: {
    principalId: managedIdentityObjectId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '12338af0-0e69-4776-bea7-57ae8d297424') // Key Vault Crypto User
    principalType: 'ServicePrincipal'
  }
}

resource keyVaultCertificatesOfficer 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (enableRbacAuthorization && !empty(managedIdentityObjectId)) {
  name: guid(keyVault.id, managedIdentityObjectId, 'Key Vault Certificates Officer')
  scope: keyVault
  properties: {
    principalId: managedIdentityObjectId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a4417e6f-fecd-4de8-b567-7b0420556985') // Key Vault Certificates Officer
    principalType: 'ServicePrincipal'
  }
}

// Common Secrets to Create
var secretsToCreate = [
  {
    name: 'DatabaseConnectionString'
    value: ''
    contentType: 'text/plain'
    tags: {
      Purpose: 'PostgreSQL connection'
      Environment: 'Production'
    }
  }
  {
    name: 'AzureAdClientSecret'
    value: ''
    contentType: 'text/plain'
    tags: {
      Purpose: 'Azure AD authentication'
      Environment: 'Production'
    }
  }
  {
    name: 'StorageAccountKey'
    value: ''
    contentType: 'text/plain'
    tags: {
      Purpose: 'Blob storage access'
      Environment: 'Production'
    }
  }
  {
    name: 'ApplicationInsightsInstrumentationKey'
    value: ''
    contentType: 'text/plain'
    tags: {
      Purpose: 'Application telemetry'
      Environment: 'Production'
    }
  }
  {
    name: 'JwtSigningKey'
    value: ''
    contentType: 'text/plain'
    tags: {
      Purpose: 'JWT token signing'
      Environment: 'Production'
    }
  }
  {
    name: 'EncryptionKey'
    value: ''
    contentType: 'text/plain'
    tags: {
      Purpose: 'Data encryption at rest'
      Environment: 'Production'
    }
  }
]

// Create placeholder secrets (values to be updated post-deployment)
resource secrets 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = [for secret in secretsToCreate: {
  parent: keyVault
  name: secret.name
  properties: {
    value: secret.value
    contentType: secret.contentType
    attributes: {
      enabled: true
    }
  }
  tags: secret.tags
}]

// Outputs
output keyVaultId string = keyVault.id
output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
output privateEndpointId string = enablePrivateEndpoint && !empty(privateEndpointSubnetId) ? privateEndpoint.id : ''
output keyVaultResourceId string = keyVault.id

// Output for reference in other modules
output keyVaultSecrets object = {
  databaseConnectionString: '${keyVault.properties.vaultUri}secrets/DatabaseConnectionString'
  azureAdClientSecret: '${keyVault.properties.vaultUri}secrets/AzureAdClientSecret'
  storageAccountKey: '${keyVault.properties.vaultUri}secrets/StorageAccountKey'
  appInsightsKey: '${keyVault.properties.vaultUri}secrets/ApplicationInsightsInstrumentationKey'
  jwtSigningKey: '${keyVault.properties.vaultUri}secrets/JwtSigningKey'
  encryptionKey: '${keyVault.properties.vaultUri}secrets/EncryptionKey'
}
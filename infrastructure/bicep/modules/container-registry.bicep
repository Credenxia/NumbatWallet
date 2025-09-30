// Azure Container Registry Module for NumbatWallet
// Purpose: Private Docker registry for container images

@description('Container Registry name')
@minLength(5)
@maxLength(50)
param registryName string

@description('Location for Container Registry')
param location string = resourceGroup().location

@description('Container Registry SKU')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param sku string = 'Premium'

@description('Enable admin user')
param enableAdminUser bool = false

@description('Enable public network access')
param publicNetworkAccess string = 'Enabled'

@description('Enable zone redundancy (Premium SKU only)')
param zoneRedundancy string = 'Enabled'

@description('Enable content trust')
param enableContentTrust bool = true

@description('Enable retention policy')
param enableRetentionPolicy bool = true

@description('Retention policy days')
@minValue(1)
@maxValue(365)
param retentionDays int = 30

@description('Enable quarantine policy')
param enableQuarantinePolicy bool = true

@description('Enable Azure AD RBAC')
param enableAzureRBAC bool = true

@description('Data endpoint enabled')
param dataEndpointEnabled bool = false

@description('Network rule set default action')
@allowed([
  'Allow'
  'Deny'
])
param networkRuleSetDefaultAction string = 'Deny'

@description('Network rule bypass options')
@allowed([
  'None'
  'AzureServices'
])
param networkRuleBypassOptions string = 'AzureServices'

@description('Allowed IP addresses')
param allowedIpAddresses array = []

@description('Virtual network rules')
param virtualNetworkRules array = []

@description('Enable private endpoint')
param enablePrivateEndpoint bool = true

@description('Private endpoint subnet ID')
param privateEndpointSubnetId string = ''

@description('Enable encryption')
param enableEncryption bool = false

@description('Customer managed key ID')
param customerManagedKeyId string = ''

@description('Enable export policy')
param enableExportPolicy bool = false

@description('Enable anonymous pull')
param enableAnonymousPull bool = false

@description('Soft delete policy days')
@minValue(1)
@maxValue(90)
param softDeletePolicyDays int = 7

@description('Soft delete policy status')
@allowed([
  'disabled'
  'enabled'
])
param softDeletePolicyStatus string = 'enabled'

@description('Log Analytics workspace ID for diagnostics')
param logAnalyticsWorkspaceId string = ''

@description('Tags to apply to Container Registry')
param tags object = {
  Component: 'Container Registry'
  Purpose: 'Image Storage'
}

// Container Registry Resource
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: registryName
  location: location
  tags: tags
  sku: {
    name: sku
  }
  properties: {
    adminUserEnabled: enableAdminUser
    publicNetworkAccess: publicNetworkAccess
    zoneRedundancy: sku == 'Premium' ? zoneRedundancy : 'Disabled'

    dataEndpointEnabled: dataEndpointEnabled

    encryption: enableEncryption && !empty(customerManagedKeyId) ? {
      status: 'enabled'
      keyVaultProperties: {
        keyIdentifier: customerManagedKeyId
      }
    } : {
      status: 'disabled'
    }

    networkRuleSet: {
      defaultAction: networkRuleSetDefaultAction
      ipRules: [for ip in allowedIpAddresses: {
        value: ip
        action: 'Allow'
      }]
      virtualNetworkRules: [for rule in virtualNetworkRules: {
        id: rule.subnetId
        action: 'Allow'
      }]
    }

    policies: {
      quarantinePolicy: {
        status: enableQuarantinePolicy ? 'enabled' : 'disabled'
      }
      trustPolicy: {
        type: 'Notary'
        status: enableContentTrust ? 'enabled' : 'disabled'
      }
      retentionPolicy: sku == 'Premium' && enableRetentionPolicy ? {
        days: retentionDays
        status: 'enabled'
      } : {
        days: 7
        status: 'disabled'
      }
      exportPolicy: {
        status: enableExportPolicy ? 'enabled' : 'disabled'
      }
      azureADAuthenticationAsArmPolicy: {
        status: enableAzureRBAC ? 'enabled' : 'disabled'
      }
      softDeletePolicy: {
        retentionDays: softDeletePolicyDays
        status: softDeletePolicyStatus
      }
    }

    anonymousPullEnabled: enableAnonymousPull

    networkRuleBypassOptions: networkRuleBypassOptions
  }
}

// Replications for geo-redundancy (Premium SKU only)
resource replication 'Microsoft.ContainerRegistry/registries/replications@2023-11-01-preview' = if (sku == 'Premium') {
  parent: containerRegistry
  name: 'australiasoutheast'
  location: 'australiasoutheast'
  tags: tags
  properties: {
    regionEndpointEnabled: true
    zoneRedundancy: 'Enabled'
  }
}

// Webhooks
resource webhook 'Microsoft.ContainerRegistry/registries/webhooks@2023-11-01-preview' = {
  parent: containerRegistry
  name: 'containerapp-webhook'
  location: location
  tags: tags
  properties: {
    status: 'enabled'
    scope: '*'
    actions: [
      'push'
      'delete'
    ]
    serviceUri: 'https://api.numbatwallet.wa.gov.au/webhook/container-registry'
    customHeaders: {
      'X-Webhook-Secret': 'WEBHOOK_SECRET_PLACEHOLDER'  // To be replaced with Key Vault reference
    }
  }
}

// Scope Maps for RBAC (Premium SKU only)
resource scopeMap 'Microsoft.ContainerRegistry/registries/scopeMaps@2023-11-01-preview' = if (sku == 'Premium') {
  parent: containerRegistry
  name: 'numbatwallet-read'
  properties: {
    description: 'Read access to NumbatWallet repositories'
    actions: [
      'repositories/*/content/read'
      'repositories/*/metadata/read'
    ]
  }
}

// Tokens for service principals (Premium SKU only)
resource token 'Microsoft.ContainerRegistry/registries/tokens@2023-11-01-preview' = if (sku == 'Premium') {
  parent: containerRegistry
  name: 'containerapp-token'
  properties: {
    scopeMapId: scopeMap.id
    status: 'enabled'
  }
}

// Tasks for automated builds (example)
resource task 'Microsoft.ContainerRegistry/registries/tasks@2019-06-01-preview' = if (false) {
  parent: containerRegistry
  name: 'build-api-image'
  location: location
  tags: tags
  properties: {
    status: 'Enabled'
    platform: {
      os: 'Linux'
      architecture: 'amd64'
    }
    agentConfiguration: {
      cpu: 2
    }
    step: {
      type: 'Docker'
      dockerFilePath: 'src/NumbatWallet.Web.Api/Dockerfile'
      contextPath: 'https://github.com/Credenxia/NumbatWallet.git'
      imageNames: [
        '${containerRegistry.name}.azurecr.io/numbatwallet-api:{{.Run.ID}}'
        '${containerRegistry.name}.azurecr.io/numbatwallet-api:latest'
      ]
      isPushEnabled: true
      noCache: false
      arguments: []
    }
    trigger: {
      sourceTriggers: [
        {
          name: 'defaultSourceTrigger'
          sourceRepository: {
            sourceControlType: 'Github'
            repositoryUrl: 'https://github.com/Credenxia/NumbatWallet.git'
            branch: 'main'
          }
          sourceTriggerEvents: [
            'commit'
            'pullrequest'
          ]
        }
      ]
      baseImageTrigger: {
        name: 'defaultBaseimageTrigger'
        baseImageTriggerType: 'Runtime'
        status: 'Enabled'
      }
    }
  }
}

// Private Endpoint for Container Registry
resource privateEndpoint 'Microsoft.Network/privateEndpoints@2023-06-01' = if (enablePrivateEndpoint && !empty(privateEndpointSubnetId)) {
  name: '${registryName}-pe'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${registryName}-pe-connection'
        properties: {
          privateLinkServiceId: containerRegistry.id
          groupIds: ['registry']
        }
      }
    ]
  }
}

// Private DNS Zone Group for Container Registry
resource privateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-06-01' = if (enablePrivateEndpoint && !empty(privateEndpointSubnetId)) {
  parent: privateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'privatelink-azurecr-io'
        properties: {
          privateDnsZoneId: '/subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.Network/privateDnsZones/privatelink.azurecr.io'
        }
      }
    ]
  }
}

// Diagnostic Settings
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (!empty(logAnalyticsWorkspaceId)) {
  name: 'diag-${registryName}'
  scope: containerRegistry
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

// Outputs
output registryId string = containerRegistry.id
output registryName string = containerRegistry.name
output loginServer string = containerRegistry.properties.loginServer
output adminUsername string = enableAdminUser ? containerRegistry.listCredentials().username : ''
output adminPassword string = enableAdminUser ? containerRegistry.listCredentials().passwords[0].value : ''
output privateEndpointId string = enablePrivateEndpoint && !empty(privateEndpointSubnetId) ? privateEndpoint.id : ''
output dataEndpoints array = dataEndpointEnabled && sku == 'Premium' ? containerRegistry.properties.dataEndpointHostNames : []
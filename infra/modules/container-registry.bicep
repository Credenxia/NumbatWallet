// POA-003: Azure Container Registry module
// Container Registry for NumbatWallet Docker images

@description('The environment for deployment')
@allowed(['dev', 'test', 'prod'])
param environment string

@description('The Azure region for resources')
param location string

@description('Base name for resource naming')
param baseName string

@description('Enable geo-replication for production')
param enableGeoReplication bool = environment == 'prod'

@description('Enable content trust for signed images')
param enableContentTrust bool = environment == 'prod'

// Variables
var acrName = replace('${baseName}${environment}acr', '-', '')
var skuName = environment == 'prod' ? 'Premium' : 'Standard'

// Container Registry
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  sku: {
    name: skuName
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: environment == 'dev' ? 'Enabled' : 'Disabled'
    networkRuleBypassOptions: 'AzureServices'
    policies: {
      quarantinePolicy: {
        status: 'enabled'
      }
      retentionPolicy: {
        days: environment == 'prod' ? 30 : 7
        status: 'enabled'
      }
      trustPolicy: {
        type: 'Notary'
        status: enableContentTrust ? 'enabled' : 'disabled'
      }
      azureADAuthenticationAsArmPolicy: {
        status: 'enabled'
      }
    }
    encryption: {
      status: environment == 'prod' ? 'enabled' : 'disabled'
    }
    dataEndpointEnabled: false
    anonymousPullEnabled: false
    zoneRedundancy: environment == 'prod' ? 'Enabled' : 'Disabled'
  }
  tags: {
    Environment: environment
    ManagedBy: 'Bicep'
    Application: 'NumbatWallet'
  }
}

// Geo-replication for production
resource geoReplication 'Microsoft.ContainerRegistry/registries/replications@2023-07-01' = if (enableGeoReplication) {
  parent: containerRegistry
  name: 'australiaeast'
  location: 'australiaeast'
  properties: {
    zoneRedundancy: 'Enabled'
  }
  tags: {
    Environment: environment
    ManagedBy: 'Bicep'
    Application: 'NumbatWallet'
  }
}

// Diagnostic settings
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (environment == 'prod') {
  name: '${acrName}-diagnostics'
  scope: containerRegistry
  properties: {
    workspaceId: '/subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.OperationalInsights/workspaces/${baseName}-${environment}-law'
    logs: [
      {
        category: 'ContainerRegistryRepositoryEvents'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 30
        }
      }
      {
        category: 'ContainerRegistryLoginEvents'
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

// Vulnerability scanning webhook
resource vulnerabilityScanWebhook 'Microsoft.ContainerRegistry/registries/webhooks@2023-07-01' = if (environment == 'prod') {
  parent: containerRegistry
  name: 'vulnerabilityscan'
  location: location
  properties: {
    status: 'enabled'
    scope: '*'
    actions: [
      'push'
    ]
    serviceUri: 'https://numbatwallet.wa.gov.au/api/webhooks/acr-scan'
    customHeaders: {
      'X-Webhook-Secret': 'acr-scan-secret'
    }
  }
}

// Build tasks for CI/CD
resource buildTask 'Microsoft.ContainerRegistry/registries/tasks@2019-04-01' = {
  parent: containerRegistry
  name: 'numbat-build-task'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
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
      contextPath: 'https://github.com/Credenxia/NumbatWallet.git#main'
      imageNames: [
        '${acrName}.azurecr.io/numbatwallet-api:{{.Run.ID}}'
        '${acrName}.azurecr.io/numbatwallet-api:latest'
      ]
      arguments: [
        '--build-arg'
        'VERSION={{.Run.ID}}'
      ]
    }
    trigger: {
      sourceTriggers: [
        {
          name: 'defaultSourceTrigger'
          sourceRepository: {
            sourceControlType: 'Github'
            repositoryUrl: 'https://github.com/Credenxia/NumbatWallet'
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
    timeout: 3600
    status: 'Enabled'
  }
  tags: {
    Environment: environment
    ManagedBy: 'Bicep'
    Application: 'NumbatWallet'
  }
}

// Outputs
output acrName string = containerRegistry.name
output acrLoginServer string = containerRegistry.properties.loginServer
output acrId string = containerRegistry.id
output acrPrincipalId string = containerRegistry.identity.principalId
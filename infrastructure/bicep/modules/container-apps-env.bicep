// Azure Container Apps Environment Module for NumbatWallet
// Purpose: Managed Kubernetes environment for hosting containerized applications

@description('Container Apps Environment name')
@minLength(1)
@maxLength(64)
param environmentName string

@description('Location for Container Apps Environment')
param location string = resourceGroup().location

@description('Log Analytics workspace ID')
param logAnalyticsWorkspaceId string

@description('Enable zone redundancy')
param enableZoneRedundancy bool = true

@description('Infrastructure subnet ID for Container Apps')
param infrastructureSubnetId string = ''

@description('Enable internal load balancer only')
param internalOnly bool = false

@description('Docker bridge CIDR')
param dockerBridgeCidr string = '10.1.0.1/16'

@description('Platform reserved CIDR')
param platformReservedCidr string = '10.2.0.0/16'

@description('Platform reserved DNS IP')
param platformReservedDnsIP string = '10.2.0.2'

@description('Enable workload profiles')
param enableWorkloadProfiles bool = true

@description('Tags to apply to Container Apps Environment')
param tags object = {
  Component: 'Container Platform'
  Purpose: 'Application Hosting'
}

// Get Log Analytics workspace details for customer ID and shared key
resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: last(split(logAnalyticsWorkspaceId, '/'))
  scope: resourceGroup(split(logAnalyticsWorkspaceId, '/')[2], split(logAnalyticsWorkspaceId, '/')[4])
}

// Container Apps Environment
resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-11-02-preview' = {
  name: environmentName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: workspace.properties.customerId
        sharedKey: workspace.listKeys().primarySharedKey
        dynamicJsonColumns: true
      }
    }

    zoneRedundant: enableZoneRedundancy

    vnetConfiguration: !empty(infrastructureSubnetId) ? {
      infrastructureSubnetId: infrastructureSubnetId
      internal: internalOnly
      dockerBridgeCidr: dockerBridgeCidr
      platformReservedCidr: platformReservedCidr
      platformReservedDnsIP: platformReservedDnsIP
    } : null

    workloadProfiles: enableWorkloadProfiles ? [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
        minimumCount: 0
        maximumCount: 3
      }
      {
        name: 'Dedicated-D4'
        workloadProfileType: 'D4'
        minimumCount: 1
        maximumCount: 3
      }
    ] : null

    peerAuthentication: {
      mtls: {
        enabled: true
      }
    }
  }
}

// Dapr Components
resource daprStateStore 'Microsoft.App/managedEnvironments/daprComponents@2023-11-02-preview' = {
  parent: containerAppsEnvironment
  name: 'statestore'
  properties: {
    componentType: 'state.redis'
    version: 'v1'
    metadata: [
      {
        name: 'redisHost'
        value: 'redis-numbatwallet.redis.cache.windows.net:6380'
      }
      {
        name: 'redisPassword'
        secretRef: 'redis-password'
      }
      {
        name: 'enableTLS'
        value: 'true'
      }
    ]
    secrets: [
      {
        name: 'redis-password'
        value: 'REDIS_PASSWORD_PLACEHOLDER'  // To be replaced with Key Vault reference
      }
    ]
    scopes: [
      'numbatwallet-api'
      'numbatwallet-admin'
    ]
  }
}

resource daprPubSub 'Microsoft.App/managedEnvironments/daprComponents@2023-11-02-preview' = {
  parent: containerAppsEnvironment
  name: 'pubsub'
  properties: {
    componentType: 'pubsub.redis'
    version: 'v1'
    metadata: [
      {
        name: 'redisHost'
        value: 'redis-numbatwallet.redis.cache.windows.net:6380'
      }
      {
        name: 'redisPassword'
        secretRef: 'redis-password'
      }
      {
        name: 'enableTLS'
        value: 'true'
      }
    ]
    secrets: [
      {
        name: 'redis-password'
        value: 'REDIS_PASSWORD_PLACEHOLDER'  // To be replaced with Key Vault reference
      }
    ]
    scopes: [
      'numbatwallet-api'
      'numbatwallet-admin'
    ]
  }
}

// Storage Component for Container Apps
resource storage 'Microsoft.App/managedEnvironments/storages@2023-11-02-preview' = {
  parent: containerAppsEnvironment
  name: 'azure-files'
  properties: {
    azureFile: {
      accountName: 'stnumbatwallet'
      accountKey: 'STORAGE_KEY_PLACEHOLDER'  // To be replaced with Key Vault reference
      shareName: 'containerappshare'
      accessMode: 'ReadWrite'
    }
  }
}

// Certificate for custom domain (example)
resource certificate 'Microsoft.App/managedEnvironments/certificates@2023-11-02-preview' = if (false) {
  parent: containerAppsEnvironment
  name: 'numbatwallet-cert'
  properties: {
    value: 'BASE64_ENCODED_PFX'  // To be replaced with actual certificate
    password: 'CERTIFICATE_PASSWORD'  // To be replaced with Key Vault reference
  }
}

// Diagnostic Settings
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'diag-${environmentName}'
  scope: containerAppsEnvironment
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
output environmentId string = containerAppsEnvironment.id
output environmentName string = containerAppsEnvironment.name
output defaultDomain string = containerAppsEnvironment.properties.defaultDomain
output staticIp string = containerAppsEnvironment.properties.staticIp ?? ''
output infrastructureResourceGroup string = containerAppsEnvironment.properties.infrastructureResourceGroup ?? ''
output customDomainVerificationId string = containerAppsEnvironment.properties.customDomainConfiguration.customDomainVerificationId ?? ''
// Azure Functions for Key Rotation Automation
// POA-131: Key Rotation Policies

@description('Base name for Function App')
param functionAppName string

@description('Location for resources')
param location string = resourceGroup().location

@description('Key Vault resource ID')
param keyVaultResourceId string

@description('Service Bus connection string')
@secure()
param serviceBusConnectionString string

@description('Application Insights connection string')
param applicationInsightsConnectionString string

@description('Storage account name for Function App')
param storageAccountName string

@description('Tags for resources')
param tags object = {
  Purpose: 'KeyRotation'
  Component: 'Automation'
  ManagedBy: 'Platform Team'
}

// Storage Account for Function App
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    networkAcls: {
      defaultAction: 'Deny'
      bypass: 'AzureServices'
    }
  }
}

// App Service Plan for Functions
resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: '${functionAppName}-plan'
  location: location
  tags: tags
  sku: {
    name: 'P1v3'
    tier: 'Premium'
    capacity: 2 // For high availability
  }
  kind: 'functionapp'
  properties: {
    reserved: false // Windows plan for .NET
    zoneRedundant: true
  }
}

// Function App for Key Rotation
resource functionApp 'Microsoft.Web/sites@2023-01-01' = {
  name: functionAppName
  location: location
  tags: tags
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      use32BitWorkerProcess: false
      alwaysOn: true
      ftpsState: 'Disabled'
      http20Enabled: true
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsightsConnectionString
        }
        {
          name: 'ServiceBusConnection'
          value: serviceBusConnectionString
        }
        {
          name: 'KeyVaultUri'
          value: reference(keyVaultResourceId, '2023-02-01').vaultUri
        }
        // Rotation Policy Settings
        {
          name: 'RotationPolicy:SigningKeyDays'
          value: '90'
        }
        {
          name: 'RotationPolicy:EncryptionKeyDays'
          value: '365'
        }
        {
          name: 'RotationPolicy:TlsCertificateDays'
          value: '30'
        }
        {
          name: 'RotationPolicy:ApiKeyDays'
          value: '180'
        }
        {
          name: 'RotationPolicy:GracePeriodDays'
          value: '7'
        }
        {
          name: 'RotationPolicy:WarningDays'
          value: '14'
        }
      ]
    }
    httpsOnly: true
  }
}

// Key Vault access for Function App
module keyVaultAccess 'keyVaultAccessPolicy.bicep' = {
  name: '${functionAppName}-kv-access'
  params: {
    keyVaultName: split(keyVaultResourceId, '/')[8]
    principalId: functionApp.identity.principalId
    permissions: {
      certificates: ['get', 'list', 'create', 'update', 'import']
      keys: ['get', 'list', 'create', 'update', 'rotate', 'backup', 'restore']
      secrets: ['get', 'list', 'set']
    }
  }
}

// Service Bus Queue for rotation events
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' existing = {
  name: split(split(serviceBusConnectionString, ';')[0], '=')[1]
}

resource rotationQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: 'key-rotation-events'
  properties: {
    maxDeliveryCount: 3
    defaultMessageTimeToLive: 'P1D'
    deadLetteringOnMessageExpiration: true
    enablePartitioning: false
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT10M'
  }
}

resource notificationQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: 'rotation-notifications'
  properties: {
    maxDeliveryCount: 5
    defaultMessageTimeToLive: 'P7D'
  }
}

// Timer-triggered functions configuration (via ARM template until Bicep supports inline functions)
resource functionDeployment 'Microsoft.Resources/deployments@2021-04-01' = {
  name: '${functionAppName}-functions'
  properties: {
    mode: 'Incremental'
    template: {
      '$schema': 'https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#'
      contentVersion: '1.0.0.0'
      resources: []
      outputs: {
        functionsConfig: {
          type: 'object'
          value: {
            functions: [
              {
                name: 'DailyRotationCheck'
                schedule: '0 0 2 * * *' // Daily at 2 AM
                description: 'Check all keys for rotation requirements'
              }
              {
                name: 'EmergencyRotation'
                trigger: 'ServiceBusQueue'
                queue: 'key-rotation-events'
                description: 'Handle emergency rotation requests'
              }
              {
                name: 'GracePeriodMonitor'
                schedule: '0 */30 * * * *' // Every 30 minutes
                description: 'Monitor keys in grace period'
              }
              {
                name: 'ComplianceReporter'
                schedule: '0 0 0 1 * *' // Monthly on the 1st
                description: 'Generate compliance reports for key rotation'
              }
            ]
          }
        }
      }
    }
  }
}

// Outputs
output functionAppId string = functionApp.id
output functionAppName string = functionApp.name
output functionAppPrincipalId string = functionApp.identity.principalId
output rotationQueueName string = rotationQueue.name
output notificationQueueName string = notificationQueue.name

output deploymentInstructions string = '''
Key Rotation Function Deployment Instructions:

1. Deploy Function Code:
   cd src/NumbatWallet.Functions.KeyRotation
   dotnet publish -c Release
   func azure functionapp publish ${functionAppName}

2. Configure Rotation Policies:
   - Update app settings with specific rotation intervals
   - Configure notification recipients
   - Set compliance requirements

3. Initialize Rotation Schedule:
   - Run initial key inventory
   - Set baseline rotation dates
   - Configure grace periods

4. Test Rotation:
   - Trigger test rotation for non-production key
   - Verify grace period handling
   - Test rollback procedures

5. Enable Monitoring:
   - Configure alerts for rotation failures
   - Setup compliance dashboard
   - Enable audit logging

6. Document Procedures:
   - Create runbook for manual rotation
   - Document emergency procedures
   - Update compliance documentation

For assistance, contact platform-security@numbatwallet.gov.au
'''
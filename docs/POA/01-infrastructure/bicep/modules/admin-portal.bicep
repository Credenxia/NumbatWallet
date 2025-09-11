// Admin Portal Module - Blazor Server Hosting
// Version: 1.0
// Purpose: Deploy and configure Blazor Server admin portal

@description('Environment name')
@allowed(['dev', 'test', 'demo', 'prod'])
param environment string

@description('Location for resources')
param location string = resourceGroup().location

@description('Application name')
param appName string = 'numbat-wallet'

@description('App Service Plan ID')
param appServicePlanId string

@description('Key Vault name for configuration')
param keyVaultName string

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Virtual Network subnet ID for integration')
param subnetId string

@description('PostgreSQL connection string secret URI')
param dbConnectionStringUri string

@description('Container registry URL')
param containerRegistryUrl string = ''

@description('Container image tag')
param imageTag string = 'latest'

@description('Minimum instance count')
param minInstances int = environment == 'prod' ? 2 : 1

@description('Maximum instance count')
param maxInstances int = environment == 'prod' ? 10 : 3

@description('Tags to apply to resources')
param tags object = {}

// Variables
var adminPortalName = '${appName}-admin-${environment}'
var managedIdentityName = '${adminPortalName}-identity'

// User-assigned Managed Identity
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: managedIdentityName
  location: location
  tags: tags
}

// Blazor Server App Service
resource adminPortal 'Microsoft.Web/sites@2023-01-01' = {
  name: adminPortalName
  location: location
  kind: 'app,linux,container'
  tags: union(tags, {
    Environment: environment
    Component: 'AdminPortal'
    Framework: 'Blazor Server'
  })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlanId
    reserved: true
    httpsOnly: true
    clientAffinityEnabled: true // Required for SignalR
    virtualNetworkSubnetId: subnetId
    vnetRouteAllEnabled: true
    siteConfig: {
      linuxFxVersion: containerRegistryUrl != '' ? 'DOCKER|${containerRegistryUrl}/${appName}-admin:${imageTag}' : 'DOTNETCORE|9.0'
      alwaysOn: true
      http20Enabled: true
      minTlsVersion: '1.3'
      ftpsState: 'Disabled'
      webSocketsEnabled: true // Required for SignalR
      use32BitWorkerProcess: false
      loadBalancing: 'LeastRequests'
      autoHealEnabled: true
      autoHealRules: {
        triggers: {
          statusCodes: [
            {
              status: 500
              subStatus: 0
              count: 5
              timeInterval: '00:05:00'
            }
          ]
          slowRequests: {
            count: 5
            timeInterval: '00:05:00'
            time: '00:00:30'
          }
        }
        actions: {
          actionType: 'Recycle'
          minProcessExecutionTime: '00:05:00'
        }
      }
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environment == 'prod' ? 'Production' : 'Development'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'ApplicationInsights__ConnectionString'
          value: appInsightsConnectionString
        }
        {
          name: 'KeyVault__Uri'
          value: 'https://${keyVaultName}.vault.azure.net/'
        }
        {
          name: 'AzureAd__Instance'
          value: 'https://login.microsoftonline.com/'
        }
        {
          name: 'AzureAd__TenantId'
          value: '@Microsoft.KeyVault(SecretUri=https://${keyVaultName}.vault.azure.net/secrets/AzureAd--TenantId)'
        }
        {
          name: 'AzureAd__ClientId'
          value: '@Microsoft.KeyVault(SecretUri=https://${keyVaultName}.vault.azure.net/secrets/AzureAd--ClientId)'
        }
        {
          name: 'AzureAd__ClientSecret'
          value: '@Microsoft.KeyVault(SecretUri=https://${keyVaultName}.vault.azure.net/secrets/AzureAd--ClientSecret)'
        }
        {
          name: 'AzureAd__CallbackPath'
          value: '/signin-oidc'
        }
        {
          name: 'AzureAd__SignedOutCallbackPath'
          value: '/signout-callback-oidc'
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: '@Microsoft.KeyVault(SecretUri=${dbConnectionStringUri})'
        }
        {
          name: 'GraphQL__Endpoint'
          value: 'https://${appName}-api-${environment}.azurewebsites.net/graphql'
        }
        {
          name: 'SignalR__HubProtocol'
          value: 'json'
        }
        {
          name: 'SignalR__MaximumReceiveMessageSize'
          value: '102400'
        }
        {
          name: 'Session__IdleTimeout'
          value: '00:30:00'
        }
        {
          name: 'Session__Cookie__Name'
          value: '.NumbatWallet.AdminSession'
        }
        {
          name: 'Session__Cookie__HttpOnly'
          value: 'true'
        }
        {
          name: 'Session__Cookie__IsEssential'
          value: 'true'
        }
        {
          name: 'Session__Cookie__SameSite'
          value: 'Strict'
        }
        {
          name: 'Session__Cookie__SecurePolicy'
          value: 'Always'
        }
      ]
      connectionStrings: [
        {
          name: 'Redis'
          connectionString: '@Microsoft.KeyVault(SecretUri=https://${keyVaultName}.vault.azure.net/secrets/Redis--ConnectionString)'
          type: 'Custom'
        }
      ]
      ipSecurityRestrictions: environment == 'prod' ? [
        {
          ipAddress: 'AzureFrontDoor.Backend'
          action: 'Allow'
          priority: 100
          name: 'Allow Azure Front Door'
          description: 'Allow traffic from Azure Front Door'
          tag: 'ServiceTag'
        }
        {
          ipAddress: 'Any'
          action: 'Deny'
          priority: 2147483647
          name: 'Deny all'
          description: 'Deny all other traffic'
        }
      ] : []
      scmIpSecurityRestrictions: [
        {
          ipAddress: 'Any'
          action: 'Deny'
          priority: 2147483647
          name: 'Deny all'
          description: 'Deny all deployment access'
        }
      ]
    }
  }
}

// Auto-scaling rules
resource autoScale 'Microsoft.Insights/autoscalesettings@2022-10-01' = {
  name: '${adminPortalName}-autoscale'
  location: location
  tags: tags
  properties: {
    targetResourceUri: adminPortal.id
    enabled: true
    profiles: [
      {
        name: 'Default'
        capacity: {
          minimum: string(minInstances)
          maximum: string(maxInstances)
          default: string(minInstances)
        }
        rules: [
          {
            metricTrigger: {
              metricName: 'CpuPercentage'
              metricResourceUri: appServicePlanId
              operator: 'GreaterThan'
              statistic: 'Average'
              threshold: 70
              timeAggregation: 'Average'
              timeGrain: 'PT1M'
              timeWindow: 'PT5M'
            }
            scaleAction: {
              direction: 'Increase'
              type: 'ChangeCount'
              value: '1'
              cooldown: 'PT5M'
            }
          }
          {
            metricTrigger: {
              metricName: 'CpuPercentage'
              metricResourceUri: appServicePlanId
              operator: 'LessThan'
              statistic: 'Average'
              threshold: 30
              timeAggregation: 'Average'
              timeGrain: 'PT1M'
              timeWindow: 'PT10M'
            }
            scaleAction: {
              direction: 'Decrease'
              type: 'ChangeCount'
              value: '1'
              cooldown: 'PT5M'
            }
          }
          {
            metricTrigger: {
              metricName: 'MemoryPercentage'
              metricResourceUri: appServicePlanId
              operator: 'GreaterThan'
              statistic: 'Average'
              threshold: 85
              timeAggregation: 'Average'
              timeGrain: 'PT1M'
              timeWindow: 'PT5M'
            }
            scaleAction: {
              direction: 'Increase'
              type: 'ChangeCount'
              value: '1'
              cooldown: 'PT5M'
            }
          }
        ]
      }
    ]
  }
}

// Diagnostic settings
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${adminPortalName}-diagnostics'
  scope: adminPortal
  properties: {
    workspaceId: resourceId('Microsoft.OperationalInsights/workspaces', '${appName}-logs-${environment}')
    logs: [
      {
        category: 'AppServiceHTTPLogs'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: environment == 'prod' ? 30 : 7
        }
      }
      {
        category: 'AppServiceConsoleLogs'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: environment == 'prod' ? 30 : 7
        }
      }
      {
        category: 'AppServiceAppLogs'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: environment == 'prod' ? 30 : 7
        }
      }
      {
        category: 'AppServiceAuditLogs'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: environment == 'prod' ? 90 : 30
        }
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: environment == 'prod' ? 30 : 7
        }
      }
    ]
  }
}

// Outputs
output adminPortalUrl string = 'https://${adminPortal.properties.defaultHostName}'
output adminPortalName string = adminPortal.name
output adminPortalId string = adminPortal.id
output managedIdentityId string = managedIdentity.id
output managedIdentityPrincipalId string = managedIdentity.properties.principalId
output managedIdentityClientId string = managedIdentity.properties.clientId

output blazorConfiguration object = {
  SignalR: {
    RequireAuthentication: true
    EnableDetailedErrors: environment != 'prod'
    KeepAliveInterval: '00:00:15'
    ClientTimeoutInterval: '00:00:30'
    HandshakeTimeout: '00:00:15'
    MaximumParallelInvocationsPerClient: 1
  }
  MudBlazor: {
    SnackbarConfiguration: {
      PositionClass: 'Defaults.Classes.Position.TopRight'
      PreventDuplicates: true
      MaxDisplayedSnackbars: 3
      VisibleStateDuration: 3000
    }
    DefaultTheme: {
      Palette: {
        Primary: '#003366'
        Secondary: '#FDB813'
        AppbarBackground: '#003366'
      }
    }
  }
}
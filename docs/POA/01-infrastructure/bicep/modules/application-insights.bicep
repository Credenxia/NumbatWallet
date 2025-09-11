// Application Insights Module
// Version: 1.0
// Purpose: Configure Application Insights for logging and monitoring

@description('Environment name')
@allowed(['dev', 'test', 'demo', 'prod'])
param environment string

@description('Location for resources')
param location string = resourceGroup().location

@description('Application name')
param appName string = 'numbat-wallet'

@description('Log Analytics Workspace ID')
param workspaceId string

@description('Key Vault name for storing connection strings')
param keyVaultName string

@description('Enable public network access')
param enablePublicAccess bool = environment == 'dev'

@description('Daily data cap in GB')
param dailyDataCapGb int = environment == 'prod' ? 100 : 10

@description('Retention in days')
param retentionInDays int = environment == 'prod' ? 90 : 30

@description('Tags to apply to resources')
param tags object = {}

// Variables
var appInsightsName = '${appName}-insights-${environment}'
var actionGroupName = '${appName}-alerts-${environment}'

// Application Insights
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  tags: union(tags, {
    Environment: environment
    Component: 'Monitoring'
  })
  properties: {
    Application_Type: 'web'
    RetentionInDays: retentionInDays
    WorkspaceResourceId: workspaceId
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: enablePublicAccess ? 'Enabled' : 'Disabled'
    publicNetworkAccessForQuery: 'Enabled' // Always allow queries
    Flow_Type: 'Bluefield'
    Request_Source: 'rest'
  }
}

// Data Cap
resource dataVolumeCap 'Microsoft.Insights/components/pricingPlans@2017-10-01' = {
  name: 'current'
  parent: appInsights
  properties: {
    cap: dailyDataCapGb
    warningThreshold: 90
    stopSendNotificationWhenHitThreshold: false
  }
}

// Action Group for Alerts
resource actionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: actionGroupName
  location: 'global'
  tags: tags
  properties: {
    groupShortName: substring('${appName}${environment}', 0, min(12, length('${appName}${environment}')))
    enabled: true
    emailReceivers: [
      {
        name: 'DevOpsTeam'
        emailAddress: 'devops@${appName}.wa.gov.au'
        useCommonAlertSchema: true
      }
    ]
    smsReceivers: environment == 'prod' ? [
      {
        name: 'OnCallEngineer'
        countryCode: '61'
        phoneNumber: '0400000000' // Update with actual number
      }
    ] : []
    webhookReceivers: [
      {
        name: 'TeamsWebhook'
        serviceUri: 'https://outlook.office.com/webhook/...' // Update with actual webhook
        useCommonAlertSchema: true
      }
    ]
  }
}

// Smart Detection Alert Rules
resource smartDetection 'Microsoft.Insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = [for config in [
  'degradationindependencyduration'
  'degradationinserverresponsetime'
  'digestMailConfiguration'
  'dysfunction'
  'exceptionvolume'
  'failureanomalies'
  'memoryleak'
  'migrationToAlertRulesCompleted'
  'requestvolume'
  'securityextensions'
  'slowpageloadtime'
  'slowserverresponsetime'
  'traceseverity'
]: {
  name: config
  parent: appInsights
  properties: {
    RuleDefinitions: {
      Name: config
      DisplayName: config
      Description: 'Smart Detection for ${config}'
      HelpUrl: 'https://docs.microsoft.com/azure/azure-monitor/app/proactive-diagnostics'
      IsHidden: false
      IsEnabledByDefault: true
      IsInPreview: false
    }
    Enabled: true
    SendEmailsToSubscriptionOwners: false
    CustomEmails: ['devops@${appName}.wa.gov.au']
  }
}]

// Metric Alerts
resource highErrorRateAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${appName}-high-error-rate-${environment}'
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when error rate exceeds threshold'
    severity: 2
    enabled: true
    scopes: [
      appInsights.id
    ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT5M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'HighErrorRate'
          metricNamespace: 'microsoft.insights/components'
          metricName: 'requests/failed'
          operator: 'GreaterThan'
          threshold: environment == 'prod' ? 10 : 50
          timeAggregation: 'Count'
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
    actions: [
      {
        actionGroupId: actionGroup.id
      }
    ]
    autoMitigate: true
  }
}

resource slowResponseAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${appName}-slow-response-${environment}'
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when response time exceeds threshold'
    severity: 3
    enabled: true
    scopes: [
      appInsights.id
    ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'SlowResponse'
          metricNamespace: 'microsoft.insights/components'
          metricName: 'requests/duration'
          operator: 'GreaterThan'
          threshold: environment == 'prod' ? 1000 : 2000 // milliseconds
          timeAggregation: 'Average'
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
    actions: [
      {
        actionGroupId: actionGroup.id
      }
    ]
    autoMitigate: true
  }
}

// Log Analytics Queries for Dashboards
var dashboardQueries = [
  {
    name: 'RequestOverview'
    query: '''
      requests
      | where timestamp > ago(1h)
      | summarize 
          TotalRequests = count(),
          SuccessRate = countif(success == true) * 100.0 / count(),
          AvgDuration = avg(duration),
          P95Duration = percentile(duration, 95)
      | extend SuccessRate = round(SuccessRate, 2)
    '''
  }
  {
    name: 'TopEndpoints'
    query: '''
      requests
      | where timestamp > ago(1h)
      | summarize RequestCount = count() by name
      | order by RequestCount desc
      | take 10
    '''
  }
  {
    name: 'ErrorDistribution'
    query: '''
      exceptions
      | where timestamp > ago(1h)
      | summarize ErrorCount = count() by type, outerMessage
      | order by ErrorCount desc
      | take 10
    '''
  }
  {
    name: 'CredentialOperations'
    query: '''
      customEvents
      | where timestamp > ago(1h)
      | where name in ("CredentialIssued", "CredentialVerified", "CredentialRevoked")
      | summarize count() by name, bin(timestamp, 5m)
      | render timechart
    '''
  }
  {
    name: 'UserActivity'
    query: '''
      customEvents
      | where timestamp > ago(1h)
      | where name == "UserAuthenticated"
      | summarize UniqueUsers = dcount(tostring(customDimensions.UserId))
    '''
  }
  {
    name: 'AuditLogs'
    query: '''
      customEvents
      | where timestamp > ago(24h)
      | where name == "AuditLog"
      | project 
          timestamp,
          Action = tostring(customDimensions.Action),
          Entity = tostring(customDimensions.Entity),
          EntityId = tostring(customDimensions.EntityId),
          UserId = tostring(customDimensions.UserId),
          Result = tostring(customDimensions.Result)
      | order by timestamp desc
      | take 100
    '''
  }
]

// Store connection string in Key Vault
module appInsightsSecret '../key-vault-secret.bicep' = {
  name: 'appInsightsSecret'
  params: {
    keyVaultName: keyVaultName
    secretName: 'ApplicationInsights--ConnectionString'
    secretValue: appInsights.properties.ConnectionString
  }
}

module appInsightsKey '../key-vault-secret.bicep' = {
  name: 'appInsightsKey'
  params: {
    keyVaultName: keyVaultName
    secretName: 'ApplicationInsights--InstrumentationKey'
    secretValue: appInsights.properties.InstrumentationKey
  }
}

// Outputs
output appInsightsId string = appInsights.id
output appInsightsName string = appInsights.name
output connectionString string = appInsights.properties.ConnectionString
output instrumentationKey string = appInsights.properties.InstrumentationKey
output actionGroupId string = actionGroup.id

output configuration object = {
  ApplicationInsights: {
    ConnectionString: '@Microsoft.KeyVault(SecretUri=https://${keyVaultName}.vault.azure.net/secrets/ApplicationInsights--ConnectionString)'
    InstrumentationKey: '@Microsoft.KeyVault(SecretUri=https://${keyVaultName}.vault.azure.net/secrets/ApplicationInsights--InstrumentationKey)'
    EnableAdaptiveSampling: true
    EnablePerformanceCounterCollectionModule: true
    EnableEventCounterCollectionModule: true
    EnableDependencyTrackingTelemetryModule: true
    EnableRequestTrackingTelemetryModule: true
    CloudRoleName: '${appName}-${environment}'
  }
  Serilog: {
    WriteTo: [
      {
        Name: 'ApplicationInsights'
        Args: {
          connectionString: '@Microsoft.KeyVault(SecretUri=https://${keyVaultName}.vault.azure.net/secrets/ApplicationInsights--ConnectionString)'
          restrictedToMinimumLevel: 'Information'
          telemetryConverter: 'Serilog.Sinks.ApplicationInsights.TelemetryConverters.TraceTelemetryConverter, Serilog.Sinks.ApplicationInsights'
        }
      }
    ]
  }
}

output kustoQueries array = dashboardQueries
// Azure Application Insights Module for NumbatWallet
// Purpose: Application performance monitoring and diagnostics

@description('Application Insights name')
@minLength(1)
@maxLength(255)
param appInsightsName string

@description('Location for Application Insights')
param location string = resourceGroup().location

@description('Application type')
@allowed([
  'web'
  'other'
])
param applicationType string = 'web'

@description('Application Insights pricing plan')
@allowed([
  'Basic'
  'Application Insights Enterprise'
])
param pricingPlan string = 'Basic'

@description('Daily cap in GB')
param dailyDataCapInGB int = 100

@description('Daily cap warning threshold percentage')
@minValue(0)
@maxValue(100)
param dailyDataCapWarningThreshold int = 90

@description('Retention in days')
@allowed([
  30
  60
  90
  120
  180
  270
  365
  550
  730
])
param retentionInDays int = 90

@description('Enable public network access for ingestion')
param publicNetworkAccessForIngestion string = 'Enabled'

@description('Enable public network access for query')
param publicNetworkAccessForQuery string = 'Enabled'

@description('Disable IP masking for better geo-location data')
param disableIpMasking bool = false

@description('Disable local authentication')
param disableLocalAuth bool = false

@description('Immediate purge data on 30 days')
param immediatePurgeDataOn30Days bool = false

@description('Log Analytics workspace ID')
param logAnalyticsWorkspaceId string

@description('Sampling percentage (0-100)')
@minValue(0)
@maxValue(100)
param samplingPercentage int = 100

@description('Tags to apply to Application Insights')
param tags object = {
  Component: 'Monitoring'
  Purpose: 'APM'
}

// Application Insights Component
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: applicationType
  properties: {
    Application_Type: applicationType
    Flow_Type: 'Bluefield'
    Request_Source: 'rest'
    RetentionInDays: retentionInDays
    WorkspaceResourceId: logAnalyticsWorkspaceId
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: publicNetworkAccessForIngestion
    publicNetworkAccessForQuery: publicNetworkAccessForQuery
    DisableIpMasking: disableIpMasking
    DisableLocalAuth: disableLocalAuth
    ImmediatePurgeDataOn30Days: immediatePurgeDataOn30Days
    SamplingPercentage: samplingPercentage
  }
}

// Pricing Plan Configuration
resource pricingPlanConfig 'microsoft.insights/components/pricingPlans@2017-10-01' = {
  parent: appInsights
  name: 'current'
  properties: {
    plan: pricingPlan
    cap: dailyDataCapInGB
    warningThreshold: dailyDataCapWarningThreshold
    stopSendNotificationWhenHitCap: false
    stopSendNotificationWhenHitThreshold: false
  }
}

// Smart Detection Rules
resource smartDetectionRules 'Microsoft.Insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = [for rule in smartDetectionRulesList: {
  parent: appInsights
  name: rule.name
  properties: {
    enabled: rule.enabled
    sendEmailsToSubscriptionOwners: false
    customEmails: []
  }
}]

var smartDetectionRulesList = [
  {
    name: 'slowpageloadtime'
    enabled: true
  }
  {
    name: 'slowserverresponsetime'
    enabled: true
  }
  {
    name: 'longdependencyduration'
    enabled: true
  }
  {
    name: 'degradationinserverresponsetime'
    enabled: true
  }
  {
    name: 'degradationindependencyduration'
    enabled: true
  }
  {
    name: 'extension_traceseveritydetector'
    enabled: true
  }
  {
    name: 'extension_exceptionchangeextension'
    enabled: true
  }
  {
    name: 'extension_memoryleakextension'
    enabled: true
  }
  {
    name: 'extension_securityextensionspackage'
    enabled: true
  }
]

// Web Tests (Availability Tests)
resource availabilityTests 'Microsoft.Insights/webtests@2022-06-15' = [for test in availabilityTestsList: {
  name: '${appInsightsName}-${test.name}'
  location: location
  tags: union(tags, {
    'hidden-link:${appInsights.id}': 'Resource'
  })
  properties: {
    SyntheticMonitorId: '${appInsightsName}-${test.name}'
    Name: test.displayName
    Description: test.description
    Enabled: test.enabled
    Frequency: test.frequencyInSeconds
    Timeout: test.timeoutInSeconds
    RetryEnabled: true
    Locations: test.locations
    Configuration: {
      WebTest: test.webTest
    }
  }
}]

var availabilityTestsList = [
  {
    name: 'health-check'
    displayName: 'Health Check Endpoint'
    description: 'Monitors the health check endpoint'
    enabled: true
    frequencyInSeconds: 300
    timeoutInSeconds: 30
    locations: [
      {
        Id: 'us-tx-sn1-azr'
      }
      {
        Id: 'apac-hk-hkn-azr'
      }
      {
        Id: 'emea-au-syd-edge'
      }
    ]
    webTest: '''
      <WebTest Name="Health Check" Id="00000000-0000-0000-0000-000000000001" Enabled="True" CssProjectStructure="" CssIteration="" Timeout="30" WorkItemIds="" xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
        <Items>
          <Request Method="GET" Guid="00000000-0000-0000-0000-000000000002" Version="1.1" Url="https://api.numbatwallet.wa.gov.au/health" ThinkTime="0" Timeout="30" ParseDependentRequests="False" FollowRedirects="True" RecordResult="True" Cache="False" ResponseTimeGoal="0" Encoding="utf-8" ExpectedHttpStatusCode="200" ExpectedResponseUrl="" ReportingName="" IgnoreHttpStatusCode="False" />
        </Items>
      </WebTest>
    '''
  }
  {
    name: 'api-availability'
    displayName: 'API Availability'
    description: 'Monitors API availability'
    enabled: true
    frequencyInSeconds: 600
    timeoutInSeconds: 30
    locations: [
      {
        Id: 'emea-au-syd-edge'
      }
      {
        Id: 'apac-sg-sin-azr'
      }
    ]
    webTest: '''
      <WebTest Name="API Availability" Id="00000000-0000-0000-0000-000000000003" Enabled="True" CssProjectStructure="" CssIteration="" Timeout="30" WorkItemIds="" xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
        <Items>
          <Request Method="GET" Guid="00000000-0000-0000-0000-000000000004" Version="1.1" Url="https://api.numbatwallet.wa.gov.au/api/v1/status" ThinkTime="0" Timeout="30" ParseDependentRequests="False" FollowRedirects="True" RecordResult="True" Cache="False" ResponseTimeGoal="0" Encoding="utf-8" ExpectedHttpStatusCode="200" ExpectedResponseUrl="" ReportingName="" IgnoreHttpStatusCode="False" />
        </Items>
      </WebTest>
    '''
  }
]

// Workbook Templates
resource workbookTemplates 'Microsoft.Insights/workbooktemplates@2020-11-20' = [for template in workbookTemplatesList: {
  name: guid(appInsights.id, template.name)
  location: location
  tags: tags
  properties: {
    priority: template.priority
    author: 'NumbatWallet Team'
    templateData: {
      version: 'Notebook/1.0'
      items: template.items
      fallbackResourceIds: [
        appInsights.id
      ]
      fromTemplateId: 'sentinel-UserWorkbook'
    }
    galleries: [
      {
        name: template.displayName
        category: 'NumbatWallet Monitoring'
        type: 'workbook'
        order: template.priority
      }
    ]
  }
}]

var workbookTemplatesList = [
  {
    name: 'performance-overview'
    displayName: 'Performance Overview'
    priority: 1
    items: []
  }
  {
    name: 'error-analysis'
    displayName: 'Error Analysis'
    priority: 2
    items: []
  }
  {
    name: 'user-analytics'
    displayName: 'User Analytics'
    priority: 3
    items: []
  }
]

// Continuous Export Configuration (if needed)
resource continuousExport 'Microsoft.Insights/components/exportConfiguration@2020-02-02' = if (false) {
  parent: appInsights
  name: 'continuous-export'
  properties: {
    ExportStatus: 'Enabled'
    DestinationType: 'Blob'
    IsEnabled: true
    RecordTypes: 'Requests, Event, Exceptions, Metrics, PageViews, PageViewPerformance, Rdd, PerformanceCounters, Availability'
    StorageAccountId: '/subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.Storage/storageAccounts/stnumbatwallet'
    ContainerName: 'app-insights-export'
  }
}

// API Key for programmatic access
resource apiKey 'Microsoft.Insights/components/apikeys@2020-02-02' = {
  parent: appInsights
  name: 'numbatwallet-api-key'
  properties: {
    name: 'NumbatWallet API Access'
    linkedReadProperties: [
      '${appInsights.id}/api'
    ]
    linkedWriteProperties: []
  }
}

// Outputs
output appInsightsId string = appInsights.id
output appInsightsName string = appInsights.name
output instrumentationKey string = appInsights.properties.InstrumentationKey
output connectionString string = appInsights.properties.ConnectionString
output appInsightsResourceId string = appInsights.id
output apiKey string = apiKey.properties.apiKey
// Azure Log Analytics Workspace Module for NumbatWallet
// Purpose: Centralized logging and monitoring for all services

@description('Log Analytics workspace name')
@minLength(4)
@maxLength(63)
param workspaceName string

@description('Location for the workspace')
param location string = resourceGroup().location

@description('Workspace SKU/pricing tier')
@allowed([
  'CapacityReservation'
  'Free'
  'LACluster'
  'PerGB2018'
  'PerNode'
  'Premium'
  'Standalone'
  'Standard'
])
param sku string = 'PerGB2018'

@description('Workspace data retention in days')
@minValue(7)
@maxValue(730)
param retentionInDays int = 30

@description('Daily quota cap in GB (-1 for unlimited)')
param dailyQuotaGb int = -1

@description('Enable public network access')
param publicNetworkAccessForIngestion string = 'Enabled'

@description('Enable public network access for query')
param publicNetworkAccessForQuery string = 'Enabled'

@description('Tags to apply to the workspace')
param tags object = {
  Component: 'Monitoring'
  Purpose: 'Centralized Logging'
}

// Log Analytics Workspace
resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: workspaceName
  location: location
  tags: tags
  properties: {
    sku: {
      name: sku
    }
    retentionInDays: retentionInDays
    workspaceCapping: dailyQuotaGb != -1 ? {
      dailyQuotaGb: dailyQuotaGb
    } : null
    publicNetworkAccessForIngestion: publicNetworkAccessForIngestion
    publicNetworkAccessForQuery: publicNetworkAccessForQuery
    features: {
      enableDataExport: true
      immediatePurgeDataOn30Days: false
      enableLogAccessUsingOnlyResourcePermissions: true
      disableLocalAuth: false
    }
  }
}

// Data Sources Configuration
resource performanceCounters 'Microsoft.OperationalInsights/workspaces/dataSources@2020-08-01' = [for counter in performanceCounterList: {
  parent: workspace
  name: counter.name
  kind: 'WindowsPerformanceCounter'
  properties: {
    objectName: counter.objectName
    instanceName: counter.instanceName
    intervalSeconds: counter.intervalSeconds
    counterName: counter.counterName
  }
}]

var performanceCounterList = [
  {
    name: 'CPU'
    objectName: 'Processor'
    instanceName: '_Total'
    intervalSeconds: 60
    counterName: '% Processor Time'
  }
  {
    name: 'Memory'
    objectName: 'Memory'
    instanceName: '*'
    intervalSeconds: 60
    counterName: 'Available MBytes'
  }
  {
    name: 'Disk'
    objectName: 'LogicalDisk'
    instanceName: '*'
    intervalSeconds: 60
    counterName: '% Free Space'
  }
  {
    name: 'Network'
    objectName: 'Network Interface'
    instanceName: '*'
    intervalSeconds: 60
    counterName: 'Bytes Total/sec'
  }
]

// Saved Searches / Queries
resource savedSearches 'Microsoft.OperationalInsights/workspaces/savedSearches@2020-08-01' = [for search in savedSearchList: {
  parent: workspace
  name: guid(workspace.id, search.displayName)
  properties: {
    etag: '*'
    displayName: search.displayName
    category: search.category
    query: search.query
    version: 2
  }
}]

var savedSearchList = [
  {
    displayName: 'All Errors and Exceptions'
    category: 'NumbatWallet'
    query: '''
      union AppExceptions, AppTraces
      | where severityLevel >= 3
      | project timestamp, message, severityLevel, operation_Name, cloud_RoleName
      | order by timestamp desc
    '''
  }
  {
    displayName: 'Failed Authentication Attempts'
    category: 'Security'
    query: '''
      AppTraces
      | where message contains "Authentication failed" or message contains "Login failed"
      | project timestamp, message, operation_Name, user_Id
      | order by timestamp desc
    '''
  }
  {
    displayName: 'Slow Requests (>1s)'
    category: 'Performance'
    query: '''
      AppRequests
      | where duration > 1000
      | project timestamp, name, duration, resultCode, operation_Name
      | order by duration desc
    '''
  }
  {
    displayName: 'Database Query Performance'
    category: 'Performance'
    query: '''
      AppDependencies
      | where type == "SQL"
      | summarize avg(duration), percentile(duration, 95), percentile(duration, 99) by name
      | order by avg_duration desc
    '''
  }
  {
    displayName: 'Credential Operations'
    category: 'NumbatWallet'
    query: '''
      AppTraces
      | where message contains "credential" or message contains "vc" or message contains "verifiable"
      | project timestamp, message, operation_Name, user_Id
      | order by timestamp desc
    '''
  }
]

// Alert Rules (using Scheduled Query Rules)
resource alertRules 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = [for alert in alertRulesList: {
  name: alert.name
  location: location
  tags: tags
  properties: {
    displayName: alert.displayName
    description: alert.description
    severity: alert.severity
    enabled: true
    evaluationFrequency: alert.evaluationFrequency
    windowSize: alert.windowSize
    criteria: {
      allOf: [
        {
          query: alert.query
          timeAggregation: alert.timeAggregation
          metricMeasureColumn: alert.metricMeasureColumn
          operator: alert.operator
          threshold: alert.threshold
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    scopes: [
      workspace.id
    ]
    targetResourceTypes: [
      'Microsoft.OperationalInsights/workspaces'
    ]
    actions: {
      actionGroups: []
      customProperties: {}
    }
  }
}]

var alertRulesList = [
  {
    name: 'high-error-rate'
    displayName: 'High Error Rate'
    description: 'Alert when error rate exceeds 5% in 5 minutes'
    severity: 2
    evaluationFrequency: 'PT5M'
    windowSize: 'PT5M'
    query: '''
      AppRequests
      | summarize errorRate = countif(resultCode >= 400) * 100.0 / count() by bin(timestamp, 5m)
      | where errorRate > 5
    '''
    timeAggregation: 'Average'
    metricMeasureColumn: 'errorRate'
    operator: 'GreaterThan'
    threshold: 5
  }
  {
    name: 'authentication-failures'
    displayName: 'Multiple Authentication Failures'
    description: 'Alert on multiple authentication failures from same IP'
    severity: 1
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    query: '''
      AppTraces
      | where message contains "Authentication failed"
      | summarize failureCount = count() by client_IP
      | where failureCount > 10
    '''
    timeAggregation: 'Count'
    metricMeasureColumn: 'failureCount'
    operator: 'GreaterThan'
    threshold: 10
  }
]

// Solutions to install
resource solutions 'Microsoft.OperationsManagement/solutions@2015-11-01-preview' = [for solution in solutionsList: {
  name: solution.name
  location: location
  tags: tags
  plan: {
    name: solution.name
    promotionCode: ''
    product: solution.product
    publisher: 'Microsoft'
  }
  properties: {
    workspaceResourceId: workspace.id
    containedResources: []
  }
}]

var solutionsList = [
  {
    name: 'AzureActivity(${workspaceName})'
    product: 'OMSGallery/AzureActivity'
  }
  {
    name: 'Security(${workspaceName})'
    product: 'OMSGallery/Security'
  }
  {
    name: 'KeyVaultAnalytics(${workspaceName})'
    product: 'OMSGallery/KeyVaultAnalytics'
  }
  {
    name: 'ContainerInsights(${workspaceName})'
    product: 'OMSGallery/ContainerInsights'
  }
  {
    name: 'SQLAssessment(${workspaceName})'
    product: 'OMSGallery/SQLAssessment'
  }
]

// Outputs
output workspaceId string = workspace.id
output workspaceName string = workspace.name
output workspaceResourceId string = workspace.id
output workspaceCustomerId string = workspace.properties.customerId
output workspacePrimarySharedKey string = workspace.listKeys().primarySharedKey
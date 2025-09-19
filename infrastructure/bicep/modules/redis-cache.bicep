// Azure Redis Cache Module for NumbatWallet
// Purpose: Session management, caching, and rate limiting

@description('Redis Cache name')
@minLength(1)
@maxLength(63)
param redisCacheName string

@description('Location for Redis Cache')
param location string = resourceGroup().location

@description('Redis Cache SKU name')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param skuName string = 'Standard'

@description('Redis Cache SKU family')
@allowed([
  'C'
  'P'
])
param skuFamily string = 'C'

@description('Redis Cache SKU capacity')
@allowed([
  0
  1
  2
  3
  4
  5
  6
])
param skuCapacity int = 1

@description('Redis version')
@allowed([
  '4'
  '6'
])
param redisVersion string = '6'

@description('Enable non-SSL port (6379)')
param enableNonSslPort bool = false

@description('Minimum TLS version')
@allowed([
  '1.0'
  '1.1'
  '1.2'
])
param minimumTlsVersion string = '1.2'

@description('Enable public network access')
param publicNetworkAccess string = 'Enabled'

@description('Redis configuration settings')
param redisConfiguration object = {
  'maxmemory-policy': 'allkeys-lru'
  'maxmemory-reserved': '50'
  'maxfragmentationmemory-reserved': '50'
  'maxclients': '1000'
  'timeout': '300'
  'tcp-keepalive': '60'
  'rdb-backup-enabled': skuName == 'Premium' ? 'true' : 'false'
  'rdb-backup-frequency': skuName == 'Premium' ? '60' : null
  'rdb-backup-max-snapshot-count': skuName == 'Premium' ? '1' : null
  'aof-backup-enabled': 'false'
}

@description('Static IP address (Premium tier only)')
param staticIP string = ''

@description('Subnet ID for Redis (Premium tier only)')
param subnetId string = ''

@description('Number of shards for clustering (Premium tier only)')
@minValue(0)
@maxValue(10)
param shardCount int = 0

@description('Number of replicas per master (Premium tier only)')
@minValue(0)
@maxValue(3)
param replicasPerMaster int = 0

@description('Availability zones for Premium tier')
param zones array = []

@description('Enable private endpoint')
param enablePrivateEndpoint bool = true

@description('Private endpoint subnet ID')
param privateEndpointSubnetId string = ''

@description('Firewall rules - allowed IP addresses')
param firewallRules array = []

@description('Patch schedule for Redis updates')
param patchSchedule object = {
  dayOfWeek: 'Sunday'
  startHourUtc: 2
  maintenanceWindow: 'PT5H'
}

@description('Log Analytics workspace ID for diagnostics')
param logAnalyticsWorkspaceId string = ''

@description('Tags to apply to Redis Cache')
param tags object = {
  Component: 'Cache'
  Purpose: 'Session Management'
}

// Redis Cache Resource
resource redisCache 'Microsoft.Cache/redis@2023-08-01' = {
  name: redisCacheName
  location: location
  tags: tags
  zones: skuName == 'Premium' && length(zones) > 0 ? zones : null
  properties: {
    sku: {
      name: skuName
      family: skuFamily
      capacity: skuCapacity
    }
    redisVersion: redisVersion
    enableNonSslPort: enableNonSslPort
    minimumTlsVersion: minimumTlsVersion
    publicNetworkAccess: publicNetworkAccess
    redisConfiguration: redisConfiguration

    // Premium tier specific properties
    staticIP: skuName == 'Premium' && !empty(staticIP) ? staticIP : null
    subnetId: skuName == 'Premium' && !empty(subnetId) ? subnetId : null
    shardCount: skuName == 'Premium' && shardCount > 0 ? shardCount : null
    replicasPerMaster: skuName == 'Premium' && replicasPerMaster > 0 ? replicasPerMaster : null
  }
}

// Firewall Rules
resource firewallRule 'Microsoft.Cache/redis/firewallRules@2023-08-01' = [for rule in firewallRules: {
  parent: redisCache
  name: rule.name
  properties: {
    startIP: rule.startIP
    endIP: rule.endIP
  }
}]

// Patch Schedule
resource patchScheduleResource 'Microsoft.Cache/redis/patchSchedules@2023-08-01' = if (!empty(patchSchedule)) {
  parent: redisCache
  name: 'default'
  properties: {
    scheduleEntries: [
      {
        dayOfWeek: patchSchedule.dayOfWeek
        startHourUtc: patchSchedule.startHourUtc
        maintenanceWindow: patchSchedule.maintenanceWindow
      }
    ]
  }
}

// Linked Servers for Geo-replication (Premium only)
resource linkedServer 'Microsoft.Cache/redis/linkedServers@2023-08-01' = if (false) {
  parent: redisCache
  name: 'secondary-region'
  properties: {
    linkedRedisCacheId: '/subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.Cache/Redis/redis-secondary'
    linkedRedisCacheLocation: 'australiasoutheast'
    serverRole: 'Secondary'
  }
}

// Private Endpoint for Redis Cache
resource privateEndpoint 'Microsoft.Network/privateEndpoints@2023-06-01' = if (enablePrivateEndpoint && !empty(privateEndpointSubnetId)) {
  name: '${redisCacheName}-pe'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${redisCacheName}-pe-connection'
        properties: {
          privateLinkServiceId: redisCache.id
          groupIds: ['redisCache']
        }
      }
    ]
  }
}

// Private DNS Zone Group for Redis
resource privateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-06-01' = if (enablePrivateEndpoint && !empty(privateEndpointSubnetId)) {
  parent: privateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'privatelink-redis-cache-windows-net'
        properties: {
          privateDnsZoneId: '/subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.Network/privateDnsZones/privatelink.redis.cache.windows.net'
        }
      }
    ]
  }
}

// Diagnostic Settings
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (!empty(logAnalyticsWorkspaceId)) {
  name: 'diag-${redisCacheName}'
  scope: redisCache
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

// Alert Rules for Redis Cache
resource alertRules 'Microsoft.Insights/metricAlerts@2018-03-01' = [for alert in alertRulesList: {
  name: '${redisCacheName}-${alert.name}'
  location: 'global'
  tags: tags
  properties: {
    description: alert.description
    severity: alert.severity
    enabled: true
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    targetResourceType: 'Microsoft.Cache/redis'
    targetResourceRegion: location
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: alert.name
          metricName: alert.metricName
          operator: alert.operator
          threshold: alert.threshold
          timeAggregation: alert.timeAggregation
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
    scopes: [
      redisCache.id
    ]
    actions: []
  }
}]

var alertRulesList = [
  {
    name: 'high-memory-usage'
    description: 'Alert when Redis memory usage exceeds 90%'
    severity: 2
    metricName: 'usedmemorypercentage'
    operator: 'GreaterThan'
    threshold: 90
    timeAggregation: 'Average'
  }
  {
    name: 'high-cpu-usage'
    description: 'Alert when Redis CPU usage exceeds 80%'
    severity: 2
    metricName: 'percentProcessorTime'
    operator: 'GreaterThan'
    threshold: 80
    timeAggregation: 'Average'
  }
  {
    name: 'high-server-load'
    description: 'Alert when Redis server load is high'
    severity: 2
    metricName: 'serverLoad'
    operator: 'GreaterThan'
    threshold: 80
    timeAggregation: 'Average'
  }
  {
    name: 'connection-errors'
    description: 'Alert on Redis connection errors'
    severity: 1
    metricName: 'connectedclients'
    operator: 'LessThan'
    threshold: 1
    timeAggregation: 'Minimum'
  }
]

// Outputs
output redisCacheId string = redisCache.id
output redisCacheName string = redisCache.name
output hostName string = redisCache.properties.hostName
output sslPort int = redisCache.properties.sslPort
output nonSslPort int = enableNonSslPort ? redisCache.properties.port : 0
output primaryKey string = redisCache.listKeys().primaryKey
output secondaryKey string = redisCache.listKeys().secondaryKey
output connectionString string = '${redisCache.properties.hostName}:${redisCache.properties.sslPort},password=${redisCache.listKeys().primaryKey},ssl=True,abortConnect=False'
output privateEndpointId string = enablePrivateEndpoint && !empty(privateEndpointSubnetId) ? privateEndpoint.id : ''
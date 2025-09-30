// Azure Database for PostgreSQL Flexible Server Module for NumbatWallet
// Purpose: Primary database for wallets, credentials, and person data

@description('PostgreSQL server name')
@minLength(3)
@maxLength(63)
param serverName string

@description('Location for PostgreSQL server')
param location string = resourceGroup().location

@description('Administrator login name')
@minLength(1)
param administratorLogin string

@description('Administrator password')
@minLength(8)
@secure()
param administratorLoginPassword string

@description('PostgreSQL version')
@allowed([
  '11'
  '12'
  '13'
  '14'
  '15'
  '16'
])
param postgresqlVersion string = '16'

@description('Server SKU name')
@allowed([
  'Standard_B1ms'
  'Standard_B2s'
  'Standard_B2ms'
  'Standard_D2s_v3'
  'Standard_D2ds_v4'
  'Standard_D4s_v3'
  'Standard_D4ds_v4'
  'Standard_D8s_v3'
  'Standard_D8ds_v4'
  'Standard_D16s_v3'
  'Standard_D16ds_v4'
  'Standard_D32s_v3'
  'Standard_D32ds_v4'
])
param skuName string = 'Standard_D2ds_v4'

@description('Server SKU tier')
@allowed([
  'Burstable'
  'GeneralPurpose'
  'MemoryOptimized'
])
param skuTier string = 'GeneralPurpose'

@description('Storage size in GB')
@minValue(32)
@maxValue(32768)
param storageSizeGB int = 128

@description('Storage auto grow enabled')
param storageAutoGrow string = 'Enabled'

@description('Backup retention days')
@minValue(7)
@maxValue(35)
param backupRetentionDays int = 7

@description('Geo-redundant backup')
@allowed([
  'Disabled'
  'Enabled'
])
param geoRedundantBackup string = 'Enabled'

@description('High availability mode')
@allowed([
  'Disabled'
  'ZoneRedundant'
  'SameZone'
])
param highAvailability string = 'ZoneRedundant'

@description('Availability zone')
param availabilityZone string = '1'

@description('Standby availability zone')
param standbyAvailabilityZone string = '2'

@description('Public network access')
@allowed([
  'Disabled'
  'Enabled'
])
param publicNetworkAccess string = 'Disabled'

@description('Azure AD authentication only')
param azureAdAuthenticationOnly bool = false

@description('Azure AD administrator object ID')
param azureAdAdministratorObjectId string = ''

@description('Azure AD administrator name')
param azureAdAdministratorName string = ''

@description('Enable private endpoint')
param enablePrivateEndpoint bool = true

@description('Private endpoint subnet ID')
param privateEndpointSubnetId string = ''

@description('Delegated subnet ID for VNet integration')
param delegatedSubnetResourceId string = ''

@description('Private DNS zone ID')
param privateDnsZoneId string = ''

@description('Firewall rules')
param firewallRules array = []

@description('PostgreSQL configurations')
param configurations object = {
  'shared_preload_libraries': 'pg_stat_statements,pgaudit'
  'log_checkpoints': 'on'
  'log_connections': 'on'
  'log_disconnections': 'on'
  'log_duration': 'off'
  'log_error_verbosity': 'default'
  'log_lock_waits': 'on'
  'log_min_duration_statement': '1000'
  'log_statement': 'all'
  'max_connections': '100'
  'shared_buffers': '32768'
  'effective_cache_size': '131072'
  'maintenance_work_mem': '16384'
  'work_mem': '4096'
  'max_wal_size': '2048'
  'min_wal_size': '512'
  'checkpoint_completion_target': '0.9'
  'wal_buffers': '16384'
  'default_statistics_target': '100'
  'random_page_cost': '1.1'
  'effective_io_concurrency': '200'
  'autovacuum': 'on'
  'autovacuum_max_workers': '4'
  'autovacuum_naptime': '60'
  'pgaudit.log': 'ALL'
  'pgaudit.log_catalog': 'on'
  'pgaudit.log_parameter': 'on'
  'pgaudit.log_statement_once': 'off'
}

@description('Database names to create')
param databases array = [
  'numbatwallet'
  'numbatwallet_audit'
]

@description('Log Analytics workspace ID for diagnostics')
param logAnalyticsWorkspaceId string = ''

@description('Tags to apply to PostgreSQL server')
param tags object = {
  Component: 'Database'
  Purpose: 'Primary Data Store'
}

// PostgreSQL Flexible Server
resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-12-01-preview' = {
  name: serverName
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: skuTier
  }
  properties: {
    version: postgresqlVersion
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorLoginPassword

    authConfig: {
      activeDirectoryAuth: azureAdAuthenticationOnly ? 'Enabled' : 'Disabled'
      passwordAuth: azureAdAuthenticationOnly ? 'Disabled' : 'Enabled'
    }

    storage: {
      storageSizeGB: storageSizeGB
      autoGrow: storageAutoGrow
    }

    backup: {
      backupRetentionDays: backupRetentionDays
      geoRedundantBackup: geoRedundantBackup
      earliestRestoreDate: ''
    }

    highAvailability: {
      mode: highAvailability
      standbyAvailabilityZone: highAvailability != 'Disabled' ? standbyAvailabilityZone : ''
    }

    network: {
      delegatedSubnetResourceId: !empty(delegatedSubnetResourceId) ? delegatedSubnetResourceId : null
      privateDnsZoneArmResourceId: !empty(privateDnsZoneId) ? privateDnsZoneId : null
      publicNetworkAccess: publicNetworkAccess
    }

    maintenanceWindow: {
      customWindow: 'Enabled'
      dayOfWeek: 0  // Sunday
      startHour: 2
      startMinute: 0
    }

    dataEncryption: {
      type: 'SystemManaged'
    }

    availabilityZone: availabilityZone
  }
}

// Azure AD Administrator
resource azureAdAdmin 'Microsoft.DBforPostgreSQL/flexibleServers/administrators@2023-12-01-preview' = if (!empty(azureAdAdministratorObjectId)) {
  parent: postgresServer
  name: azureAdAdministratorObjectId
  properties: {
    principalType: 'User'
    principalName: azureAdAdministratorName
    tenantId: subscription().tenantId
  }
}

// Firewall Rules
resource firewallRule 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-12-01-preview' = [for rule in firewallRules: {
  parent: postgresServer
  name: rule.name
  properties: {
    startIpAddress: rule.startIpAddress
    endIpAddress: rule.endIpAddress
  }
}]

// Databases
resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-12-01-preview' = [for db in databases: {
  parent: postgresServer
  name: db
  properties: {
    charset: 'utf8'
    collation: 'en_US.utf8'
  }
}]

// Server Configurations
resource serverConfig 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2023-12-01-preview' = [for config in items(configurations): {
  parent: postgresServer
  name: config.key
  properties: {
    value: string(config.value)
    source: 'user-override'
  }
}]

// Private Endpoint for PostgreSQL
resource privateEndpoint 'Microsoft.Network/privateEndpoints@2023-06-01' = if (enablePrivateEndpoint && !empty(privateEndpointSubnetId)) {
  name: '${serverName}-pe'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${serverName}-pe-connection'
        properties: {
          privateLinkServiceId: postgresServer.id
          groupIds: ['postgresqlServer']
        }
      }
    ]
  }
}

// Private DNS Zone Group for PostgreSQL
resource privateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-06-01' = if (enablePrivateEndpoint && !empty(privateEndpointSubnetId)) {
  parent: privateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'privatelink-postgres-database-azure-com'
        properties: {
          privateDnsZoneId: '/subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.Network/privateDnsZones/privatelink.postgres.database.azure.com'
        }
      }
    ]
  }
}

// Diagnostic Settings
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (!empty(logAnalyticsWorkspaceId)) {
  name: 'diag-${serverName}'
  scope: postgresServer
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

// Alert Rules for PostgreSQL
resource alertRules 'Microsoft.Insights/metricAlerts@2018-03-01' = [for alert in alertRulesList: {
  name: '${serverName}-${alert.name}'
  location: 'global'
  tags: tags
  properties: {
    description: alert.description
    severity: alert.severity
    enabled: true
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    targetResourceType: 'Microsoft.DBforPostgreSQL/flexibleServers'
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
      postgresServer.id
    ]
    actions: []
  }
}]

var alertRulesList = [
  {
    name: 'high-cpu'
    description: 'Alert when CPU usage exceeds 80%'
    severity: 2
    metricName: 'cpu_percent'
    operator: 'GreaterThan'
    threshold: 80
    timeAggregation: 'Average'
  }
  {
    name: 'high-memory'
    description: 'Alert when memory usage exceeds 80%'
    severity: 2
    metricName: 'memory_percent'
    operator: 'GreaterThan'
    threshold: 80
    timeAggregation: 'Average'
  }
  {
    name: 'high-connections'
    description: 'Alert when active connections exceed 80'
    severity: 2
    metricName: 'active_connections'
    operator: 'GreaterThan'
    threshold: 80
    timeAggregation: 'Average'
  }
  {
    name: 'storage-usage'
    description: 'Alert when storage usage exceeds 80%'
    severity: 2
    metricName: 'storage_percent'
    operator: 'GreaterThan'
    threshold: 80
    timeAggregation: 'Average'
  }
]

// Outputs
output serverId string = postgresServer.id
output serverName string = postgresServer.name
output fqdn string = postgresServer.properties.fullyQualifiedDomainName
output administratorLogin string = administratorLogin
output databases array = databases
output privateEndpointId string = enablePrivateEndpoint && !empty(privateEndpointSubnetId) ? privateEndpoint.id : ''
output connectionString string = 'Server=${postgresServer.properties.fullyQualifiedDomainName};Database=numbatwallet;Port=5432;User Id=${administratorLogin}@${serverName};Password=${administratorLoginPassword};Ssl Mode=Require;'
// Database Module - PostgreSQL Flexible Server
// Purpose: Multi-tenant database with high availability

@description('Azure region for resources')
param location string

@description('Naming prefix for resources')
param namingPrefix string

@description('Environment name')
param environment string

@description('Virtual network ID')
param virtualNetworkId string

@description('Database subnet ID')
param databaseSubnetId string

@description('Administrator login')
@secure()
param administratorLogin string

@description('Administrator password')
@secure()
param administratorPassword string = newGuid()

@description('Tags to apply to resources')
param tags object

// Variables
var serverName = 'psql-${namingPrefix}'
var skuTier = environment == 'prod' ? 'GeneralPurpose' : 'Burstable'
var skuName = environment == 'prod' ? 'Standard_D2s_v3' : 'Standard_B2s'
var storageSizeGB = environment == 'prod' ? 256 : 32
var backupRetentionDays = environment == 'prod' ? 30 : 7
var geoRedundantBackup = environment == 'prod' ? 'Enabled' : 'Disabled'
var highAvailabilityMode = environment == 'prod' ? 'ZoneRedundant' : 'Disabled'

// PostgreSQL Flexible Server
resource postgresqlServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-06-30' = {
  name: serverName
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: skuTier
  }
  properties: {
    version: '16'
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorPassword
    storage: {
      storageSizeGB: storageSizeGB
      autoGrow: 'Enabled'
    }
    backup: {
      backupRetentionDays: backupRetentionDays
      geoRedundantBackup: geoRedundantBackup
    }
    network: {
      delegatedSubnetResourceId: databaseSubnetId
      privateDnsZoneArmResourceId: postgresqlPrivateDnsZone.id
    }
    highAvailability: {
      mode: highAvailabilityMode
      standbyAvailabilityZone: environment == 'prod' ? '2' : ''
    }
    maintenanceWindow: {
      customWindow: 'Enabled'
      startHour: 2
      startMinute: 0
      dayOfWeek: 0
    }
  }
}

// Database for each tenant (starting with dev tenant)
resource devDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-06-30' = {
  parent: postgresqlServer
  name: 'numbatwallet_dev'
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

// Firewall Rules - Allow Azure Services
resource firewallRuleAzure 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-06-30' = {
  parent: postgresqlServer
  name: 'AllowAllAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// PostgreSQL Server Parameters
resource sslEnforcement 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2023-06-30' = {
  parent: postgresqlServer
  name: 'require_secure_transport'
  properties: {
    value: 'ON'
  }
}

resource connectionThrottling 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2023-06-30' = {
  parent: postgresqlServer
  name: 'connection_throttle.enable'
  properties: {
    value: 'ON'
  }
}

resource logCheckpoints 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2023-06-30' = {
  parent: postgresqlServer
  name: 'log_checkpoints'
  properties: {
    value: 'ON'
  }
}

resource logConnections 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2023-06-30' = {
  parent: postgresqlServer
  name: 'log_connections'
  properties: {
    value: 'ON'
  }
}

resource logDisconnections 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2023-06-30' = {
  parent: postgresqlServer
  name: 'log_disconnections'
  properties: {
    value: 'ON'
  }
}

// Private DNS Zone for PostgreSQL
resource postgresqlPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.postgres.database.azure.com'
  location: 'global'
  tags: tags
}

// Link Private DNS Zone to VNet
resource postgresqlPrivateDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: postgresqlPrivateDnsZone
  name: 'vnet-link-${serverName}'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: virtualNetworkId
    }
  }
}

// Alert Rules for Production
resource cpuAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = if (environment == 'prod') {
  name: 'alert-${serverName}-cpu'
  location: 'global'
  tags: tags
  properties: {
    severity: 2
    enabled: true
    scopes: [postgresqlServer.id]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          threshold: 80
          name: 'CPU Percent'
          metricNamespace: 'Microsoft.DBforPostgreSQL/flexibleServers'
          metricName: 'cpu_percent'
          operator: 'GreaterThan'
          timeAggregation: 'Average'
        }
      ]
    }
    autoMitigate: false
  }
}

resource storageAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = if (environment == 'prod') {
  name: 'alert-${serverName}-storage'
  location: 'global'
  tags: tags
  properties: {
    severity: 2
    enabled: true
    scopes: [postgresqlServer.id]
    evaluationFrequency: 'PT15M'
    windowSize: 'PT1H'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          threshold: 80
          name: 'Storage Percent'
          metricNamespace: 'Microsoft.DBforPostgreSQL/flexibleServers'
          metricName: 'storage_percent'
          operator: 'GreaterThan'
          timeAggregation: 'Average'
        }
      ]
    }
    autoMitigate: false
  }
}

// Outputs
output serverName string = postgresqlServer.name
output serverId string = postgresqlServer.id
output serverFqdn string = postgresqlServer.properties.fullyQualifiedDomainName
output administratorLogin string = administratorLogin
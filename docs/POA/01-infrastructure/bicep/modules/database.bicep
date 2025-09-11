// Database Module - PostgreSQL Flexible Server
// Version: 1.0
// Description: Creates PostgreSQL database with environment-based sizing

@description('Azure region for resources')
param location string

@description('Naming prefix')
param namingPrefix string

@description('Resource tags')
param tags object

@description('PostgreSQL SKU name')
param skuName string

@description('Storage size in GB')
param storageSizeGB int

@description('Subnet ID for database')
param subnetId string

@description('Key Vault name for secrets')
param keyVaultName string

@description('Environment name')
param environment string

// ==================== VARIABLES ====================
var serverName = 'psql-${namingPrefix}'
var adminUsername = 'numbatadmin'
var adminPassword = '${uniqueString(resourceGroup().id)}Poa2025!'

// High availability configuration based on environment
var haConfig = {
  dev: 'Disabled'
  test: 'Disabled'
  demo: 'SameZone'
  prod: 'ZoneRedundant'
}

// Backup retention based on environment
var backupRetention = {
  dev: 7
  test: 14
  demo: 30
  prod: 35
}

// ==================== POSTGRESQL SERVER ====================
resource postgresqlServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-06-01-preview' = {
  name: serverName
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: environment == 'prod' ? 'GeneralPurpose' : 'Burstable'
  }
  properties: {
    version: '15'
    administratorLogin: adminUsername
    administratorLoginPassword: adminPassword
    storage: {
      storageSizeGB: storageSizeGB
      autoGrow: environment == 'prod' ? 'Enabled' : 'Disabled'
    }
    backup: {
      backupRetentionDays: backupRetention[environment]
      geoRedundantBackup: environment == 'prod' ? 'Enabled' : 'Disabled'
    }
    network: {
      delegatedSubnetResourceId: subnetId
      privateDnsZoneArmResourceId: privateDnsZone.id
    }
    highAvailability: {
      mode: haConfig[environment]
      standbyAvailabilityZone: environment == 'prod' ? '2' : '1'
    }
    maintenanceWindow: {
      customWindow: 'Enabled'
      dayOfWeek: 0  // Sunday
      startHour: 2
      startMinute: 0
    }
  }
}

// ==================== PRIVATE DNS ZONE ====================
resource privateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.postgres.database.azure.com'
  location: 'global'
  tags: tags
}

resource privateDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: privateDnsZone
  name: '${serverName}-link'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: resourceGroup().id
    }
  }
}

// ==================== DATABASES ====================
// Create databases for different credential types
resource walletDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-06-01-preview' = {
  parent: postgresqlServer
  name: 'numbat_wallet'
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

resource auditDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-06-01-preview' = if (environment != 'dev') {
  parent: postgresqlServer
  name: 'numbat_audit'
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

// ==================== FIREWALL RULES ====================
// Allow Azure services for dev/test only
resource allowAzureServices 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-06-01-preview' = if (environment == 'dev' || environment == 'test') {
  parent: postgresqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ==================== SERVER PARAMETERS ====================
// Configure PostgreSQL parameters based on environment
resource maxConnections 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2023-06-01-preview' = {
  parent: postgresqlServer
  name: 'max_connections'
  properties: {
    value: environment == 'prod' ? '200' : '100'
    source: 'user-override'
  }
}

resource sharedBuffers 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2023-06-01-preview' = {
  parent: postgresqlServer
  name: 'shared_buffers'
  properties: {
    value: environment == 'prod' ? '256000' : '32768'  // in 8KB pages
    source: 'user-override'
  }
}

resource logStatement 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2023-06-01-preview' = {
  parent: postgresqlServer
  name: 'log_statement'
  properties: {
    value: environment == 'prod' ? 'mod' : 'all'  // Log more in dev/test
    source: 'user-override'
  }
}

// Enable extensions
resource pgcryptoExtension 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2023-06-01-preview' = {
  parent: postgresqlServer
  name: 'azure.extensions'
  properties: {
    value: 'PGCRYPTO,UUID-OSSP,CITEXT'
    source: 'user-override'
  }
}

// ==================== KEY VAULT SECRETS ====================
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource dbHostSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'DatabaseHost'
  properties: {
    value: postgresqlServer.properties.fullyQualifiedDomainName
  }
}

resource dbUsernameSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'DatabaseUsername'
  properties: {
    value: adminUsername
  }
}

resource dbPasswordSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'DatabasePassword'
  properties: {
    value: adminPassword
  }
}

resource connectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'DatabaseConnectionString'
  properties: {
    value: 'Host=${postgresqlServer.properties.fullyQualifiedDomainName};Database=numbat_wallet;Username=${adminUsername};Password=${adminPassword};SSL Mode=Require;Trust Server Certificate=true;'
  }
}

// ==================== DIAGNOSTICS ====================
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (environment != 'dev') {
  scope: postgresqlServer
  name: '${serverName}-diagnostics'
  properties: {
    logs: [
      {
        categoryGroup: 'allLogs'
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
          days: environment == 'prod' ? 90 : 30
        }
      }
    ]
  }
}

// ==================== OUTPUTS ====================
output serverName string = postgresqlServer.name
output serverFQDN string = postgresqlServer.properties.fullyQualifiedDomainName
output databaseName string = walletDatabase.name
output serverResourceId string = postgresqlServer.id
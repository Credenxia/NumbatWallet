// dev.bicepparam - Development environment parameters for NumbatWallet
using '../main.bicep'

// Environment configuration
param environment = 'dev'
param location = 'australiaeast'
param locationCode = 'aue'
param applicationName = 'numbatwallet'

// Resource naming
param resourceGroupName = 'rg-numbatwallet-dev-aue'

// Network configuration (using new VNet for dev)
param existingVnetResourceGroup = ''
param existingVnetName = ''
param privateEndpointSubnetId = ''

// Security & Access
param administratorObjectId = '${readEnvironmentVariable('AZURE_ADMIN_OBJECT_ID', '')}'
param managedIdentityObjectId = ''
param enablePrivateEndpoints = false // Public access for dev
param allowedIpAddresses = [
  // Add developer IP addresses here
]

// Database configuration
param postgresAdminUsername = 'nwadmin'
param postgresAdminPassword = '${readEnvironmentVariable('POSTGRES_ADMIN_PASSWORD', '')}'

// Application configuration
param jwtSigningKey = '${readEnvironmentVariable('JWT_SIGNING_KEY', '')}'
param apiImageTag = 'latest'
param adminImageTag = 'latest'

// Monitoring
param logAnalyticsWorkspaceName = 'law-numbatwallet-dev-aue'

// Tags
param tags = {
  Application: 'NumbatWallet'
  Environment: 'dev'
  ManagedBy: 'Bicep'
  DeploymentDate: utcNow('yyyy-MM-dd')
  CostCenter: 'Development'
  Owner: 'WA-Government'
  DataClassification: 'Protected'
  Compliance: 'TDIF'
}
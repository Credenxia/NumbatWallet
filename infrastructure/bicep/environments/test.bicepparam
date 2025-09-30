// test.bicepparam - Test/Staging environment parameters for NumbatWallet
using '../main.bicep'

// Environment configuration
param environment = 'test'
param location = 'australiaeast'
param locationCode = 'aue'
param applicationName = 'numbatwallet'

// Resource naming
param resourceGroupName = 'rg-numbatwallet-test-aue'

// Network configuration (using new VNet for test)
param existingVnetResourceGroup = ''
param existingVnetName = ''
param privateEndpointSubnetId = ''

// Security & Access
param administratorObjectId = '${readEnvironmentVariable('AZURE_ADMIN_OBJECT_ID', '')}'
param managedIdentityObjectId = '${readEnvironmentVariable('MANAGED_IDENTITY_OBJECT_ID', '')}'
param enablePrivateEndpoints = true // Private endpoints for test
param allowedIpAddresses = []

// Database configuration
param postgresAdminUsername = 'nwadmin'
param postgresAdminPassword = '${readEnvironmentVariable('POSTGRES_ADMIN_PASSWORD', '')}'

// Application configuration
param jwtSigningKey = '${readEnvironmentVariable('JWT_SIGNING_KEY', '')}'
param apiImageTag = '${readEnvironmentVariable('API_IMAGE_TAG', 'latest')}'
param adminImageTag = '${readEnvironmentVariable('ADMIN_IMAGE_TAG', 'latest')}'

// Monitoring
param logAnalyticsWorkspaceName = 'law-numbatwallet-test-aue'

// Tags
param tags = {
  Application: 'NumbatWallet'
  Environment: 'test'
  ManagedBy: 'Bicep'
  DeploymentDate: utcNow('yyyy-MM-dd')
  CostCenter: 'Testing'
  Owner: 'WA-Government'
  DataClassification: 'Protected'
  Compliance: 'TDIF'
}
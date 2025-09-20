// prod.bicepparam - Production environment parameters for NumbatWallet
using '../main.bicep'

// Environment configuration
param environment = 'prod'
param location = 'australiaeast'
param locationCode = 'aue'
param applicationName = 'numbatwallet'

// Resource naming
param resourceGroupName = 'rg-numbatwallet-prod-aue'

// Network configuration (using existing enterprise VNet for prod)
param existingVnetResourceGroup = '${readEnvironmentVariable('PROD_VNET_RESOURCE_GROUP', '')}'
param existingVnetName = '${readEnvironmentVariable('PROD_VNET_NAME', '')}'
param privateEndpointSubnetId = '${readEnvironmentVariable('PROD_PRIVATE_ENDPOINT_SUBNET_ID', '')}'

// Security & Access
param administratorObjectId = '${readEnvironmentVariable('AZURE_ADMIN_OBJECT_ID', '')}'
param managedIdentityObjectId = '${readEnvironmentVariable('MANAGED_IDENTITY_OBJECT_ID', '')}'
param enablePrivateEndpoints = true // Always private for production
param allowedIpAddresses = [] // No public access in production

// Database configuration
param postgresAdminUsername = 'nwadmin'
param postgresAdminPassword = '${readEnvironmentVariable('POSTGRES_ADMIN_PASSWORD', '')}'

// Application configuration
param jwtSigningKey = '${readEnvironmentVariable('JWT_SIGNING_KEY', '')}'
param apiImageTag = '${readEnvironmentVariable('API_IMAGE_TAG', 'stable')}'
param adminImageTag = '${readEnvironmentVariable('ADMIN_IMAGE_TAG', 'stable')}'

// Monitoring
param logAnalyticsWorkspaceName = 'law-numbatwallet-prod-aue'

// Tags
param tags = {
  Application: 'NumbatWallet'
  Environment: 'prod'
  ManagedBy: 'Bicep'
  DeploymentDate: utcNow('yyyy-MM-dd')
  CostCenter: 'Production'
  Owner: 'WA-Government'
  DataClassification: 'Protected'
  Compliance: 'TDIF'
  BusinessCritical: 'true'
  BackupRequired: 'true'
  DisasterRecovery: 'enabled'
}
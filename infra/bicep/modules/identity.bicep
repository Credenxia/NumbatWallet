// Identity Module - Managed Identity for secure authentication
// Purpose: System-assigned managed identity for Azure services

@description('Azure region for resources')
param location string

@description('Naming prefix for resources')
param namingPrefix string

@description('Environment name')
param environment string

@description('Tags to apply to resources')
param tags object

// User-Assigned Managed Identity
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-${namingPrefix}'
  location: location
  tags: tags
}

// Outputs
output managedIdentityId string = managedIdentity.id
output principalId string = managedIdentity.properties.principalId
output clientId string = managedIdentity.properties.clientId
output tenantId string = managedIdentity.properties.tenantId
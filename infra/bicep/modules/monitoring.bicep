// Monitoring Module
@description('Azure region')
param location string
@description('Naming prefix')
param namingPrefix string
@description('Environment')
param environment string
@description('Admin email')
param adminEmail string
@description('Tags')
param tags object

resource workspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'log-${namingPrefix}'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: environment == 'prod' ? 90 : 30
  }
}

output workspaceId string = workspace.id

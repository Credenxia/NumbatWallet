// App Service Module
@description('Azure region')
param location string
@description('Naming prefix')
param namingPrefix string
@description('Environment')
param environment string
@description('VNet ID')
param virtualNetworkId string
@description('App subnet ID')
param appSubnetId string
@description('Managed Identity ID')
param managedIdentityId string
@description('Tags')
param tags object

var appServicePlanName = 'asp-${namingPrefix}'
var appServiceName = 'app-${namingPrefix}'

resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  sku: {
    name: environment == 'prod' ? 'P1v3' : 'B1'
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource appService 'Microsoft.Web/sites@2023-01-01' = {
  name: appServiceName
  location: location
  tags: tags
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
  }
}

output defaultHostName string = appService.properties.defaultHostName

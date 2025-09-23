// Application Gateway Module
@description('Azure region')
param location string
@description('Naming prefix')
param namingPrefix string
@description('Environment')
param environment string
@description('VNet ID')
param virtualNetworkId string
@description('Gateway subnet ID')
param gatewaySubnetId string
@description('Backend pool FQDNs')
param backendPoolFqdns array
@description('Tags')
param tags object

output fqdn string = 'appgw-${namingPrefix}.${location}.cloudapp.azure.com'

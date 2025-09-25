// POA-004: Virtual Network and networking module
// Network configuration for NumbatWallet infrastructure

@description('The environment for deployment')
@allowed(['dev', 'test', 'prod'])
param environment string

@description('The Azure region for resources')
param location string

@description('Base name for resource naming')
param baseName string

@description('Enable DDoS protection (for production)')
param enableDdosProtection bool = environment == 'prod'

// Variables
var vnetName = '${baseName}-${environment}-vnet'
var nsgName = '${baseName}-${environment}-nsg'
var ddosProtectionPlanName = '${baseName}-${environment}-ddos'

// Address spaces
var addressPrefix = environment == 'prod' ? '10.0.0.0/16' : environment == 'test' ? '10.1.0.0/16' : '10.2.0.0/16'

// DDoS Protection Plan (for production)
resource ddosProtectionPlan 'Microsoft.Network/ddosProtectionPlans@2023-05-01' = if (enableDdosProtection) {
  name: ddosProtectionPlanName
  location: location
  properties: {}
  tags: {
    Environment: environment
    ManagedBy: 'Bicep'
    Application: 'NumbatWallet'
  }
}

// Network Security Group
resource networkSecurityGroup 'Microsoft.Network/networkSecurityGroups@2023-05-01' = {
  name: nsgName
  location: location
  properties: {
    securityRules: [
      {
        name: 'AllowHttpsInbound'
        properties: {
          priority: 100
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '443'
          sourceAddressPrefix: environment == 'prod' ? 'Internet' : '*'
          destinationAddressPrefix: '*'
        }
      }
      {
        name: 'AllowHealthProbes'
        properties: {
          priority: 110
          direction: 'Inbound'
          access: 'Allow'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: 'AzureLoadBalancer'
          destinationAddressPrefix: '*'
        }
      }
      {
        name: 'AllowContainerAppsControl'
        properties: {
          priority: 120
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '443'
          sourceAddressPrefix: 'AzureContainerApps'
          destinationAddressPrefix: '*'
        }
      }
      {
        name: 'AllowPostgreSQL'
        properties: {
          priority: 130
          direction: 'Outbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '5432'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: 'Sql.AustraliaEast'
        }
      }
      {
        name: 'AllowKeyVault'
        properties: {
          priority: 140
          direction: 'Outbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '443'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: 'AzureKeyVault.AustraliaEast'
        }
      }
      {
        name: 'AllowStorage'
        properties: {
          priority: 150
          direction: 'Outbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '443'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: 'Storage.AustraliaEast'
        }
      }
      {
        name: 'AllowAzureMonitor'
        properties: {
          priority: 160
          direction: 'Outbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '443'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: 'AzureMonitor'
        }
      }
      {
        name: 'DenyAllInbound'
        properties: {
          priority: 4096
          direction: 'Inbound'
          access: 'Deny'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
        }
      }
    ]
  }
  tags: {
    Environment: environment
    ManagedBy: 'Bicep'
    Application: 'NumbatWallet'
  }
}

// Virtual Network
resource virtualNetwork 'Microsoft.Network/virtualNetworks@2023-05-01' = {
  name: vnetName
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        addressPrefix
      ]
    }
    ddosProtectionPlan: enableDdosProtection ? {
      id: ddosProtectionPlan.id
    } : null
    subnets: [
      {
        name: 'container-apps-subnet'
        properties: {
          addressPrefix: '${split(addressPrefix, '/')[0]}/24'
          networkSecurityGroup: {
            id: networkSecurityGroup.id
          }
          serviceEndpoints: [
            {
              service: 'Microsoft.KeyVault'
              locations: [
                location
              ]
            }
            {
              service: 'Microsoft.Storage'
              locations: [
                location
              ]
            }
            {
              service: 'Microsoft.Sql'
              locations: [
                location
              ]
            }
            {
              service: 'Microsoft.ContainerRegistry'
              locations: [
                location
              ]
            }
          ]
          delegations: [
            {
              name: 'containerApps'
              properties: {
                serviceName: 'Microsoft.App/environments'
              }
            }
          ]
        }
      }
      {
        name: 'postgresql-subnet'
        properties: {
          addressPrefix: '10.${environment == 'prod' ? '0' : environment == 'test' ? '1' : '2'}.1.0/24'
          networkSecurityGroup: {
            id: networkSecurityGroup.id
          }
          serviceEndpoints: [
            {
              service: 'Microsoft.Sql'
              locations: [
                location
              ]
            }
          ]
          delegations: [
            {
              name: 'postgresql'
              properties: {
                serviceName: 'Microsoft.DBforPostgreSQL/flexibleServers'
              }
            }
          ]
        }
      }
      {
        name: 'private-endpoints-subnet'
        properties: {
          addressPrefix: '10.${environment == 'prod' ? '0' : environment == 'test' ? '1' : '2'}.2.0/24'
          networkSecurityGroup: {
            id: networkSecurityGroup.id
          }
          privateEndpointNetworkPolicies: 'Disabled'
          privateLinkServiceNetworkPolicies: 'Disabled'
        }
      }
      {
        name: 'gateway-subnet'
        properties: {
          addressPrefix: '10.${environment == 'prod' ? '0' : environment == 'test' ? '1' : '2'}.3.0/24'
          networkSecurityGroup: {
            id: networkSecurityGroup.id
          }
        }
      }
    ]
  }
  tags: {
    Environment: environment
    ManagedBy: 'Bicep'
    Application: 'NumbatWallet'
  }
}

// Application Gateway (for production)
resource applicationGateway 'Microsoft.Network/applicationGateways@2023-05-01' = if (environment == 'prod') {
  name: '${baseName}-${environment}-appgw'
  location: location
  properties: {
    sku: {
      name: 'WAF_v2'
      tier: 'WAF_v2'
      capacity: 2
    }
    gatewayIPConfigurations: [
      {
        name: 'appGatewayIpConfig'
        properties: {
          subnet: {
            id: '${virtualNetwork.id}/subnets/gateway-subnet'
          }
        }
      }
    ]
    frontendIPConfigurations: [
      {
        name: 'appGwPublicFrontendIp'
        properties: {
          privateIPAllocationMethod: 'Dynamic'
          publicIPAddress: {
            id: publicIP.id
          }
        }
      }
    ]
    frontendPorts: [
      {
        name: 'port_443'
        properties: {
          port: 443
        }
      }
    ]
    backendAddressPools: [
      {
        name: 'containerAppsBackend'
        properties: {}
      }
    ]
    backendHttpSettingsCollection: [
      {
        name: 'containerAppsHttpSettings'
        properties: {
          port: 443
          protocol: 'Https'
          cookieBasedAffinity: 'Disabled'
          pickHostNameFromBackendAddress: true
          requestTimeout: 30
          probe: {
            id: resourceId('Microsoft.Network/applicationGateways/probes', '${baseName}-${environment}-appgw', 'containerAppsProbe')
          }
        }
      }
    ]
    httpListeners: [
      {
        name: 'containerAppsListener'
        properties: {
          frontendIPConfiguration: {
            id: resourceId('Microsoft.Network/applicationGateways/frontendIPConfigurations', '${baseName}-${environment}-appgw', 'appGwPublicFrontendIp')
          }
          frontendPort: {
            id: resourceId('Microsoft.Network/applicationGateways/frontendPorts', '${baseName}-${environment}-appgw', 'port_443')
          }
          protocol: 'Https'
          requireServerNameIndication: true
        }
      }
    ]
    requestRoutingRules: [
      {
        name: 'containerAppsRule'
        properties: {
          ruleType: 'Basic'
          priority: 100
          httpListener: {
            id: resourceId('Microsoft.Network/applicationGateways/httpListeners', '${baseName}-${environment}-appgw', 'containerAppsListener')
          }
          backendAddressPool: {
            id: resourceId('Microsoft.Network/applicationGateways/backendAddressPools', '${baseName}-${environment}-appgw', 'containerAppsBackend')
          }
          backendHttpSettings: {
            id: resourceId('Microsoft.Network/applicationGateways/backendHttpSettingsCollection', '${baseName}-${environment}-appgw', 'containerAppsHttpSettings')
          }
        }
      }
    ]
    probes: [
      {
        name: 'containerAppsProbe'
        properties: {
          protocol: 'Https'
          path: '/health'
          interval: 30
          timeout: 30
          unhealthyThreshold: 3
          pickHostNameFromBackendHttpSettings: true
        }
      }
    ]
    webApplicationFirewallConfiguration: {
      enabled: true
      firewallMode: 'Prevention'
      ruleSetType: 'OWASP'
      ruleSetVersion: '3.2'
      requestBodyCheck: true
      maxRequestBodySizeInKb: 128
      fileUploadLimitInMb: 100
    }
    enableHttp2: true
    autoscaleConfiguration: {
      minCapacity: 2
      maxCapacity: 10
    }
  }
  tags: {
    Environment: environment
    ManagedBy: 'Bicep'
    Application: 'NumbatWallet'
  }
}

// Public IP for Application Gateway
resource publicIP 'Microsoft.Network/publicIPAddresses@2023-05-01' = if (environment == 'prod') {
  name: '${baseName}-${environment}-appgw-pip'
  location: location
  sku: {
    name: 'Standard'
    tier: 'Regional'
  }
  properties: {
    publicIPAllocationMethod: 'Static'
    publicIPAddressVersion: 'IPv4'
    dnsSettings: {
      domainNameLabel: '${baseName}-${environment}'
    }
  }
  tags: {
    Environment: environment
    ManagedBy: 'Bicep'
    Application: 'NumbatWallet'
  }
}

// NAT Gateway for outbound connectivity
resource natGateway 'Microsoft.Network/natGateways@2023-05-01' = {
  name: '${baseName}-${environment}-natgw'
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    idleTimeoutInMinutes: 4
    publicIpAddresses: [
      {
        id: natPublicIP.id
      }
    ]
  }
  tags: {
    Environment: environment
    ManagedBy: 'Bicep'
    Application: 'NumbatWallet'
  }
}

// Public IP for NAT Gateway
resource natPublicIP 'Microsoft.Network/publicIPAddresses@2023-05-01' = {
  name: '${baseName}-${environment}-natgw-pip'
  location: location
  sku: {
    name: 'Standard'
    tier: 'Regional'
  }
  properties: {
    publicIPAllocationMethod: 'Static'
    publicIPAddressVersion: 'IPv4'
  }
  zones: environment == 'prod' ? ['1', '2', '3'] : []
  tags: {
    Environment: environment
    ManagedBy: 'Bicep'
    Application: 'NumbatWallet'
  }
}

// Outputs
output vnetId string = virtualNetwork.id
output vnetName string = virtualNetwork.name
output containerAppsSubnetId string = '${virtualNetwork.id}/subnets/container-apps-subnet'
output postgresqlSubnetId string = '${virtualNetwork.id}/subnets/postgresql-subnet'
output privateEndpointsSubnetId string = '${virtualNetwork.id}/subnets/private-endpoints-subnet'
output gatewaySubnetId string = '${virtualNetwork.id}/subnets/gateway-subnet'
output applicationGatewayId string = environment == 'prod' ? applicationGateway.id : ''
output publicIPAddress string = environment == 'prod' ? publicIP.properties.ipAddress : ''
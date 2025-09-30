// Azure Dedicated HSM Module for NumbatWallet
// FIPS 140-2 Level 2+ Compliance
// POA-128: HSM Integration

@description('The name of the Dedicated HSM')
param hsmName string

@description('The location of the HSM')
param location string = resourceGroup().location

@description('The SKU of the Dedicated HSM')
@allowed([
  'SafeNet Luna Network HSM A790'
  'payShield10K_LMK1_CPS60'
  'payShield10K_LMK1_CPS250'
  'payShield10K_LMK1_CPS2500'
  'payShield10K_LMK2_CPS60'
  'payShield10K_LMK2_CPS250'
  'payShield10K_LMK2_CPS2500'
])
param hsmSku string = 'SafeNet Luna Network HSM A790'

@description('The subnet ID for HSM deployment')
param subnetId string

@description('Tags for the HSM')
param tags object = {
  Environment: 'Production'
  Purpose: 'PKI'
  Compliance: 'FIPS-140-2-Level2+'
  ManagedBy: 'Platform Team'
}

@description('Administrators group object ID from Entra ID')
param administratorsGroupObjectId string

@description('Backup operators group object ID from Entra ID')
param backupOperatorsGroupObjectId string

// Network interface for HSM
resource hsmNetworkInterface 'Microsoft.Network/networkInterfaces@2023-09-01' = {
  name: '${hsmName}-nic'
  location: location
  tags: tags
  properties: {
    ipConfigurations: [
      {
        name: 'ipconfig1'
        properties: {
          privateIPAllocationMethod: 'Dynamic'
          subnet: {
            id: subnetId
          }
        }
      }
    ]
    enableAcceleratedNetworking: true
    enableIPForwarding: false
  }
}

// Dedicated HSM Resource
resource dedicatedHsm 'Microsoft.HardwareSecurityModules/dedicatedHSMs@2021-11-30' = {
  name: hsmName
  location: location
  tags: tags
  sku: {
    name: hsmSku
  }
  properties: {
    networkProfile: {
      subnet: {
        id: subnetId
      }
      networkInterfaces: [
        {
          id: hsmNetworkInterface.id
        }
      ]
    }
    stampId: 'stamp1' // Availability zone stamp
    managementNetworkProfile: {
      subnet: {
        id: subnetId
      }
      networkInterfaces: [
        {
          id: hsmNetworkInterface.id
        }
      ]
    }
  }
  zones: ['1'] // Deploy to availability zone 1 for HA
}

// Diagnostic settings for HSM
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${hsmName}-diagnostics'
  scope: dedicatedHsm
  properties: {
    workspaceId: resourceId('Microsoft.OperationalInsights/workspaces', 'log-numbatwallet-${location}')
    logs: [
      {
        category: 'AuditEvent'
        enabled: true
        retentionPolicy: {
          days: 365
          enabled: true
        }
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
        retentionPolicy: {
          days: 90
          enabled: true
        }
      }
    ]
  }
}

// Role assignments for HSM administration
resource hsmAdminRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(dedicatedHsm.id, administratorsGroupObjectId, 'HSM Crypto Officer')
  scope: dedicatedHsm
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '515c2406-2bce-4ba3-8e05-4f6d74c23f88') // HSM Crypto Officer role
    principalId: administratorsGroupObjectId
    principalType: 'Group'
  }
}

resource hsmBackupRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(dedicatedHsm.id, backupOperatorsGroupObjectId, 'HSM Backup Operator')
  scope: dedicatedHsm
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b616af62-9999-4c3a-850f-6d9d319916aa') // HSM Backup Operator role
    principalId: backupOperatorsGroupObjectId
    principalType: 'Group'
  }
}

// Private endpoint for HSM (for secure access)
resource privateEndpoint 'Microsoft.Network/privateEndpoints@2023-09-01' = {
  name: '${hsmName}-pe'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: subnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${hsmName}-connection'
        properties: {
          privateLinkServiceId: dedicatedHsm.id
          groupIds: ['dedicatedHsm']
        }
      }
    ]
  }
}

// Private DNS zone for HSM
resource privateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.azure-hsm.net'
  location: 'global'
  tags: tags
}

resource privateDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: privateDnsZone
  name: '${hsmName}-link'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: resourceId('Microsoft.Network/virtualNetworks', 'vnet-numbatwallet-${location}')
    }
  }
}

resource privateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-09-01' = {
  parent: privateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'config1'
        properties: {
          privateDnsZoneId: privateDnsZone.id
        }
      }
    ]
  }
}

// Outputs for use in other modules
output hsmId string = dedicatedHsm.id
output hsmName string = dedicatedHsm.name
output hsmUri string = 'https://${hsmName}.${location}.azure-hsm.net'
output privateEndpointId string = privateEndpoint.id
output networkInterfaceId string = hsmNetworkInterface.id

// Important configuration outputs
output configurationInstructions string = '''
HSM Post-Deployment Configuration Required:

1. Initialize HSM:
   - Connect via SSH to HSM management interface
   - Run initialization wizard
   - Set HSM admin password
   - Configure network settings

2. Create Security Domain:
   - Generate security domain certificates (minimum 3)
   - Use M of N quorum (recommended: 3 of 5)
   - Securely store security domain files

3. Configure Partitions:
   - Create partition for production keys
   - Create partition for development/test
   - Set partition policies and PINs

4. Key Ceremony:
   - Schedule key ceremony with security officers
   - Generate master keys
   - Implement dual control procedures
   - Document key ceremony in compliance log

5. High Availability Setup:
   - Deploy second HSM in availability zone 2
   - Configure HSM clustering
   - Setup automatic failover
   - Test failover procedures

6. Monitoring:
   - Configure SNMP monitoring
   - Setup alerts for critical events
   - Enable audit logging to SIEM
   - Configure performance metrics

7. Backup Procedures:
   - Configure automated backups
   - Test backup restoration
   - Store backups in geographically separate location
   - Implement backup encryption

Contact platform-security@numbatWallet.gov.au for assistance.
'''
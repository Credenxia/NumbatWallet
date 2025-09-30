// POA-009d: Azure Container Apps module
// Container Apps for hosting NumbatWallet API and Admin Portal

@description('The environment for deployment')
@allowed(['dev', 'test', 'prod'])
param environment string

@description('The Azure region for resources')
param location string

@description('Base name for resource naming')
param baseName string

@description('Container Registry name')
param containerRegistryName string

@description('Key Vault resource ID for secrets')
param keyVaultId string

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('PostgreSQL connection string secret name in Key Vault')
param dbConnectionStringSecretName string = 'postgresql-connection-string'

@description('Virtual Network subnet ID for Container Apps environment')
param subnetId string

@description('Log Analytics Workspace ID')
param logAnalyticsWorkspaceId string

@description('Docker image tag')
param imageTag string = 'latest'

// Variables
var resourcePrefix = '${baseName}-${environment}'
var containerAppsEnvironmentName = '${resourcePrefix}-cae'
var apiContainerAppName = '${resourcePrefix}-api'
var adminContainerAppName = '${resourcePrefix}-admin'

// Container Apps Environment
resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: containerAppsEnvironmentName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: reference(logAnalyticsWorkspaceId, '2022-10-01').customerId
        sharedKey: listKeys(logAnalyticsWorkspaceId, '2022-10-01').primarySharedKey
      }
    }
    vnetConfiguration: {
      infrastructureSubnetId: subnetId
    }
    zoneRedundant: environment == 'prod'
    workloadProfiles: [
      {
        workloadProfileType: 'Consumption'
        name: 'Consumption'
      }
    ]
  }
  tags: {
    Environment: environment
    ManagedBy: 'Bicep'
    Application: 'NumbatWallet'
  }
}

// Container Apps API
resource apiContainerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: apiContainerAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: 'Multiple'
      ingress: {
        external: true
        targetPort: 80
        transport: 'http'
        traffic: [
          {
            weight: 100
            latestRevision: true
          }
        ]
        corsPolicy: {
          allowedOrigins: environment == 'prod' ? [
            'https://numbatwallet.wa.gov.au'
            'https://*.wa.gov.au'
          ] : [
            '*'
          ]
          allowedMethods: [
            'GET'
            'POST'
            'PUT'
            'DELETE'
            'OPTIONS'
          ]
          allowedHeaders: [
            '*'
          ]
          allowCredentials: true
        }
      }
      secrets: [
        {
          name: 'connection-string'
          keyVaultUrl: '${keyVaultId}/secrets/${dbConnectionStringSecretName}'
          identity: 'System'
        }
        {
          name: 'app-insights'
          value: appInsightsConnectionString
        }
      ]
      registries: [
        {
          server: '${containerRegistryName}.azurecr.io'
          identity: 'System'
        }
      ]
      dapr: {
        enabled: true
        appId: 'numbat-api'
        appPort: 80
        appProtocol: 'http'
      }
    }
    template: {
      containers: [
        {
          image: '${containerRegistryName}.azurecr.io/numbatwallet-api:${imageTag}'
          name: 'api'
          resources: {
            cpu: environment == 'prod' ? json('1.0') : json('0.5')
            memory: environment == 'prod' ? '2Gi' : '1Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: environment == 'prod' ? 'Production' : 'Development'
            }
            {
              name: 'ConnectionStrings__DefaultConnection'
              secretRef: 'connection-string'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              secretRef: 'app-insights'
            }
            {
              name: 'Azure__KeyVault__Url'
              value: keyVaultId
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:80'
            }
            {
              name: 'Jwt__Issuer'
              value: 'https://numbatwallet.wa.gov.au'
            }
            {
              name: 'MultiTenancy__Enabled'
              value: 'true'
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: 80
                scheme: 'HTTP'
              }
              initialDelaySeconds: 30
              periodSeconds: 30
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health/ready'
                port: 80
                scheme: 'HTTP'
              }
              initialDelaySeconds: 10
              periodSeconds: 10
            }
          ]
        }
      ]
      scale: {
        minReplicas: environment == 'prod' ? 2 : 1
        maxReplicas: environment == 'prod' ? 10 : 3
        rules: [
          {
            name: 'http-rule'
            http: {
              metadata: {
                concurrentRequests: '50'
              }
            }
          }
          {
            name: 'cpu-rule'
            custom: {
              type: 'cpu'
              metadata: {
                type: 'Utilization'
                value: '70'
              }
            }
          }
        ]
      }
    }
  }
  tags: {
    Environment: environment
    ManagedBy: 'Bicep'
    Application: 'NumbatWallet'
    Component: 'API'
  }
}

// Container Apps Admin Portal
resource adminContainerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: adminContainerAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 80
        transport: 'http'
        traffic: [
          {
            weight: 100
            latestRevision: true
          }
        ]
        ipSecurityRestrictions: environment == 'prod' ? [
          {
            name: 'AllowWAGovOnly'
            description: 'Allow WA Government IP ranges only'
            ipAddressRange: '203.0.113.0/24' // Replace with actual WA Gov IP range
            action: 'Allow'
          }
        ] : []
      }
      secrets: [
        {
          name: 'connection-string'
          keyVaultUrl: '${keyVaultId}/secrets/${dbConnectionStringSecretName}'
          identity: 'System'
        }
        {
          name: 'app-insights'
          value: appInsightsConnectionString
        }
      ]
      registries: [
        {
          server: '${containerRegistryName}.azurecr.io'
          identity: 'System'
        }
      ]
    }
    template: {
      containers: [
        {
          image: '${containerRegistryName}.azurecr.io/numbatwallet-admin:${imageTag}'
          name: 'admin'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: environment == 'prod' ? 'Production' : 'Development'
            }
            {
              name: 'ConnectionStrings__DefaultConnection'
              secretRef: 'connection-string'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              secretRef: 'app-insights'
            }
            {
              name: 'Azure__KeyVault__Url'
              value: keyVaultId
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:80'
            }
            {
              name: 'Authentication__AzureAd__TenantId'
              value: tenant().tenantId
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: 80
                scheme: 'HTTP'
              }
              initialDelaySeconds: 30
              periodSeconds: 30
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: environment == 'prod' ? 3 : 1
      }
    }
  }
  tags: {
    Environment: environment
    ManagedBy: 'Bicep'
    Application: 'NumbatWallet'
    Component: 'Admin'
  }
}

// Key Vault access for Container Apps
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: split(keyVaultId, '/')[8]
  scope: resourceGroup(split(keyVaultId, '/')[4])
}

resource apiKeyVaultAccessPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2023-07-01' = {
  name: 'add'
  parent: keyVault
  properties: {
    accessPolicies: [
      {
        tenantId: tenant().tenantId
        objectId: apiContainerApp.identity.principalId
        permissions: {
          secrets: [
            'get'
            'list'
          ]
          keys: [
            'get'
            'list'
            'sign'
            'verify'
          ]
        }
      }
      {
        tenantId: tenant().tenantId
        objectId: adminContainerApp.identity.principalId
        permissions: {
          secrets: [
            'get'
            'list'
          ]
        }
      }
    ]
  }
}

// Container Registry access
module containerRegistryAccess './container-registry-rbac.bicep' = {
  name: 'container-registry-access'
  params: {
    containerRegistryName: containerRegistryName
    principalIds: [
      apiContainerApp.identity.principalId
      adminContainerApp.identity.principalId
    ]
  }
}

// Outputs
output apiUrl string = 'https://${apiContainerApp.properties.configuration.ingress.fqdn}'
output adminUrl string = 'https://${adminContainerApp.properties.configuration.ingress.fqdn}'
output apiPrincipalId string = apiContainerApp.identity.principalId
output adminPrincipalId string = adminContainerApp.identity.principalId
output containerAppsEnvironmentId string = containerAppsEnvironment.id
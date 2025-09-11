// Container Apps Module
// Version: 1.0
// Description: Creates Container Apps with environment-based scaling

@description('Azure region')
param location string

@description('Naming prefix')
param namingPrefix string

@description('Resource tags')
param tags object

@description('Container Apps Environment ID')
param containerEnvironmentId string

@description('Container CPU cores')
param containerCpu string

@description('Container memory')
param containerMemory string

@description('Minimum replicas')
param minReplicas int

@description('Maximum replicas')
param maxReplicas int

@description('Enable auto-scaling')
param enableAutoScaling bool

@description('Key Vault name')
param keyVaultName string

@description('Managed Identity ID')
param managedIdentityId string

@description('Database host')
param databaseHost string

@description('Redis cache host')
param cacheHost string

@description('Service Bus namespace')
param serviceBusNamespace string

@description('Storage account name')
param storageAccountName string

@description('Environment name')
param environment string

// ==================== VARIABLES ====================
var apiAppName = 'ca-${namingPrefix}-api'
var workerAppName = 'ca-${namingPrefix}-worker'
var containerRegistry = environment == 'prod' ? 'numbatprod.azurecr.io' : 'numbatdev.azurecr.io'
var imageTag = environment == 'prod' ? 'stable' : 'latest'

// ==================== KEY VAULT REFERENCE ====================
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

// ==================== API CONTAINER APP ====================
resource apiApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: apiAppName
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerEnvironmentId
    configuration: {
      activeRevisionsMode: 'Multiple'  // Blue-green deployment
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        allowInsecure: false
        traffic: [
          {
            weight: 100
            latestRevision: true
          }
        ]
        corsPolicy: {
          allowedOrigins: environment == 'prod' ? [
            'https://wallet.wa.gov.au'
            'https://servicewa.wa.gov.au'
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
          name: 'db-connection'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/DatabaseConnectionString'
          identity: managedIdentityId
        }
        {
          name: 'redis-connection'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/RedisConnectionString'
          identity: managedIdentityId
        }
        {
          name: 'servicebus-connection'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/ServiceBusConnectionString'
          identity: managedIdentityId
        }
      ]
      registries: environment != 'dev' ? [
        {
          server: containerRegistry
          identity: managedIdentityId
        }
      ] : []
    }
    template: {
      containers: [
        {
          image: environment == 'dev' ? 'mcr.microsoft.com/dotnet/samples:aspnetapp' : '${containerRegistry}/numbat-api:${imageTag}'
          name: 'api'
          resources: {
            cpu: json(containerCpu)
            memory: containerMemory
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: environment == 'prod' ? 'Production' : 'Development'
            }
            {
              name: 'ConnectionStrings__Database'
              secretRef: 'db-connection'
            }
            {
              name: 'ConnectionStrings__Redis'
              secretRef: 'redis-connection'
            }
            {
              name: 'ConnectionStrings__ServiceBus'
              secretRef: 'servicebus-connection'
            }
            {
              name: 'Azure__KeyVault__Uri'
              value: keyVault.properties.vaultUri
            }
            {
              name: 'Azure__ManagedIdentity__ClientId'
              value: managedIdentityId
            }
            {
              name: 'ApplicationInsights__InstrumentationKey'
              value: 'will-be-set-by-monitoring-module'
            }
            {
              name: 'Tenant__MultiTenancy__Enabled'
              value: 'true'
            }
            {
              name: 'RateLimit__PermitLimit'
              value: environment == 'prod' ? '100' : '1000'
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health/live'
                port: 8080
              }
              initialDelaySeconds: 30
              periodSeconds: 30
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health/ready'
                port: 8080
              }
              initialDelaySeconds: 10
              periodSeconds: 10
            }
            {
              type: 'Startup'
              httpGet: {
                path: '/health/startup'
                port: 8080
              }
              initialDelaySeconds: 5
              periodSeconds: 5
              failureThreshold: 10
            }
          ]
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
        rules: enableAutoScaling ? [
          {
            name: 'http-scale-rule'
            http: {
              metadata: {
                concurrentRequests: '50'
              }
            }
          }
          {
            name: 'cpu-scale-rule'
            custom: {
              type: 'cpu'
              metadata: {
                type: 'Utilization'
                value: '70'
              }
            }
          }
          {
            name: 'memory-scale-rule'
            custom: {
              type: 'memory'
              metadata: {
                type: 'Utilization'
                value: '80'
              }
            }
          }
        ] : []
      }
    }
  }
}

// ==================== WORKER CONTAINER APP ====================
resource workerApp 'Microsoft.App/containerApps@2023-05-01' = if (environment != 'dev') {
  name: workerAppName
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerEnvironmentId
    configuration: {
      activeRevisionsMode: 'Single'
      secrets: [
        {
          name: 'db-connection'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/DatabaseConnectionString'
          identity: managedIdentityId
        }
        {
          name: 'servicebus-connection'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/ServiceBusConnectionString'
          identity: managedIdentityId
        }
      ]
      registries: [
        {
          server: containerRegistry
          identity: managedIdentityId
        }
      ]
    }
    template: {
      containers: [
        {
          image: '${containerRegistry}/numbat-worker:${imageTag}'
          name: 'worker'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'DOTNET_ENVIRONMENT'
              value: environment == 'prod' ? 'Production' : 'Development'
            }
            {
              name: 'ConnectionStrings__Database'
              secretRef: 'db-connection'
            }
            {
              name: 'ConnectionStrings__ServiceBus'
              secretRef: 'servicebus-connection'
            }
            {
              name: 'Worker__BatchSize'
              value: environment == 'prod' ? '100' : '10'
            }
            {
              name: 'Worker__MaxConcurrency'
              value: environment == 'prod' ? '10' : '2'
            }
          ]
        }
      ]
      scale: {
        minReplicas: environment == 'prod' ? 2 : 1
        maxReplicas: environment == 'prod' ? 10 : 3
        rules: [
          {
            name: 'servicebus-scale-rule'
            custom: {
              type: 'azure-servicebus'
              metadata: {
                queueName: 'credential-processing'
                messageCount: '10'
              }
              auth: [
                {
                  secretRef: 'servicebus-connection'
                  triggerParameter: 'connection'
                }
              ]
            }
          }
        ]
      }
    }
  }
}

// ==================== OUTPUTS ====================
output apiUrl string = 'https://${apiApp.properties.configuration.ingress.fqdn}'
output apiAppName string = apiApp.name
output workerAppName string = environment != 'dev' ? workerApp.name : 'N/A'
output apiAppId string = apiApp.id
// Container Apps Module for NumbatWallet Applications
// Deploys API and Admin containers to Container Apps Environment

@description('Container Apps Environment ID')
param containerAppsEnvId string

@description('Location for resources')
param location string

@description('Container Registry server')
param containerRegistryServer string

@description('Container Registry managed identity')
param registryManagedIdentityId string = ''

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Key Vault URI')
param keyVaultUri string

@description('PostgreSQL connection string (stored in Key Vault)')
@secure()
param postgresConnectionString string

@description('Redis connection string (stored in Key Vault)')
@secure()
param redisConnectionString string

@description('JWT signing key')
@secure()
param jwtSigningKey string

@description('Environment name')
@allowed([
  'dev'
  'test'
  'prod'
])
param environment string

@description('API image tag')
param apiImageTag string = 'latest'

@description('Admin image tag')
param adminImageTag string = 'latest'

@description('Minimum replicas')
param minReplicas int = environment == 'prod' ? 2 : 1

@description('Maximum replicas')
param maxReplicas int = environment == 'prod' ? 10 : 5

@description('CPU cores')
param cpu string = environment == 'prod' ? '1' : '0.5'

@description('Memory in Gi')
param memory string = environment == 'prod' ? '2Gi' : '1Gi'

@description('Custom domain for API')
param apiCustomDomain string = ''

@description('Custom domain for Admin')
param adminCustomDomain string = ''

@description('Tags to apply to resources')
param tags object = {}

// ========================================
// Resources
// ========================================

// Managed Identity for Container Apps
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'mi-containerapps-${environment}'
  location: location
  tags: tags
}

// API Container App
resource apiContainerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'ca-api-${environment}'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvId
    configuration: {
      activeRevisionsMode: 'Multiple'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        traffic: [
          {
            revisionName: '${apiContainerApp.name}--${apiImageTag}'
            weight: 100
          }
        ]
        corsPolicy: {
          allowedOrigins: environment == 'prod' ? [
            'https://${apiCustomDomain}'
            'https://${adminCustomDomain}'
          ] : ['*']
          allowedMethods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS']
          allowedHeaders: ['*']
          exposeHeaders: ['*']
          allowCredentials: true
          maxAge: 86400
        }
        customDomains: empty(apiCustomDomain) ? [] : [
          {
            name: apiCustomDomain
            bindingType: 'SniEnabled'
          }
        ]
      }
      registries: empty(registryManagedIdentityId) ? [] : [
        {
          server: containerRegistryServer
          identity: registryManagedIdentityId
        }
      ]
      secrets: [
        {
          name: 'postgres-connection'
          value: postgresConnectionString
        }
        {
          name: 'redis-connection'
          value: redisConnectionString
        }
        {
          name: 'jwt-signing-key'
          value: jwtSigningKey
        }
      ]
      dapr: {
        enabled: true
        appId: 'numbatwallet-api'
        appProtocol: 'http'
        appPort: 8080
        httpReadBufferSize: 30
      }
    }
    template: {
      containers: [
        {
          image: '${containerRegistryServer}/numbatwallet-api:${apiImageTag}'
          name: 'api'
          resources: {
            cpu: json(cpu)
            memory: memory
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: environment == 'prod' ? 'Production' : environment == 'test' ? 'Staging' : 'Development'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: appInsightsConnectionString
            }
            {
              name: 'AZURE_KEY_VAULT_URI'
              value: keyVaultUri
            }
            {
              name: 'ConnectionStrings__PostgreSQL'
              secretRef: 'postgres-connection'
            }
            {
              name: 'ConnectionStrings__Redis'
              secretRef: 'redis-connection'
            }
            {
              name: 'Authentication__JwtSigningKey'
              secretRef: 'jwt-signing-key'
            }
            {
              name: 'MultiTenancy__Enabled'
              value: 'true'
            }
            {
              name: 'OTEL_EXPORTER_OTLP_ENDPOINT'
              value: 'http://localhost:4317'
            }
            {
              name: 'OTEL_SERVICE_NAME'
              value: 'numbatwallet-api'
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
              periodSeconds: 10
              failureThreshold: 30
            }
          ]
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '100'
              }
            }
          }
          {
            name: 'cpu-scaling'
            custom: {
              type: 'cpu'
              metadata: {
                type: 'Utilization'
                value: '80'
              }
            }
          }
        ]
      }
    }
  }
}

// Admin Portal Container App
resource adminContainerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'ca-admin-${environment}'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
        ipSecurityRestrictions: environment == 'prod' ? [
          {
            name: 'AllowGovernmentNetwork'
            description: 'Allow WA Government network'
            ipAddressRange: '203.0.113.0/24'  // Replace with actual Government IP range
            action: 'Allow'
          }
        ] : []
        customDomains: empty(adminCustomDomain) ? [] : [
          {
            name: adminCustomDomain
            bindingType: 'SniEnabled'
          }
        ]
      }
      registries: empty(registryManagedIdentityId) ? [] : [
        {
          server: containerRegistryServer
          identity: registryManagedIdentityId
        }
      ]
      secrets: [
        {
          name: 'postgres-connection'
          value: postgresConnectionString
        }
        {
          name: 'redis-connection'
          value: redisConnectionString
        }
      ]
    }
    template: {
      containers: [
        {
          image: '${containerRegistryServer}/numbatwallet-admin:${adminImageTag}'
          name: 'admin'
          resources: {
            cpu: json(cpu)
            memory: memory
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: environment == 'prod' ? 'Production' : environment == 'test' ? 'Staging' : 'Development'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: appInsightsConnectionString
            }
            {
              name: 'AZURE_KEY_VAULT_URI'
              value: keyVaultUri
            }
            {
              name: 'ConnectionStrings__PostgreSQL'
              secretRef: 'postgres-connection'
            }
            {
              name: 'ConnectionStrings__Redis'
              secretRef: 'redis-connection'
            }
            {
              name: 'Authentication__AzureAd__TenantId'
              value: subscription().tenantId
            }
            {
              name: 'Authentication__AzureAd__ClientId'
              value: 'TO_BE_CONFIGURED'  // Will be configured during deployment
            }
            {
              name: 'OTEL_SERVICE_NAME'
              value: 'numbatwallet-admin'
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: 8080
              }
              initialDelaySeconds: 30
              periodSeconds: 30
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health'
                port: 8080
              }
              initialDelaySeconds: 10
              periodSeconds: 10
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1  // Admin portal typically needs fewer instances
        maxReplicas: environment == 'prod' ? 5 : 3
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '50'
              }
            }
          }
        ]
      }
    }
  }
}

// Dapr Components for State Store
resource daprStateStore 'Microsoft.App/managedEnvironments/daprComponents@2023-05-01' = {
  name: '${containerAppsEnvId}/statestore'
  properties: {
    componentType: 'state.redis'
    version: 'v1'
    ignoreErrors: false
    initTimeout: '30s'
    metadata: [
      {
        name: 'redisHost'
        value: split(redisConnectionString, ',')[0]
      }
      {
        name: 'redisPassword'
        value: split(split(redisConnectionString, 'password=')[1], ',')[0]
      }
      {
        name: 'enableTLS'
        value: 'true'
      }
    ]
    scopes: [
      'numbatwallet-api'
    ]
  }
}

// Dapr Components for Pub/Sub
resource daprPubSub 'Microsoft.App/managedEnvironments/daprComponents@2023-05-01' = {
  name: '${containerAppsEnvId}/pubsub'
  properties: {
    componentType: 'pubsub.redis'
    version: 'v1'
    ignoreErrors: false
    initTimeout: '30s'
    metadata: [
      {
        name: 'redisHost'
        value: split(redisConnectionString, ',')[0]
      }
      {
        name: 'redisPassword'
        value: split(split(redisConnectionString, 'password=')[1], ',')[0]
      }
      {
        name: 'enableTLS'
        value: 'true'
      }
    ]
    scopes: [
      'numbatwallet-api'
    ]
  }
}

// ========================================
// Outputs
// ========================================

output apiAppUrl string = apiContainerApp.properties.configuration.ingress.fqdn
output adminAppUrl string = adminContainerApp.properties.configuration.ingress.fqdn
output apiAppId string = apiContainerApp.id
output adminAppId string = adminContainerApp.id
output managedIdentityId string = managedIdentity.id
output managedIdentityClientId string = managedIdentity.properties.clientId
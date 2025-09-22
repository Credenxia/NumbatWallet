// Azure Container Apps deployment for NumbatWallet API
// POA: Issue #37 - Deploy backend to Azure Container Apps

@description('The environment name (dev, test, prod)')
param environment string = 'dev'

@description('The location for all resources')
param location string = resourceGroup().location

@description('The container image to deploy')
param containerImage string = 'numbatwallet.azurecr.io/numbatwallet-api:latest'

@description('The ACR registry name')
param containerRegistryName string = 'numbatwallet'

@description('PostgreSQL connection string')
@secure()
param postgresConnectionString string

@description('Key Vault URI')
param keyVaultUri string

@description('Application Insights connection string')
@secure()
param appInsightsConnectionString string

@description('Minimum number of replicas')
@minValue(0)
@maxValue(10)
param minReplicas int = 1

@description('Maximum number of replicas')
@minValue(1)
@maxValue(30)
param maxReplicas int = 10

var uniqueSuffix = uniqueString(resourceGroup().id)
var workspaceName = 'law-numbatwallet-${environment}-${uniqueSuffix}'
var containerAppEnvName = 'cae-numbatwallet-${environment}'
var containerAppName = 'ca-numbatwallet-api-${environment}'
var userAssignedIdentityName = 'id-numbatwallet-${environment}'

// User Assigned Managed Identity
resource userAssignedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: userAssignedIdentityName
  location: location
}

// Log Analytics Workspace
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: workspaceName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// Container Apps Environment
resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: containerAppEnvName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
    zoneRedundant: environment == 'prod'
  }
}

// Container Registry
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: containerRegistryName
}

// Container App
resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: containerAppName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      secrets: [
        {
          name: 'postgres-connection'
          value: postgresConnectionString
        }
        {
          name: 'appinsights-connection'
          value: appInsightsConnectionString
        }
      ]
      registries: [
        {
          server: '${containerRegistryName}.azurecr.io'
          identity: userAssignedIdentity.id
        }
      ]
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        corsPolicy: {
          allowedOrigins: [
            'https://numbatwallet.gov.au'
            'https://*.numbatwallet.gov.au'
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
        traffic: [
          {
            weight: 100
            latestRevision: true
          }
        ]
      }
    }
    template: {
      containers: [
        {
          image: containerImage
          name: 'numbatwallet-api'
          resources: {
            cpu: json(environment == 'prod' ? '2.0' : '0.5')
            memory: environment == 'prod' ? '4.0Gi' : '1.0Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: environment == 'prod' ? 'Production' : 'Development'
            }
            {
              name: 'ConnectionStrings__numbatwallet'
              secretRef: 'postgres-connection'
            }
            {
              name: 'KeyVault__Uri'
              value: keyVaultUri
            }
            {
              name: 'ApplicationInsights__ConnectionString'
              secretRef: 'appinsights-connection'
            }
            {
              name: 'Hsm__Provider'
              value: environment == 'prod' ? 'KeyVault' : 'Software'
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: userAssignedIdentity.properties.clientId
            }
          ]
          probes: [
            {
              type: 'Startup'
              httpGet: {
                path: '/health'
                port: 8080
              }
              initialDelaySeconds: 10
              periodSeconds: 10
              failureThreshold: 3
            }
            {
              type: 'Liveness'
              httpGet: {
                path: '/live'
                port: 8080
              }
              initialDelaySeconds: 30
              periodSeconds: 30
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/ready'
                port: 8080
              }
              initialDelaySeconds: 20
              periodSeconds: 10
              failureThreshold: 3
            }
          ]
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
        rules: [
          {
            name: 'http-rule'
            http: {
              metadata: {
                concurrentRequests: '100'
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
}

// Outputs
output containerAppUrl string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
output containerAppId string = containerApp.id
output managedIdentityClientId string = userAssignedIdentity.properties.clientId
output managedIdentityPrincipalId string = userAssignedIdentity.properties.principalId
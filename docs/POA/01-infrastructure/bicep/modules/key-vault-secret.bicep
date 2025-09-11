// Key Vault Secret Helper Module
// Version: 1.0
// Purpose: Helper module to create or update Key Vault secrets

@description('Key Vault name')
param keyVaultName string

@description('Secret name')
@minLength(1)
@maxLength(127)
param secretName string

@description('Secret value')
@secure()
param secretValue string

@description('Secret expiry date (optional)')
param expiryDate string = ''

@description('Secret activation date (optional)')
param activationDate string = ''

@description('Tags to apply to the secret')
param tags object = {}

@description('Content type of the secret')
param contentType string = 'text/plain'

// Reference existing Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

// Create or update the secret
resource secret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: secretName
  tags: tags
  properties: {
    value: secretValue
    contentType: contentType
    attributes: {
      enabled: true
      exp: !empty(expiryDate) ? dateTimeToEpoch(expiryDate) : null
      nbf: !empty(activationDate) ? dateTimeToEpoch(activationDate) : null
    }
  }
}

// Outputs
output secretId string = secret.id
output secretUri string = secret.properties.secretUri
output secretUriWithVersion string = secret.properties.secretUriWithVersion
output secretName string = secret.name
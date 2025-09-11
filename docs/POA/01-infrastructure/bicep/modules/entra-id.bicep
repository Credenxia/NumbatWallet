// Azure Entra ID Configuration Module
// Version: 1.0
// Purpose: Configure Azure Entra ID app registration and permissions for POA

@description('Environment name')
@allowed(['dev', 'test', 'demo', 'prod'])
param environment string

@description('Application name')
param appName string = 'numbat-wallet'

@description('Primary domain for the organization')
param primaryDomain string = 'wa.gov.au'

@description('Admin portal URL')
param adminPortalUrl string

@description('API URL')
param apiUrl string

@description('Key Vault name for storing secrets')
param keyVaultName string

@description('Tags to apply to resources')
param tags object = {}

// Variables
var appRegistrationName = '${appName}-${environment}'
var replyUrls = [
  '${adminPortalUrl}/signin-oidc'
  '${apiUrl}/signin-oidc'
  environment == 'dev' ? 'https://localhost:5001/signin-oidc' : ''
]

// Note: Azure Entra ID App Registrations cannot be created via Bicep
// This module outputs the PowerShell script to create the app registration

output setupScript string = '''
# Azure Entra ID Setup Script for ${appRegistrationName}
# Generated: ${utcNow()}
# Environment: ${environment}

# Variables
$appName = "${appRegistrationName}"
$replyUrls = @(
  "${adminPortalUrl}/signin-oidc",
  "${apiUrl}/signin-oidc"
)

if ("${environment}" -eq "dev") {
  $replyUrls += "https://localhost:5001/signin-oidc"
}

# Connect to Azure AD
Connect-AzureAD

# Create App Registration
$app = New-AzureADApplication `
  -DisplayName $appName `
  -IdentifierUris "https://${primaryDomain}/${appName}" `
  -ReplyUrls $replyUrls `
  -AvailableToOtherTenants $false

Write-Host "Created App Registration: $($app.ObjectId)"

# Create Client Secret
$endDate = (Get-Date).AddYears(1)
$secret = New-AzureADApplicationPasswordCredential `
  -ObjectId $app.ObjectId `
  -EndDate $endDate `
  -CustomKeyIdentifier "${appName}-secret"

Write-Host "Created Client Secret (store securely): $($secret.Value)"

# Configure API Permissions
$graphApiId = "00000003-0000-0000-c000-000000000000"  # Microsoft Graph

# User.Read permission
$userReadPermission = "e1fe6dd8-ba31-4d61-89e7-88639da4683d"
$graphAccess = New-Object -TypeName Microsoft.Open.AzureAD.Model.RequiredResourceAccess
$graphAccess.ResourceAppId = $graphApiId
$graphAccess.ResourceAccess = @(
  New-Object -TypeName Microsoft.Open.AzureAD.Model.ResourceAccess `
    -ArgumentList $userReadPermission, "Scope"
)

# Set permissions
Set-AzureADApplication `
  -ObjectId $app.ObjectId `
  -RequiredResourceAccess @($graphAccess)

Write-Host "Configured API Permissions"

# Create App Roles
$adminRole = New-Object Microsoft.Open.AzureAD.Model.AppRole
$adminRole.AllowedMemberTypes = @("User")
$adminRole.Description = "System Administrator with full access"
$adminRole.DisplayName = "Admin"
$adminRole.Id = [Guid]::NewGuid()
$adminRole.IsEnabled = $true
$adminRole.Value = "Admin"

$issuerRole = New-Object Microsoft.Open.AzureAD.Model.AppRole
$issuerRole.AllowedMemberTypes = @("User")
$issuerRole.Description = "Credential Issuer"
$issuerRole.DisplayName = "Issuer"
$issuerRole.Id = [Guid]::NewGuid()
$issuerRole.IsEnabled = $true
$issuerRole.Value = "Issuer"

$verifierRole = New-Object Microsoft.Open.AzureAD.Model.AppRole
$verifierRole.AllowedMemberTypes = @("User", "Application")
$verifierRole.Description = "Credential Verifier"
$verifierRole.DisplayName = "Verifier"
$verifierRole.Id = [Guid]::NewGuid()
$verifierRole.IsEnabled = $true
$verifierRole.Value = "Verifier"

$operatorRole = New-Object Microsoft.Open.AzureAD.Model.AppRole
$operatorRole.AllowedMemberTypes = @("User")
$operatorRole.Description = "System Operator with limited access"
$operatorRole.DisplayName = "Operator"
$operatorRole.Id = [Guid]::NewGuid()
$operatorRole.IsEnabled = $true
$operatorRole.Value = "Operator"

# Set app roles
Set-AzureADApplication `
  -ObjectId $app.ObjectId `
  -AppRoles @($adminRole, $issuerRole, $verifierRole, $operatorRole)

Write-Host "Created App Roles"

# Create Service Principal
$sp = New-AzureADServicePrincipal `
  -AppId $app.AppId `
  -AppRoleAssignmentRequired $true

Write-Host "Created Service Principal: $($sp.ObjectId)"

# Output configuration
Write-Host ""
Write-Host "========================================="
Write-Host "Azure Entra ID Configuration Complete"
Write-Host "========================================="
Write-Host "Application (client) ID: $($app.AppId)"
Write-Host "Directory (tenant) ID: $((Get-AzureADTenantDetail).ObjectId)"
Write-Host "Client Secret: $($secret.Value)"
Write-Host ""
Write-Host "Add these values to Key Vault ${keyVaultName}:"
Write-Host "  az keyvault secret set --vault-name ${keyVaultName} --name 'AzureAd--ClientId' --value '$($app.AppId)'"
Write-Host "  az keyvault secret set --vault-name ${keyVaultName} --name 'AzureAd--ClientSecret' --value '$($secret.Value)'"
Write-Host "  az keyvault secret set --vault-name ${keyVaultName} --name 'AzureAd--TenantId' --value '$((Get-AzureADTenantDetail).ObjectId)'"
Write-Host ""
Write-Host "Update appsettings.json with:"
Write-Host '{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "@Microsoft.KeyVault(SecretUri=https://${keyVaultName}.vault.azure.net/secrets/AzureAd--TenantId)",
    "ClientId": "@Microsoft.KeyVault(SecretUri=https://${keyVaultName}.vault.azure.net/secrets/AzureAd--ClientId)",
    "ClientSecret": "@Microsoft.KeyVault(SecretUri=https://${keyVaultName}.vault.azure.net/secrets/AzureAd--ClientSecret)",
    "Domain": "${primaryDomain}",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  }
}'
'''

output appRegistrationName string = appRegistrationName
output identifierUri string = 'https://${primaryDomain}/${appRegistrationName}'
output replyUrls array = filter(replyUrls, url => !empty(url))

output configurationJson object = {
  AzureAd: {
    Instance: 'https://login.microsoftonline.com/'
    TenantId: '@Microsoft.KeyVault(SecretUri=https://${keyVaultName}.vault.azure.net/secrets/AzureAd--TenantId)'
    ClientId: '@Microsoft.KeyVault(SecretUri=https://${keyVaultName}.vault.azure.net/secrets/AzureAd--ClientId)'
    ClientSecret: '@Microsoft.KeyVault(SecretUri=https://${keyVaultName}.vault.azure.net/secrets/AzureAd--ClientSecret)'
    Domain: primaryDomain
    CallbackPath: '/signin-oidc'
    SignedOutCallbackPath: '/signout-callback-oidc'
    Scopes: [
      'User.Read'
      'profile'
      'email'
    ]
    TokenValidationParameters: {
      ValidateIssuer: true
      ValidateAudience: true
      ValidateLifetime: true
      ClockSkew: '00:05:00'
    }
  }
}

output roles array = [
  {
    name: 'Admin'
    description: 'System Administrator with full access'
    value: 'Admin'
  }
  {
    name: 'Issuer'
    description: 'Credential Issuer'
    value: 'Issuer'
  }
  {
    name: 'Verifier'
    description: 'Credential Verifier'
    value: 'Verifier'
  }
  {
    name: 'Operator'
    description: 'System Operator with limited access'
    value: 'Operator'
  }
]

output postDeploymentSteps array = [
  '1. Run the PowerShell script output above to create the Azure Entra ID app registration'
  '2. Store the Client ID and Client Secret in Key Vault ${keyVaultName}'
  '3. Grant admin consent for the API permissions in Azure Portal'
  '4. Assign users to the appropriate roles'
  '5. Configure Conditional Access policies if required'
  '6. Enable MFA for all users accessing the admin portal'
]
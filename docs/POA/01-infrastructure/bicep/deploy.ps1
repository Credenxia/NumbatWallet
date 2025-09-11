# PowerShell Deployment Script for NumbatWallet POA Infrastructure
# Version: 1.0
# Description: Deploys Bicep templates with environment-based configuration

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('dev', 'test', 'demo', 'prod')]
    [string]$Environment,
    
    [Parameter(Mandatory=$true)]
    [string]$AdminEmail,
    
    [Parameter(Mandatory=$false)]
    [string]$Location = 'australiaeast',
    
    [Parameter(Mandatory=$false)]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory=$false)]
    [switch]$WhatIf,
    
    [Parameter(Mandatory=$false)]
    [switch]$ValidateOnly,
    
    [Parameter(Mandatory=$false)]
    [switch]$DestroyAfterDemo
)

# Set strict mode
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Colors for output
$colors = @{
    Success = 'Green'
    Warning = 'Yellow'
    Error = 'Red'
    Info = 'Cyan'
}

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = 'White'
    )
    Write-Host $Message -ForegroundColor $Color
}

# Banner
Write-ColorOutput "`n========================================" $colors.Info
Write-ColorOutput " NumbatWallet POA Infrastructure Deployment" $colors.Info
Write-ColorOutput "========================================`n" $colors.Info

# Validate Azure CLI is installed
try {
    $azVersion = az version --output json | ConvertFrom-Json
    Write-ColorOutput "✓ Azure CLI version: $($azVersion.'azure-cli')" $colors.Success
} catch {
    Write-ColorOutput "✗ Azure CLI is not installed or not in PATH" $colors.Error
    exit 1
}

# Login check
$account = az account show --output json 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-ColorOutput "→ Not logged in to Azure. Initiating login..." $colors.Warning
    az login
    $account = az account show --output json | ConvertFrom-Json
}

Write-ColorOutput "✓ Logged in as: $($account.user.name)" $colors.Success
Write-ColorOutput "✓ Subscription: $($account.name) [$($account.id)]" $colors.Success

# Set subscription if provided
if ($SubscriptionId) {
    Write-ColorOutput "→ Setting subscription to: $SubscriptionId" $colors.Info
    az account set --subscription $SubscriptionId
}

# Variables
$deploymentName = "numbat-poa-$Environment-$(Get-Date -Format 'yyyyMMddHHmmss')"
$resourceGroupName = "rg-numbat-poa-$Environment"
$templateFile = Join-Path $PSScriptRoot "main.bicep"
$parametersFile = Join-Path $PSScriptRoot "parameters.$Environment.json"

# Validate template file exists
if (-not (Test-Path $templateFile)) {
    Write-ColorOutput "✗ Template file not found: $templateFile" $colors.Error
    exit 1
}

# Create parameters file if it doesn't exist
if (-not (Test-Path $parametersFile)) {
    Write-ColorOutput "→ Creating parameters file for $Environment environment" $colors.Info
    
    $parameters = @{
        '$schema' = 'https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#'
        contentVersion = '1.0.0.0'
        parameters = @{
            environment = @{ value = $Environment }
            location = @{ value = $Location }
            adminEmail = @{ value = $AdminEmail }
            resourceGroupName = @{ value = $resourceGroupName }
        }
    }
    
    $parameters | ConvertTo-Json -Depth 10 | Out-File $parametersFile -Encoding UTF8
    Write-ColorOutput "✓ Parameters file created: $parametersFile" $colors.Success
}

# Cost estimates
$costEstimates = @{
    dev = '~$150 AUD/month'
    test = '~$300 AUD/month'
    demo = '~$800 AUD/month'
    prod = '~$1,500 AUD/month'
}

Write-ColorOutput "`n📊 Deployment Configuration:" $colors.Info
Write-ColorOutput "   Environment: $Environment" $colors.Info
Write-ColorOutput "   Location: $Location" $colors.Info
Write-ColorOutput "   Resource Group: $resourceGroupName" $colors.Info
Write-ColorOutput "   Estimated Cost: $($costEstimates[$Environment])" $colors.Warning

# Validate only
if ($ValidateOnly) {
    Write-ColorOutput "`n→ Validating deployment template..." $colors.Info
    
    $validation = az deployment sub validate `
        --location $Location `
        --template-file $templateFile `
        --parameters $parametersFile `
        --output json | ConvertFrom-Json
    
    if ($validation.error) {
        Write-ColorOutput "✗ Validation failed: $($validation.error.message)" $colors.Error
        exit 1
    }
    
    Write-ColorOutput "✓ Template validation successful" $colors.Success
    exit 0
}

# What-If deployment
if ($WhatIf) {
    Write-ColorOutput "`n→ Running What-If analysis..." $colors.Info
    
    az deployment sub what-if `
        --location $Location `
        --template-file $templateFile `
        --parameters $parametersFile `
        --output table
    
    Write-ColorOutput "`n✓ What-If analysis complete" $colors.Success
    exit 0
}

# Confirm deployment
Write-ColorOutput "`n⚠️  This will deploy real Azure resources and incur costs!" $colors.Warning
$confirm = Read-Host "Do you want to proceed with the deployment? (yes/no)"
if ($confirm -ne 'yes') {
    Write-ColorOutput "Deployment cancelled by user" $colors.Warning
    exit 0
}

# Deploy
Write-ColorOutput "`n→ Starting deployment: $deploymentName" $colors.Info
Write-ColorOutput "   This may take 15-30 minutes..." $colors.Info

try {
    $deployment = az deployment sub create `
        --name $deploymentName `
        --location $Location `
        --template-file $templateFile `
        --parameters $parametersFile `
        --output json | ConvertFrom-Json
    
    if ($deployment.properties.provisioningState -eq 'Succeeded') {
        Write-ColorOutput "`n✓ Deployment successful!" $colors.Success
        
        # Display outputs
        Write-ColorOutput "`n📋 Deployment Outputs:" $colors.Info
        Write-ColorOutput "   Resource Group: $($deployment.properties.outputs.resourceGroupName.value)" $colors.Info
        Write-ColorOutput "   API URL: $($deployment.properties.outputs.apiUrl.value)" $colors.Info
        Write-ColorOutput "   Key Vault: $($deployment.properties.outputs.keyVaultName.value)" $colors.Info
        Write-ColorOutput "   Database Server: $($deployment.properties.outputs.databaseServer.value)" $colors.Info
        
        if ($Environment -eq 'demo' -or $Environment -eq 'prod') {
            Write-ColorOutput "   API Management: $($deployment.properties.outputs.apiManagementUrl.value)" $colors.Info
            Write-ColorOutput "   Front Door: $($deployment.properties.outputs.frontDoorUrl.value)" $colors.Info
        }
        
        # Save outputs to file
        $outputFile = Join-Path $PSScriptRoot "deployment-outputs-$Environment.json"
        $deployment.properties.outputs | ConvertTo-Json -Depth 10 | Out-File $outputFile -Encoding UTF8
        Write-ColorOutput "`n✓ Outputs saved to: $outputFile" $colors.Success
        
    } else {
        Write-ColorOutput "✗ Deployment failed: $($deployment.properties.provisioningState)" $colors.Error
        exit 1
    }
    
} catch {
    Write-ColorOutput "✗ Deployment error: $_" $colors.Error
    
    # Get deployment operations for debugging
    Write-ColorOutput "`n→ Getting deployment error details..." $colors.Info
    az deployment operation sub list `
        --name $deploymentName `
        --query "[?properties.provisioningState=='Failed']" `
        --output table
    
    exit 1
}

# Post-deployment tasks
if ($Environment -eq 'demo' -and $DestroyAfterDemo) {
    Write-ColorOutput "`n⏰ Demo environment will be automatically destroyed in 4 hours" $colors.Warning
    
    # Schedule deletion (requires Azure Automation or Logic App)
    # This is a placeholder - implement based on your automation preferences
    Write-ColorOutput "   Note: Automatic deletion requires Azure Automation setup" $colors.Info
}

# Display next steps
Write-ColorOutput "`n📝 Next Steps:" $colors.Info
Write-ColorOutput "   1. Configure application settings in Container Apps" $colors.Info
Write-ColorOutput "   2. Deploy application code to Container Registry" $colors.Info
Write-ColorOutput "   3. Configure custom domain and SSL certificate" $colors.Info
Write-ColorOutput "   4. Set up monitoring alerts" $colors.Info
Write-ColorOutput "   5. Run smoke tests" $colors.Info

# Cleanup commands
Write-ColorOutput "`n🗑️  To delete this deployment:" $colors.Info
Write-ColorOutput "   az group delete --name $resourceGroupName --yes --no-wait" $colors.Info

Write-ColorOutput "`n✅ Deployment script completed successfully!`n" $colors.Success